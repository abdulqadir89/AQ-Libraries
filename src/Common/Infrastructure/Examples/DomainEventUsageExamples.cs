using AQ.Common.Application.Services;
using AQ.Common.Domain.Entities;
using AQ.Common.Domain.Events;
using AQ.Common.Infrastructure.Configurations;
using AQ.Common.Infrastructure.Extensions;
using AQ.Common.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

// Note: This is an example file - you may need to add Microsoft.EntityFrameworkCore.Relational
// NuGet package for ToTable() extension method if not already present

namespace AQ.Common.Infrastructure.Examples;

/// <summary>
/// Examples demonstrating how to use the outbox pattern and domain event dispatcher.
/// </summary>
public static class DomainEventUsageExamples
{
    /// <summary>
    /// Example of how to configure your DbContext to support outbox events.
    /// </summary>
    public class ExampleApplicationDbContext(
        DbContextOptions<ExampleApplicationDbContext> options,
        ICurrentUserService currentUserService,
        IDomainEventDispatcher domainEventDispatcher) : ApplicationDbContext<ExampleApplicationDbContext>(options, currentUserService, domainEventDispatcher)
    {

        // Your domain entity DbSets
        public DbSet<ExampleEntity> ExampleEntities { get; set; } = null!;

        // Required for outbox pattern - this should be in all contexts
        public DbSet<OutboxEvent> OutboxEvents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure outbox events
            modelBuilder.ApplyConfiguration(new OutboxEventConfiguration());

            // Configure concurrency tokens for all entities
            ConfigureConcurrencyTokens(modelBuilder);

            // Your other entity configurations...
            ConfigureExampleEntity(modelBuilder);
        }

        private static void ConfigureExampleEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExampleEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                // entity.ToTable("ExampleEntities"); // Uncomment if you have the Relational package
            });
        }
    }

    /// <summary>
    /// Example domain entity that raises events.
    /// </summary>
    public class ExampleEntity : AuditableEntity
    {
        public string Name { get; private set; } = string.Empty;

        private ExampleEntity() { } // For EF Core

        public ExampleEntity(string name, Guid? createdBy = null)
        {
            Name = name;

            // Raise domain event when entity is created
            AddDomainEvent(new EntityCreatedEvent(Id, nameof(ExampleEntity), createdBy));
        }

        public void UpdateName(string newName, Guid? updatedBy = null)
        {
            if (Name != newName)
            {
                var oldName = Name;
                Name = newName;

                // Raise domain event when entity is updated
                AddDomainEvent(new EntityUpdatedEvent(Id, nameof(ExampleEntity), updatedBy, new[] { nameof(Name) }));
            }
        }
    }

    /// <summary>
    /// Example service class showing how to use entities with domain events.
    /// </summary>
    public class ExampleService
    {
        private readonly ExampleApplicationDbContext _dbContext;

        public ExampleService(ExampleApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Creates a new example entity.
        /// Domain events will be automatically dispatched when SaveChanges is called.
        /// </summary>
        public async Task<ExampleEntity> CreateExampleAsync(string name, Guid? userId, CancellationToken cancellationToken = default)
        {
            // Create entity - this raises EntityCreatedEvent
            var entity = new ExampleEntity(name, userId);

            // Add to context
            _dbContext.ExampleEntities.Add(entity);

            // Save changes - this will automatically:
            // 1. Update auditable fields
            // 2. Dispatch domain events (either immediately or to outbox)
            // 3. Save the entity and events to database
            await _dbContext.SaveChangesAsync(cancellationToken);

            return entity;
        }

        /// <summary>
        /// Updates an existing entity.
        /// Domain events will be automatically dispatched when SaveChanges is called.
        /// </summary>
        public async Task UpdateExampleAsync(Guid entityId, string newName, Guid? userId, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.ExampleEntities.FindAsync(entityId);
            if (entity == null)
                throw new InvalidOperationException($"Entity with ID {entityId} not found");

            // Update entity - this may raise EntityUpdatedEvent if the name actually changes
            entity.UpdateName(newName, userId);

            // Save changes - domain events will be dispatched automatically
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>
/// Configuration examples for dependency injection.
/// </summary>
public static class ServiceConfigurationExamples
{
    /// <summary>
    /// Example of how to configure services for immediate domain event processing.
    /// Use this for simple applications where eventual consistency is not required.
    /// </summary>
    public static void ConfigureImmediateEventProcessing(IServiceCollection services)
    {
        // Configure immediate event processing
        services.AddImmediateDomainEventProcessing();

        // Register event handlers
        services.AddDomainEventHandlersFromAssembly(typeof(ServiceConfigurationExamples).Assembly);

        // Or register handlers individually
        // services.AddDomainEventHandler<EntityCreatedEvent, EntityCreatedEventHandler>();
    }

    /// <summary>
    /// Example of how to configure services for outbox pattern event processing.
    /// Use this for production applications that need reliable event delivery.
    /// </summary>
    public static void ConfigureOutboxEventProcessing(IServiceCollection services)
    {
        // Configure outbox pattern event processing
        services.AddOutboxDomainEventProcessing();

        // Register event handlers
        services.AddDomainEventHandlersFromAssembly(typeof(ServiceConfigurationExamples).Assembly);

        // The background service is automatically registered and will process outbox events
    }
}

/// <summary>
/// Migration guide for existing applications.
/// </summary>
public static class MigrationGuide
{
    /*
    ## Migration Steps for Existing Applications

    ### 1. Update Your DbContext
    
    ```csharp
    // Before
    public class YourDbContext : ApplicationDbContext
    {
        public YourDbContext(DbContextOptions<YourDbContext> options, ICurrentUserService currentUserService)
            : base(options, currentUserService)
        {
        }
    }

    // After
    public class YourDbContext : ApplicationDbContext
    {
        public YourDbContext(
            DbContextOptions<YourDbContext> options, 
            ICurrentUserService currentUserService,
            IDomainEventDispatcher domainEventDispatcher)
            : base(options, currentUserService, domainEventDispatcher)
        {
        }

        // Add this DbSet for outbox events
        public DbSet<OutboxEvent> OutboxEvents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Add this configuration
            modelBuilder.ApplyConfiguration(new OutboxEventConfiguration());
        }
    }
    ```

    ### 2. Create and Run Database Migration

    ```bash
    # Add migration for outbox events table
    dotnet ef migrations add AddOutboxEvents

    # Update database
    dotnet ef database update
    ```

    ### 3. Configure Services in Startup/Program.cs

    ```csharp
    // Choose one approach:

    // Option A: Immediate processing (simple, synchronous)
    services.AddImmediateDomainEventProcessing();

    // Option B: Outbox pattern (production-ready, reliable)
    services.AddOutboxDomainEventProcessing();

    // Register your event handlers
    services.AddDomainEventHandlersFromAssembly(typeof(Program).Assembly);
    ```

    ### 4. Create Event Handlers

    ```csharp
    public class YourCustomEventHandler : IDomainEventHandler<EntityCreatedEvent>
    {
        public async Task HandleAsync(EntityCreatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            // Your event handling logic here
        }
    }
    ```

    ### 5. Update Your Entities (Optional)

    Your entities already support domain events if they inherit from Entity.
    You can start raising events by calling AddDomainEvent():

    ```csharp
    public void SomeBusinessMethod()
    {
        // Your business logic...
        
        // Raise a domain event
        AddDomainEvent(new SomethingHappenedEvent(Id, "details"));
    }
    ```
    */
}
