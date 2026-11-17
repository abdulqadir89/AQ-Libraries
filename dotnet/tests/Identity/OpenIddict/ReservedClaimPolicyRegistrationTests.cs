using AQ.Identity.Core;
using AQ.Identity.Core.Configuration;
using AQ.Identity.OpenIddict.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AQ.Identity.OpenIddict.Tests;

public class ReservedClaimPolicyRegistrationTests
{
    private static ServiceProvider Build(
        IEnumerable<string>? reserved = null,
        Action<IServiceCollection>? afterRegistration = null,
        Action<IServiceCollection>? beforeRegistration = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        beforeRegistration?.Invoke(services);
        services.AddDbContext<TestIdentityDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddAqIdentity<TestIdentityDbContext>(
            new AqIdentityOptions
            {
                Issuer = "https://identity.example.com",
                ReservedServiceAccountClaims = [.. reserved ?? []],
            },
            []);
        afterRegistration?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void RegisteredPolicy_IncludesTheAppsReservedNames()
    {
        using var provider = Build(["app_internal_id"]);

        var policy = provider.GetRequiredService<ReservedClaimPolicy>();

        policy.IsReserved("app_internal_id").Should().BeTrue();
        policy.IsReserved("sub").Should().BeTrue();
    }

    [Fact]
    public void RegisteredPolicy_FollowsLaterOptionsConfiguration()
    {
        using var provider = Build(afterRegistration: s =>
            s.Configure<AqIdentityOptions>(o => o.ReservedServiceAccountClaims.Add("late_claim")));

        provider.GetRequiredService<ReservedClaimPolicy>().IsReserved("late_claim").Should().BeTrue();
    }

    [Fact]
    public void NamesBoundBeforeRegistration_AreKept_NotOverwritten()
    {
        using var provider = Build(
            ["registered_claim"],
            beforeRegistration: s => s.Configure<AqIdentityOptions>(o => o.ReservedServiceAccountClaims.Add("bound_claim")));

        var policy = provider.GetRequiredService<ReservedClaimPolicy>();

        policy.IsReserved("bound_claim").Should().BeTrue();
        policy.IsReserved("registered_claim").Should().BeTrue();
    }

    [Fact]
    public void BlankReservedName_FailsWithTheValidatorMessage()
    {
        using var provider = Build([" "]);

        var act = () => provider.GetRequiredService<ReservedClaimPolicy>();

        act.Should().Throw<OptionsValidationException>().WithMessage("*ReservedServiceAccountClaims*");
    }
}
