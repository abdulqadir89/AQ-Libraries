using System.Security.Claims;
using AQ.Actors;
using AQ.Identity.Core;
using AQ.Identity.Core.Auditing;
using AQ.Identity.Core.Entities;
using FluentAssertions;
using Xunit;

namespace AQ.Identity.Core.Tests;

public class AuditEntryTests
{
    [Theory]
    [InlineData(PrincipalKind.Human, AuditActorKind.Human)]
    [InlineData(PrincipalKind.Service, AuditActorKind.Service)]
    [InlineData(PrincipalKind.System, AuditActorKind.System)]
    public void Log_RecordsActorKindAndId(PrincipalKind kind, AuditActorKind expected)
    {
        var actorId = Guid.NewGuid();
        var subject = Guid.NewGuid();

        var entry = AuditEntry.Log(AuditEntry.Actions.UserActivated, new Actor(actorId, kind, ClientId: "c1"), subject);

        entry.ActorKind.Should().Be(expected);
        entry.ActorId.Should().Be(actorId);
        entry.SubjectUserId.Should().Be(subject);
        entry.ClientId.Should().Be("c1");
    }

    [Fact]
    public void LogAnonymous_HasNoActorId()
    {
        var subject = Guid.NewGuid();

        var entry = AuditEntry.LogAnonymous(AuditEntry.Actions.LoginFailed, subject, "1.2.3.4", "agent");

        entry.ActorKind.Should().Be(AuditActorKind.Anonymous);
        entry.ActorId.Should().BeNull();
        entry.SubjectUserId.Should().Be(subject);
        entry.IpAddress.Should().Be("1.2.3.4");
    }

    [Fact]
    public void Log_RejectsMissingActor()
    {
        var act = () => AuditEntry.Log(AuditEntry.Actions.KeyRotated, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Log_RejectsBlankAction()
    {
        var act = () => AuditEntry.Log(" ", new Actor(Guid.NewGuid(), PrincipalKind.System));

        act.Should().Throw<ArgumentException>();
    }
}

public class AuditActorsTests
{
    private static ClaimsPrincipal User(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));

    [Fact]
    public void Human_FromSubClaim()
    {
        var id = Guid.NewGuid();

        var actor = AuditActors.Require(User(new Claim("sub", id.ToString())));

        actor.Should().Be(new Actor(id, PrincipalKind.Human));
    }

    [Fact]
    public void Human_FromNameIdentifierClaim()
    {
        var id = Guid.NewGuid();

        AuditActors.Require(User(new Claim(ClaimTypes.NameIdentifier, id.ToString()))).PrincipalId.Should().Be(id);
    }

    [Fact]
    public void Service_FromPrincipalTypeClaim()
    {
        var id = Guid.NewGuid();

        var actor = AuditActors.Require(User(
            new Claim("sub", id.ToString()),
            new Claim(PrincipalClaims.Type, PrincipalClaims.Service),
            new Claim(PrincipalClaims.ClientId, "els-api")));

        actor.Should().Be(new Actor(id, PrincipalKind.Service, ClientId: "els-api"));
    }

    [Fact]
    public void NonGuidSubject_HasNoActor_AndRequireThrows()
    {
        var user = User(new Claim("sub", "els-api"));

        AuditActors.TryGet(user).Should().BeNull();
        var act = () => AuditActors.Require(user);
        act.Should().Throw<InvalidOperationException>();
    }
}

public class ReservedClaimPolicyTests
{
    [Theory]
    [InlineData("sub")]
    [InlineData("principal_type")]
    [InlineData("client_id")]
    [InlineData("SUB")]
    public void BaseTypes_AreRejected_WithNoRegistration(string type)
    {
        ReservedClaimPolicy.Create().Validate([type]).Should().NotBeNull();
    }

    [Fact]
    public void PermissionStyleClaims_AreAllowed()
    {
        ReservedClaimPolicy.Create(["app_internal_id"]).Validate(["app_system_permissions"]).Should().BeNull();
    }

    [Fact]
    public void AppClaim_IsAllowed_UntilRegistered()
    {
        ReservedClaimPolicy.Create().Validate(["app_internal_id"]).Should().BeNull();
        ReservedClaimPolicy.Create(["app_internal_id"]).Validate(["APP_INTERNAL_ID"]).Should().NotBeNull();
    }

    [Fact]
    public void RegisteredNames_NeverRemoveTheBaseSet()
    {
        var policy = ReservedClaimPolicy.Create(["app_internal_id"]);

        policy.Reserved.Should().Contain(PrincipalClaims.BaseReserved).And.Contain("app_internal_id");
    }

    [Theory]
    [InlineData(" sub")]
    [InlineData("client_id ")]
    [InlineData("\tprincipal_type")]
    public void SurroundingWhitespace_DoesNotSlipPastTheCheck(string type)
    {
        ReservedClaimPolicy.Create().Validate([type]).Should().NotBeNull();
    }

    [Fact]
    public void ReservedSet_CannotBeEditedThroughTheProperty()
    {
        var policy = ReservedClaimPolicy.Create();

        policy.Reserved.Should().NotBeOfType<HashSet<string>>();
        var act = () => ((ICollection<string>)policy.Reserved).Remove("sub");
        act.Should().Throw<NotSupportedException>();
        policy.IsReserved("sub").Should().BeTrue();
    }

    [Fact]
    public void BlankRegisteredName_Throws()
    {
        var act = () => ReservedClaimPolicy.Create([" "]);

        act.Should().Throw<ArgumentException>();
    }
}
