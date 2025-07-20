# Domain Events and Outbox Pattern Implementation

This implementation provides a robust domain event system with support for both immediate processing and the outbox pattern for reliable event delivery.

## Overview

The domain event system includes:

1. **Domain Event Interfaces** (`IDomainEvent`, `IDomainEventHandler<T>`, `IDomainEventDispatcher`)
2. **Outbox Pattern Implementation** for reliable event processing
3. **Immediate Processing** for simple scenarios  
4. **Background Processing Service** for outbox events
5. **Entity Framework Integration** with automatic event dispatching

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Domain Layer  │    │ Application     │    │ Infrastructure  │
│                 │    │ Layer           │    │ Layer           │
├─────────────────┤    ├─────────────────┤    ├─────────────────┤
│ IDomainEvent    │    │ IApplicationDb  │    │ ApplicationDb   │
│ IDomainEventHa* │    │ Context         │    │ Context         │
│ Entity          │    │                 │    │                 │
│ OutboxEvent     │    │                 │    │ DomainEvent     │
│                 │    │                 │    │ Dispatcher      │
│                 │    │                 │    │                 │
│                 │    │                 │    │ OutboxEvent     │
│                 │    │                 │    │ Processor       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Quick Start

### 1. Choose Your Processing Strategy

#### Option A: Immediate Processing (Simple)
```csharp
// In Program.cs or Startup.cs
services.AddImmediateDomainEventProcessing();
services.AddDomainEventHandlersFromAssembly(typeof(Program).Assembly);
```

#### Option B: Outbox Pattern (Recommended for Production)
```csharp
// In Program.cs or Startup.cs
services.AddOutboxDomainEventProcessing();
services.AddDomainEventHandlersFromAssembly(typeof(Program).Assembly);
```

### 2. Update Your DbContext

```csharp
public class YourDbContext : ApplicationDbContext
{
    public YourDbContext(
        DbContextOptions<YourDbContext> options,
        ICurrentUserService currentUserService,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, currentUserService, domainEventDispatcher)
    {
    }

    // Required for outbox pattern
    public DbSet<OutboxEvent> OutboxEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure outbox events
        modelBuilder.ApplyConfiguration(new OutboxEventConfiguration());
        
        // Configure concurrency tokens
        ConfigureConcurrencyTokens(modelBuilder);
    }
}
```

### 3. Create Database Migration

```bash
dotnet ef migrations add AddOutboxEvents
dotnet ef database update
```

### 4. Raise Domain Events in Your Entities

```csharp
public class Customer : AuditableEntity
{
    public string Name { get; private set; }

    public void UpdateName(string newName, Guid updatedBy)
    {
        if (Name != newName)
        {
            Name = newName;
            
            // Raise domain event
            AddDomainEvent(new EntityUpdatedEvent(Id, nameof(Customer), updatedBy));
        }
    }
}
```

### 5. Create Event Handlers

```csharp
public class CustomerUpdatedHandler : IDomainEventHandler<EntityUpdatedEvent>
{
    private readonly ILogger<CustomerUpdatedHandler> _logger;

    public CustomerUpdatedHandler(ILogger<CustomerUpdatedHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(EntityUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Customer {Id} was updated", domainEvent.AggregateId);
        
        // Your business logic here:
        // - Send notifications
        // - Update read models  
        // - Trigger integrations
        // - Update caches
    }
}
```

## Processing Strategies

### Immediate Processing

- **When to use**: Simple applications, development, testing
- **Pros**: Simple, synchronous, easy to debug
- **Cons**: No fault tolerance, events lost if handler fails

```csharp
services.AddImmediateDomainEventProcessing();
```

Events are processed immediately when `SaveChanges()` is called, within the same transaction.

### Outbox Pattern

- **When to use**: Production applications, when reliability is important
- **Pros**: Transactional, fault-tolerant, retry logic, eventual consistency
- **Cons**: More complex, eventual processing, requires background service

```csharp
services.AddOutboxDomainEventProcessing();
```

Events are stored in the database and processed by a background service with:
- Automatic retry with exponential backoff
- Error handling and logging
- Maximum retry limits
- Transactional consistency

## Configuration Options

### Background Service Settings

The `OutboxEventProcessorService` can be configured by modifying the constructor parameters:

```csharp
public class OutboxEventProcessorService : BackgroundService
{
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30);
    private readonly int _maxRetryAttempts = 5;
    private readonly TimeSpan _baseRetryDelay = TimeSpan.FromMinutes(1);
}
```

### Outbox Event Entity

The `OutboxEvent` entity tracks:
- `EventType`: Fully qualified type name
- `EventData`: JSON serialized event data  
- `OccurredOn`: When the event was raised
- `ProcessedOn`: When successfully processed
- `IsProcessed`: Processing status
- `ProcessingAttempts`: Retry count
- `LastError`: Last error message
- `NextProcessingAttempt`: When to retry next

## Database Schema

The outbox events table includes these indexes for optimal performance:

```sql
-- For finding unprocessed events
CREATE INDEX IX_OutboxEvents_IsProcessed_OccurredOn 
ON OutboxEvents (IsProcessed, OccurredOn);

-- For retry processing
CREATE INDEX IX_OutboxEvents_NextProcessingAttempt 
ON OutboxEvents (NextProcessingAttempt) 
WHERE NextProcessingAttempt IS NOT NULL;

-- For monitoring failed events
CREATE INDEX IX_OutboxEvents_IsProcessed_ProcessingAttempts 
ON OutboxEvents (IsProcessed, ProcessingAttempts);
```

## Event Handler Registration

### Automatic Registration

Register all handlers from an assembly:
```csharp
services.AddDomainEventHandlersFromAssembly(typeof(Program).Assembly);
```

### Manual Registration  

Register individual handlers:
```csharp
services.AddDomainEventHandler<EntityCreatedEvent, EntityCreatedEventHandler>();
```

## Monitoring and Troubleshooting

### Logging

The implementation includes comprehensive logging:
- Event dispatching (Debug level)
- Processing success/failure (Info/Error level)
- Handler execution (Debug level)
- Retry attempts (Error level)

### Monitoring Queries

Find failed events:
```sql
SELECT * FROM OutboxEvents 
WHERE IsProcessed = 0 AND ProcessingAttempts >= 5;
```

Find events ready for retry:
```sql
SELECT * FROM OutboxEvents 
WHERE IsProcessed = 0 AND NextProcessingAttempt <= GETUTCDATE();
```

Monitor processing performance:
```sql
SELECT 
    CAST(OccurredOn AS DATE) as EventDate,
    COUNT(*) as TotalEvents,
    SUM(CASE WHEN IsProcessed = 1 THEN 1 ELSE 0 END) as ProcessedEvents,
    AVG(CAST(ProcessingAttempts AS FLOAT)) as AvgAttempts
FROM OutboxEvents 
GROUP BY CAST(OccurredOn AS DATE)
ORDER BY EventDate DESC;
```

## Best Practices

1. **Keep handlers idempotent** - they may be called multiple times
2. **Keep handlers fast** - avoid long-running operations
3. **Use logging** - for monitoring and troubleshooting
4. **Handle exceptions gracefully** - let the retry mechanism work
5. **Monitor outbox events** - set up alerts for failed events
6. **Clean up processed events** - archive/delete old events periodically

## Common Patterns

### Notification Handler
```csharp
public class NotificationHandler : IDomainEventHandler<EntityCreatedEvent>
{
    public async Task HandleAsync(EntityCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Send email, SMS, push notification, etc.
    }
}
```

### Read Model Updater
```csharp
public class ReadModelUpdater : IDomainEventHandler<EntityUpdatedEvent>  
{
    public async Task HandleAsync(EntityUpdatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Update denormalized views, search indexes, etc.
    }
}
```

### Integration Event Publisher  
```csharp
public class IntegrationEventPublisher : IDomainEventHandler<EntityCreatedEvent>
{
    public async Task HandleAsync(EntityCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Publish to service bus, message queue, webhook, etc.
    }
}
```

## Migration from Existing Code

See `DomainEventUsageExamples.cs` for detailed migration steps and examples.

The implementation is backward compatible - existing entities that inherit from `Entity` already support domain events and will work without changes once you configure the dispatcher.
