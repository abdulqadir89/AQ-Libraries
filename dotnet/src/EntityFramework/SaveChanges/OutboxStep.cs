using System.Text.Json;
using AQ.Events.Domain;
using AQ.Events.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class OutboxStep
{
    public static void Run(DbContext context, Guid? currentUserId)
    {
        var entitiesWithDomainEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithDomainEvents.Count == 0)
        {
            return;
        }

        foreach (var entity in entitiesWithDomainEvents)
        {
            foreach (var domainEvent in entity.DomainEvents.ToList())
            {
                domainEvent.UserId = currentUserId;
                var eventType = domainEvent.GetType().AssemblyQualifiedName!;
                var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

                context.Set<OutboxEvent>().Add(new OutboxEvent(
                    OutboxEventCategory.Domain,
                    eventType,
                    eventData,
                    DateTimeOffset.UtcNow));
            }

            entity.ClearDomainEvents();
        }
    }
}
