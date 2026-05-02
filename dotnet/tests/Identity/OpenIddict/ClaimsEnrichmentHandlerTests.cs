using AQ.Identity.Core.Abstractions;
using AQ.Identity.OpenIddict.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Security.Claims;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Tests;

public class ClaimsEnrichmentHandlerTests
{
    private readonly IClaimsEnricher _enricher;
    private readonly ILogger<ClaimsEnrichmentHandler> _logger;
    private readonly ClaimsEnrichmentHandler _handler;

    public ClaimsEnrichmentHandlerTests()
    {
        _enricher = Substitute.For<IClaimsEnricher>();
        _logger = Substitute.For<ILogger<ClaimsEnrichmentHandler>>();
        _handler = new ClaimsEnrichmentHandler(_enricher, _logger);
    }

    private OpenIddictServerEvents.ProcessSignInContext CreateContext(ClaimsPrincipal? principal = null)
    {
        var context = Substitute.For<OpenIddictServerEvents.ProcessSignInContext>();
        context.Principal.Returns(principal);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task HandleAsync_WithValidPrincipalAndEnrichedClaims_MergesClaimsIntoPrincipal()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var clientId = "test-client";
        var scopes = new[] { "openid", "profile" };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subjectId.ToString()),
            new Claim(Claims.ClientId, clientId),
            new Claim(Claims.Scope, scopes[0]),
            new Claim(Claims.Scope, scopes[1])
        }));

        var enrichedClaims = new Dictionary<string, string>
        {
            { "custom_claim", "custom_value" },
            { "org", "acme" }
        };

        _enricher.EnrichAsync(subjectId, clientId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(enrichedClaims);

        var context = CreateContext(principal);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        principal.FindFirst("custom_claim")?.Value.Should().Be("custom_value");
        principal.FindFirst("org")?.Value.Should().Be("acme");
    }

    [Fact]
    public async Task HandleAsync_WhenEnricherThrows_LogsWarningAndContinuesWithoutExtraClaimsAsync()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var clientId = "test-client";

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subjectId.ToString()),
            new Claim(Claims.ClientId, clientId),
            new Claim(Claims.Scope, "openid")
        }));

        var exception = new InvalidOperationException("Enricher failed");
        _enricher.EnrichAsync(subjectId, clientId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        var context = CreateContext(principal);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Claims enrichment failed")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());

        // Principal should remain unchanged (no extra claims added)
        principal.FindAll("custom_claim").Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithNullPrincipal_ReturnsWithoutProcessing()
    {
        // Arrange
        var context = CreateContext(null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        await _enricher.DidNotReceive().EnrichAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithMissingSubjectClaim_ReturnsWithoutProcessing()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(Claims.ClientId, "test-client"),
            new Claim(Claims.Scope, "openid")
        }));

        var context = CreateContext(principal);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        await _enricher.DidNotReceive().EnrichAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidSubjectFormat_ReturnsWithoutProcessing()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
            new Claim(Claims.ClientId, "test-client"),
            new Claim(Claims.Scope, "openid")
        }));

        var context = CreateContext(principal);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        await _enricher.DidNotReceive().EnrichAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithMissingClientId_ReturnsWithoutProcessing()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subjectId.ToString()),
            new Claim(Claims.Scope, "openid")
        }));

        var context = CreateContext(principal);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        await _enricher.DidNotReceive().EnrichAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNoScopes_PassesEmptyListToEnricher()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var clientId = "test-client";

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subjectId.ToString()),
            new Claim(Claims.ClientId, clientId)
        }));

        var enrichedClaims = new Dictionary<string, string> { { "role", "user" } };
        _enricher.EnrichAsync(subjectId, clientId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(enrichedClaims);

        var context = CreateContext(principal);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        await _enricher.Received(1).EnrichAsync(
            subjectId,
            clientId,
            Arg.Is<IEnumerable<string>>(s => !s.Any()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithSubClaimAlternative_UsesSubClaimWhenNameIdentifierMissing()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var clientId = "test-client";

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", subjectId.ToString()),
            new Claim(Claims.ClientId, clientId),
            new Claim(Claims.Scope, "openid")
        }));

        var enrichedClaims = new Dictionary<string, string> { { "role", "admin" } };
        _enricher.EnrichAsync(subjectId, clientId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(enrichedClaims);

        var context = CreateContext(principal);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        principal.FindFirst("role")?.Value.Should().Be("admin");
    }

    [Fact]
    public async Task HandleAsync_WithThrowArgumentNull_DoesNotThrow()
    {
        // Arrange
        var context = CreateContext(null);

        // Act & Assert
        var act = async () => await _handler.HandleAsync(context);
        await act.Should().NotThrowAsync();
    }
}
