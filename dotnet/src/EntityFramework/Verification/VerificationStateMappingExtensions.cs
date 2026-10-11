using System.Linq.Expressions;
using System.Text.Json;
using AQ.Abstractions;
using AQ.Utilities.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AQ.EntityFramework.Verification;

public static class VerificationStateMappingExtensions
{
    public const string CountColumnName = "VerificationCount";
    public const string EntriesColumnName = "CurrentVerifications";

    private static readonly ValueConverter<List<VerificationEntry>, string> EntriesConverter = new(
        entries => JsonSerializer.Serialize(entries, (JsonSerializerOptions?)null),
        json => string.IsNullOrWhiteSpace(json)
            ? new List<VerificationEntry>()
            : JsonSerializer.Deserialize<List<VerificationEntry>>(json, (JsonSerializerOptions?)null) ?? new List<VerificationEntry>(),
        convertsNulls: true);

    private static readonly ValueComparer<List<VerificationEntry>> EntriesComparer = new(
        (left, right) => left!.SequenceEqual(right!, VerificationEntryComparer.Instance),
        entries => entries.Aggregate(0, (hash, entry) => HashCode.Combine(hash, entry.VerifiedByUserId, entry.VerifiedAt)),
        entries => entries.Select(entry => new VerificationEntry { VerifiedByUserId = entry.VerifiedByUserId, VerifiedAt = entry.VerifiedAt }).ToList());

    public static EntityTypeBuilder<TEntity> HasVerificationState<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, VerificationState>> property) where TEntity : class
    {
        builder.OwnsOne(property, owned =>
        {
            owned.Property(p => p.Count).HasColumnName(CountColumnName);
            owned.Property(p => p.Entries)
                .HasColumnName(EntriesColumnName)
                .HasConversion(EntriesConverter, EntriesComparer)
                .IsRequired(false);
        });
        return builder;
    }

    private sealed class VerificationEntryComparer : IEqualityComparer<VerificationEntry>
    {
        public static readonly VerificationEntryComparer Instance = new();

        public bool Equals(VerificationEntry? x, VerificationEntry? y) =>
            x is not null && y is not null && x.VerifiedByUserId == y.VerifiedByUserId && x.VerifiedAt == y.VerifiedAt;

        public int GetHashCode(VerificationEntry obj) => HashCode.Combine(obj.VerifiedByUserId, obj.VerifiedAt);
    }
}
