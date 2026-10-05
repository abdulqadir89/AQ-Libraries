using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using AQ.Identity.OpenIddict.Sessions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Xunit;

namespace AQ.Identity.OpenIddict.Tests;

public class BackchannelLogoutNotifierTests
{
    private const string Issuer = "https://id.example.com/";
    private const string ClientId = "web";
    private const string LogoutUri = "https://web.example.com/api/auth/backchannel-logout";

    private readonly IOpenIddictApplicationManager _applicationManager = Substitute.For<IOpenIddictApplicationManager>();
    private readonly RecordingHandler _http = new();
    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "k1" };
    private readonly BackchannelLogoutNotifier _notifier;

    public BackchannelLogoutNotifierTests()
    {
        var options = new OpenIddictServerOptions { Issuer = new Uri(Issuer) };
        options.SigningCredentials.Add(new SigningCredentials(_key, SecurityAlgorithms.RsaSha256));
        var optionsMonitor = Substitute.For<IOptionsMonitor<OpenIddictServerOptions>>();
        optionsMonitor.CurrentValue.Returns(options);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(_http, disposeHandler: false));

        _notifier = new BackchannelLogoutNotifier(_applicationManager, optionsMonitor, httpClientFactory, Substitute.For<ILogger<BackchannelLogoutNotifier>>());
    }

    [Fact]
    public async Task CreateLogoutToken_HasTheClaimsTheSpecRequires()
    {
        var token = _notifier.CreateLogoutToken(ClientId, new BackchannelLogoutTarget("app", "user-1", "sid-1"));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = ClientId,
            IssuerSigningKey = _key,
            ValidTypes = [BackchannelLogout.TokenType],
        });

        result.IsValid.Should().BeTrue(result.Exception?.Message);
        var jwt = (JsonWebToken)result.SecurityToken;
        jwt.Subject.Should().Be("user-1");
        jwt.GetClaim("sid").Value.Should().Be("sid-1");
        jwt.TryGetClaim("jti", out _).Should().BeTrue();
        jwt.TryGetClaim("iat", out _).Should().BeTrue();
        jwt.TryGetClaim("nonce", out _).Should().BeFalse();

        var events = JsonDocument.Parse(jwt.GetPayloadValue<JsonElement>("events").GetRawText()).RootElement;
        events.TryGetProperty(BackchannelLogout.EventType, out var evt).Should().BeTrue();
        evt.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task NotifyAsync_PostsTheLogoutTokenToTheClient()
    {
        RegisterClient(LogoutUri, sessionRequired: true);

        await _notifier.NotifyAsync([new BackchannelLogoutTarget("app", "user-1", "sid-1")], CancellationToken.None);

        _http.Requests.Should().ContainSingle();
        var (uri, body) = _http.Requests[0];
        uri.Should().Be(LogoutUri);
        body.Should().StartWith("logout_token=");
    }

    [Fact]
    public async Task NotifyAsync_SkipsClientsWithoutAUri_AndSessionRequiredClientsWithoutSid()
    {
        RegisterClient(uri: null, sessionRequired: false);
        await _notifier.NotifyAsync([new BackchannelLogoutTarget("app", "user-1", "sid-1")], CancellationToken.None);

        RegisterClient(LogoutUri, sessionRequired: true);
        await _notifier.NotifyAsync([new BackchannelLogoutTarget("app", "user-1", SessionId: null)], CancellationToken.None);

        _http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyAsync_SurvivesAnUnreachableClient()
    {
        RegisterClient(LogoutUri, sessionRequired: false);
        _http.Fail = true;

        var act = () => _notifier.NotifyAsync([new BackchannelLogoutTarget("app", "user-1", "sid-1")], CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private void RegisterClient(string? uri, bool sessionRequired)
    {
        var application = new object();
        var properties = ImmutableDictionary<string, JsonElement>.Empty;
        if (uri is not null)
        {
            properties = properties
                .Add(BackchannelLogout.UriProperty, JsonSerializer.SerializeToElement(uri))
                .Add(BackchannelLogout.SessionRequiredProperty, JsonSerializer.SerializeToElement(sessionRequired));
        }

        _applicationManager.FindByIdAsync("app", Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>(application));
        _applicationManager.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(ClientId));
        _applicationManager.GetPropertiesAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<ImmutableDictionary<string, JsonElement>>(properties));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(string Uri, string Body)> Requests { get; } = [];
        public bool Fail { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Fail) throw new HttpRequestException("unreachable");
            Requests.Add((request.RequestUri!.ToString(), await request.Content!.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
