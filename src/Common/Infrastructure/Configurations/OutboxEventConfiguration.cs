using AQ.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AQ.Common.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for the OutboxEvent entity.
/// </summary>
public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");

        // Primary key is inherited from Entity base class
        
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.EventData)
            .IsRequired()
            .HasColumnType("nvarchar(max)"); // Large text field for JSON data

        builder.Property(e => e.OccurredOn)
            .IsRequired();

        builder.Property(e => e.ProcessedOn)
            .IsRequired(false);

        builder.Property(e => e.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ProcessingAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(e => e.NextProcessingAttempt)
            .IsRequired(false);

        // Indexes for efficient querying
        builder.HasIndex(e => new { e.IsProcessed, e.OccurredOn })
            .HasDatabaseName("IX_OutboxEvents_IsProcessed_OccurredOn");

        builder.HasIndex(e => e.NextProcessingAttempt)
            .HasDatabaseName("IX_OutboxEvents_NextProcessingAttempt")
            .HasFilter("NextProcessingAttempt IS NOT NULL");

        builder.HasIndex(e => new { e.IsProcessed, e.ProcessingAttempts })
            .HasDatabaseName("IX_OutboxEvents_IsProcessed_ProcessingAttempts");
    }
}
