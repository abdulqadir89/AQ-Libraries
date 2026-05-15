using AQ.Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.Core.Abstractions;

public interface IIdentityDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<AuditEntry> AuditLog { get; }
    DbSet<SigningKey> SigningKeys { get; }
    DbSet<UserClaim> StoredClaims { get; }
    DbSet<IdentityScope> IdentityScopes { get; }
    DbSet<ScopeClaimType> ScopeClaimTypes { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
