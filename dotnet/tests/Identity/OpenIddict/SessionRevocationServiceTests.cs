using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using AQ.Identity.OpenIddict.Sessions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Tests;

public class SessionRevocationServiceTests
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";
    private const string WebAppId = "app-web";

    private readonly IOpenIddictAuthorizationManager _authorizationManager = Substitute.For<IOpenIddictAuthorizationManager>();
    private readonly IOpenIddictTokenManager _tokenManager = Substitute.For<IOpenIddictTokenManager>();
    private readonly BackchannelLogoutNotifier _notifier;
    private readonly List<BackchannelLogoutTarget> _notified = [];
    private readonly List<object> _authorizations = [];
    private readonly SessionRevocationService _service;

    public SessionRevocationServiceTests()
    {
        _authorizationManager.FindBySubjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => ToAsync(_authorizations.Where(a => ((Authorization)a).Subject == ci.ArgAt<string>(0))));
        _authorizationManager.FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ValueTask<object?>(_authorizations.FirstOrDefault(a => ((Authorization)a).Id == ci.ArgAt<string>(0))));
        _authorizationManager.GetIdAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ValueTask<string?>(((Authorization)ci.ArgAt<object>(0)).Id));
        _authorizationManager.GetSubjectAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ValueTask<string?>(((Authorization)ci.ArgAt<object>(0)).Subject));
        _authorizationManager.GetStatusAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ValueTask<string?>(((Authorization)ci.ArgAt<object>(0)).Status));
        _authorizationManager.GetApplicationIdAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>(WebAppId));
        _authorizationManager.GetPropertiesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ValueTask<ImmutableDictionary<string, JsonElement>>(((Authorization)ci.ArgAt<object>(0)).Properties));

        _notifier = Substitute.ForPartsOf<BackchannelLogoutNotifier>(
            Substitute.For<IOpenIddictApplicationManager>(),
            Substitute.For<IOptionsMonitor<OpenIddictServerOptions>>(),
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<ILogger<BackchannelLogoutNotifier>>());
        _notifier.NotifyAsync(Arg.Any<IEnumerable<BackchannelLogoutTarget>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _notified.AddRange(ci.ArgAt<IEnumerable<BackchannelLogoutTarget>>(0));
                return Task.CompletedTask;
            });

        _service = new SessionRevocationService(_authorizationManager, _tokenManager, _notifier, Substitute.For<ILogger<SessionRevocationService>>());
    }

    [Fact]
    public async Task RevokeForSignOutAsync_RevokesEverySessionOfTheBrowserSession_AndNoOthers()
    {
        var webApp = Add("a1", UserId, sid: "browser-1");
        var otherApp = Add("a2", UserId, sid: "browser-1");
        var otherDevice = Add("a3", UserId, sid: "browser-2");

        await _service.RevokeForSignOutAsync(Browser(UserId, "browser-1"), idTokenHint: null, CancellationToken.None);

        await AssertRevokedAsync(webApp, otherApp);
        await AssertNotRevokedAsync(otherDevice);
        _notified.Should().BeEquivalentTo([
            new BackchannelLogoutTarget(WebAppId, UserId, "browser-1"),
            new BackchannelLogoutTarget(WebAppId, UserId, "browser-1"),
        ]);
    }

    [Fact]
    public async Task RevokeForSignOutAsync_WithoutIdpCookie_UsesTheIdTokenHint()
    {
        var session = Add("a1", UserId, sid: "browser-1");
        var legacy = Add("a2", UserId, sid: null);

        await _service.RevokeForSignOutAsync(browser: null, Hint(UserId, "browser-1", authorizationId: "a2"), CancellationToken.None);

        await AssertRevokedAsync(session, legacy);
        _notified.Should().Contain(new BackchannelLogoutTarget(WebAppId, UserId, null));
    }

    [Fact]
    public async Task RevokeForSignOutAsync_IgnoresAHintForAnotherUser()
    {
        var mine = Add("a1", UserId, sid: "browser-1");
        var theirs = Add("a2", OtherUserId, sid: "browser-9");

        await _service.RevokeForSignOutAsync(Browser(UserId, "browser-1"), Hint(OtherUserId, "browser-9", "a2"), CancellationToken.None);

        await AssertRevokedAsync(mine);
        await AssertNotRevokedAsync(theirs);
    }

    [Fact]
    public async Task RevokeAuthorizationAsync_RefusesAnotherUsersSession()
    {
        var theirs = Add("a1", OtherUserId, sid: "browser-9");

        var revoked = await _service.RevokeAuthorizationAsync(UserId, "a1", CancellationToken.None);

        revoked.Should().BeFalse();
        await AssertNotRevokedAsync(theirs);
        _notified.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeAuthorizationAsync_SkipsAnAlreadyRevokedSession()
    {
        var revokedEarlier = Add("a1", UserId, sid: "browser-1", status: Statuses.Revoked);

        var revoked = await _service.RevokeAuthorizationAsync(UserId, "a1", CancellationToken.None);

        revoked.Should().BeFalse();
        await AssertNotRevokedAsync(revokedEarlier);
        _notified.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeAllAsync_RevokesAndNotifiesEverySession_ThenSweepsTheRest()
    {
        var first = Add("a1", UserId, sid: "browser-1");
        var second = Add("a2", UserId, sid: "browser-2");

        await _service.RevokeAllAsync(UserId, CancellationToken.None);

        await AssertRevokedAsync(first, second);
        _notified.Select(t => t.SessionId).Should().BeEquivalentTo(["browser-1", "browser-2"]);
        await _authorizationManager.Received(1).RevokeBySubjectAsync(UserId, Arg.Any<CancellationToken>());
        await _tokenManager.Received(1).RevokeBySubjectAsync(UserId, Arg.Any<CancellationToken>());
    }

    private Authorization Add(string id, string subject, string? sid, string status = Statuses.Valid)
    {
        var properties = sid is null
            ? ImmutableDictionary<string, JsonElement>.Empty
            : ImmutableDictionary<string, JsonElement>.Empty.Add(BrowserSession.SessionIdClaim, JsonSerializer.SerializeToElement(sid));
        var authorization = new Authorization(id, subject, status, properties);
        _authorizations.Add(authorization);
        return authorization;
    }

    private async Task AssertRevokedAsync(params Authorization[] authorizations)
    {
        foreach (var authorization in authorizations)
        {
            await _authorizationManager.Received(1).TryRevokeAsync(authorization, Arg.Any<CancellationToken>());
            await _tokenManager.Received(1).RevokeByAuthorizationIdAsync(authorization.Id, Arg.Any<CancellationToken>());
        }
    }

    private async Task AssertNotRevokedAsync(params Authorization[] authorizations)
    {
        foreach (var authorization in authorizations)
        {
            await _authorizationManager.DidNotReceive().TryRevokeAsync(authorization, Arg.Any<CancellationToken>());
            await _tokenManager.DidNotReceive().RevokeByAuthorizationIdAsync(authorization.Id, Arg.Any<CancellationToken>());
        }
    }

    private static ClaimsPrincipal Browser(string subject, string sid) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, subject), new Claim(BrowserSession.SessionIdClaim, sid)], "Identity.Application"));

    private static ClaimsPrincipal Hint(string subject, string sid, string authorizationId)
    {
        var identity = new ClaimsIdentity("OpenIddict.Server.AspNetCore");
        identity.SetClaim(Claims.Subject, subject).SetClaim(BrowserSession.SessionIdClaim, sid);
        var principal = new ClaimsPrincipal(identity);
        principal.SetAuthorizationId(authorizationId);
        return principal;
    }

    private static async IAsyncEnumerable<object> ToAsync(IEnumerable<object> items)
    {
        foreach (var item in items.ToList())
        {
            yield return item;
            await Task.Yield();
        }
    }

    private sealed record Authorization(string Id, string Subject, string Status, ImmutableDictionary<string, JsonElement> Properties);
}
