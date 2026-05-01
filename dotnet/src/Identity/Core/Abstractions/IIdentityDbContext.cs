using AQ.Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.Core.Abstractions;

public interface IIdentityDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<AuditEntry> AuditLog { get; }
    DbSet<SigningKey> SigningKeys { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
