using System.Text.Json;
using AQ.Actors;
using AQ.Events.Domain;
using AQ.Events.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class OutboxStep
{
    public static void Run(DbContext context, Actor? actor, JsonSerializerOptions? json)
    {
        var entitiesWithDomainEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithDomainEvents.Count == 0)
        {
            return;
        }

        // Lifecycle hooks may have raised events after the actor check.
        if (actor is null)
        {
            throw new MissingActorException();
        }

        foreach (var entity in entitiesWithDomainEvents)
        {
            foreach (var domainEvent in entity.DomainEvents.ToList())
            {
                domainEvent.ActorId = actor.PrincipalId;
                domainEvent.OnBehalfOfId = actor.OnBehalfOfId;
                domainEvent.CorrelationId = actor.CorrelationId;
                var eventType = domainEvent.GetType().AssemblyQualifiedName!;
                var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), json);

                context.Set<OutboxEvent>().Add(new OutboxEvent(
                    OutboxEventCategory.Domain,
                    eventType,
                    eventData,
                    DateTimeOffset.UtcNow,
                    actor));
            }

            entity.ClearDomainEvents();
        }
    }
}
