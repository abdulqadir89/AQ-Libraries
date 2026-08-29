using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Management.Endpoints.Users;
using FluentAssertions;
using Xunit;

namespace AQ.Identity.OpenIddict.Tests;

public class AdminClaimGuardTests : IDisposable
{
    private readonly TestIdentityDbContext _context;

    public AdminClaimGuardTests()
    {
        _context = TestIdentityDbContext.Create();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WouldRemoveLastAdmin_WithNoOtherAdmins_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _context.StoredClaims.Add(UserClaim.Create(userId, "manage_api", "true"));
        await _context.SaveChangesAsync();

        var result = await AdminClaimGuard.WouldRemoveLastAdminAsync(_context, userId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WouldRemoveLastAdmin_WithAnotherAdmin_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _context.StoredClaims.Add(UserClaim.Create(userId, "manage_api", "true"));
        _context.StoredClaims.Add(UserClaim.Create(otherUserId, "manage_api", "true"));
        await _context.SaveChangesAsync();

        var result = await AdminClaimGuard.WouldRemoveLastAdminAsync(_context, userId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task WouldRemoveLastAdmin_WithNoAdminsAtAll_ReturnsTrue()
    {
        var userId = Guid.NewGuid();

        var result = await AdminClaimGuard.WouldRemoveLastAdminAsync(_context, userId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WouldRemoveLastAdmin_IgnoresOtherClaimTypesOnOtherUsers()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _context.StoredClaims.Add(UserClaim.Create(userId, "manage_api", "true"));
        _context.StoredClaims.Add(UserClaim.Create(otherUserId, "some_other_claim", "value"));
        await _context.SaveChangesAsync();

        var result = await AdminClaimGuard.WouldRemoveLastAdminAsync(_context, userId, CancellationToken.None);

        result.Should().BeTrue();
    }
}
