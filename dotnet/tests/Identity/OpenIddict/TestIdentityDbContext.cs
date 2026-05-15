using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Tests;

public class TestIdentityDbContext(DbContextOptions<TestIdentityDbContext> options)
    : DbContext(options), IIdentityDbContext
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();
    public DbSet<SigningKey> SigningKeys => Set<SigningKey>();
    public DbSet<UserClaim> StoredClaims => Set<UserClaim>();
    public DbSet<IdentityScope> IdentityScopes => Set<IdentityScope>();
    public DbSet<ScopeClaimType> ScopeClaimTypes => Set<ScopeClaimType>();

    public new Task<int> SaveChangesAsync(CancellationToken ct = default) => base.SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().HasKey(u => u.Id);
        builder.Entity<AuditEntry>().HasKey(e => e.Id);
        builder.Entity<SigningKey>().HasKey(k => k.Id);
        builder.Entity<UserClaim>().HasKey(c => c.Id);
        builder.Entity<IdentityScope>().HasKey(s => s.Id);
        builder.Entity<ScopeClaimType>().HasKey(sct => sct.Id);
        builder.Entity<ScopeClaimType>()
            .HasOne(sct => sct.Scope)
            .WithMany(s => s.ClaimTypes)
            .HasForeignKey(sct => sct.ScopeId);
    }

    public static TestIdentityDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestIdentityDbContext(options);
    }
}
