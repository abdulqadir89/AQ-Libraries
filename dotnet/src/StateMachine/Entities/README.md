# State Machine Transition Requirements System

## Overview

The State Machine system supports requirements that must be evaluated before allowing transitions. Requirements are stored as JSON in the database with flexible handler architecture. The system uses entity-based operations throughout.

## Key Concepts

### Requirements
- **AND Relationship**: ALL requirements must be fulfilled for a transition to proceed
- **JSON Storage**: Requirements are stored as JSON for flexibility
- **Type-Safe**: Strong typing through interfaces while maintaining database flexibility
- **Handler Evaluation**: Two-phase evaluation with specific handlers first, then multiple generic handlers
- **Type-Based Discovery**: Uses `typeof(T).Name` instead of manual RequirementType strings

### Handlers
- **Specific Handlers**: Implement `IStateMachineTransitionRequirementHandler<TRequirement>` for typed requirements
- **Generic Handlers**: Implement `IStateMachineTransitionHandler` to process all requirements (multiple supported)
- **Evaluation Sequence**: System first tries specific handlers, then evaluates all registered generic handlers
- **Auto-Discovery**: Handlers are automatically discovered from configured assemblies

### Requirement Data
- **User Data Collection**: Requirements can specify that additional user data must be collected before evaluation
- **Marker Interface**: Implement `IStateMachineTransitionRequirementData` on entities that store required user data
- **Handler Fetches Data**: Handlers fetch required data directly from the database using StateMachineId and TransitionId
- **Discovery**: Use `GetRequiredDataTypes()` on transitions to discover what data needs to be collected
- **Database-Backed**: Data entities are persisted in your application database with normal EF Core configuration

### Entity-Based Operations
- **States**: Use `StateMachineState` entities instead of string names
- **Triggers**: Use `StateMachineTrigger` entities instead of string names
- **Consistent API**: All operations use proper entity references

## Architecture

### Domain Layer
- `IStateMachineTransitionRequirement` - Base requirement interface
- `StateMachineTransitionRequirement` - Abstract base class with common properties and `GetRequiredDataType()` method
- `IStateMachineTransitionRequirementHandler<TRequirement>` - Typed handler interface
- `IStateMachineTransitionHandler` - Generic handler interface for processing all requirements
- `IStateMachineTransitionRequirementData` - Marker interface for requirement data entities
- `RequirementEvaluationStatus` - Tracks evaluation status and handler processing
- Example requirements in `Requirements/ExampleRequirements.cs`

### Application Layer
- `IStateMachineTransitionService` - Main service for executing transitions (entity-based)
- `IStateMachineRequirementEvaluationService` - Service for evaluating requirements

### Infrastructure Layer
- JSON storage configuration for requirements in EF Core
- Custom value converters for requirement serialization

## Usage

### Defining Requirements

Requirements automatically use their class name as the type identifier:

```csharp
public class UserRoleRequirement : StateMachineTransitionRequirement
{
    public string RequiredRole { get; set; } = default!;
    public bool AllowSuperUser { get; set; } = true;
}
```

The requirement type is automatically determined by `typeof(UserRoleRequirement).Name`.

### Adding Requirements to Transitions

```csharp
var requirements = new List<IStateMachineTransitionRequirement>
{
    new UserRoleRequirement { RequiredRole = "Manager", Description = "Requires manager role" },
    new ApprovalRequirement { MinimumApprovals = 2, RequiredApprovers = ["user1", "user2"] }
};

definition.AddTransition(fromState, toState, trigger, requirements);
```

### Creating Requirement Handlers

#### Specific Handler (for typed requirements)
```csharp
public class UserRoleRequirementHandler : IStateMachineTransitionRequirementHandler<UserRoleRequirement>
{
    public async Task<bool> HandleAsync(
        UserRoleRequirement requirement, 
        Guid stateMachineId, 
        object? requirementContext)
    {
        // Implementation logic here
        return userHasRole;
    }
}
```

#### Handler that Fetches User-Provided Data
```csharp
public class ApprovalJustificationRequirementHandler(IApplicationDbContext dbContext) 
    : IStateMachineTransitionRequirementHandler<ApprovalJustificationRequirement>
{
    public async Task<bool> HandleAsync(
        ApprovalJustificationRequirement requirement,
        Guid stateMachineId,
        object? requirementContext)
    {
        // Fetch the user-provided data from database
        var justificationData = await dbContext.ApprovalJustifications
            .FirstOrDefaultAsync(aj => 
                aj.StateMachineId == stateMachineId && 
                aj.TransitionId == requirement.TransitionId);

        // If data hasn't been collected yet, requirement not fulfilled
        if (justificationData == null)
            return false;

        // Validate the data
        if (justificationData.Justification.Length < requirement.MinimumCharacters)
            return false;

        if (requirement.RequiresSupportingDocuments && !justificationData.SupportingDocumentUrls.Any())
            return false;

        // Additional business logic
        return true;
    }
}
```

#### Generic Handler (processes all requirements with status)
```csharp
public class AdminOverrideHandler : IStateMachineTransitionHandler
{
    public async Task<IEnumerable<RequirementEvaluationStatus>> HandleAsync(
        IEnumerable<RequirementEvaluationStatus> requirements,
        StateMachineInstance stateMachine,
        IDictionary<string, object>? context)
    {
        var statuses = requirements.ToList();
        
        foreach (var status in statuses.Where(r => !r.IsFulfilled))
        {
            // Admin override logic
            if (context?.TryGetValue("UserRoles", out var rolesObj) == true &&
                rolesObj is IEnumerable<string> roles &&
                roles.Contains("Admin"))
            {
                status.IsFulfilled = true;
                status.HandlerUsed = "AdminOverrideHandler";
            }
        }
        
        return statuses;
    }
}
```

#### Multiple Generic Handlers
You can register multiple generic handlers that will all be evaluated:

```csharp
public class LoggingHandler : IStateMachineTransitionHandler
{
    public async Task<IEnumerable<RequirementEvaluationStatus>> HandleAsync(
        IEnumerable<RequirementEvaluationStatus> requirements,
        StateMachineInstance stateMachine,
        IDictionary<string, object>? context)
    {
        // Log requirement evaluation attempts
        foreach (var status in requirements)
        {
            Log.Information("Requirement {Type} evaluated: {Fulfilled}", 
                status.Requirement.GetType().Name, status.IsFulfilled);
        }
        
        return requirements; // Return unchanged
    }
}
```

### Using the Transition Service (Entity-Based)

```csharp
// Get entities
var toState = await GetStateMachineStateAsync(stateMachineId, "Approved");
var trigger = await GetStateMachineTriggerAsync(stateMachineId, "Approve");

// Execute transition
var result = await transitionService.TryTransitionAsync(
    stateMachine, 
    trigger, 
    currentUser, 
    context);

if (result.IsSuccess)
{
    // Transition succeeded
    var transitionResult = result.Value;
}

// Force transition (bypasses requirements)
var forceResult = await transitionService.ForceTransitionAsync(
    stateMachine,
    toState,
    currentUser,
    context);
```

## Handler Evaluation Sequence

1. **Specific Handlers First**: System attempts to find and execute `IStateMachineTransitionRequirementHandler<T>` for each requirement
2. **Generic Handler Processing**: `IStateMachineTransitionHandler` processes all requirements with their current status
3. **Status Tracking**: `RequirementEvaluationStatus` tracks which requirements were processed by specific handlers

```csharp
public class RequirementEvaluationStatus
{
    public bool IsFulfilled { get; set; }
    public bool WasProcessedBySpecificHandler { get; set; }
    public Dictionary<string, object> RequirementData { get; set; } = new();
}
```

## Database Schema

Requirements are stored as JSON in the `RequirementsJson` column of the `StateMachineTransition` table:

```json
[
  {
    "RequirementType": "UserRole",
    "RequiredRole": "Manager",
    "AllowSuperUser": true,
    "Description": "Requires manager role"
  }
]
```

## Example Requirements

The system includes several example requirement types:

- **UserRoleRequirement**: Checks user roles
- **FieldValueRequirement**: Validates field values
- **CollectionRequirement**: Validates collections
- **ApprovalRequirement**: Handles approval workflows
- **BusinessHoursRequirement**: Time-based restrictions

## Benefits

1. **Entity-Based**: Clean API using proper entities instead of strings
2. **Type-Safe Discovery**: Uses `typeof(T).Name` instead of manual string properties
3. **Multiple Generic Handlers**: Support for multiple generic handlers that all get evaluated
4. **Flexible Assembly Scanning**: Configurable assemblies for handler discovery
5. **Auto-Registration**: Automatic handler discovery and registration
6. **Simple Data Collection**: Requirements signal what data is needed, handlers fetch it
7. **No Extra Layers**: Handlers fetch data directly from database - no service abstraction needed
8. **Extensibility**: Easy to add new requirement types and handlers
9. **Separation of Concerns**: Business logic separated from state machine mechanics
10. **Type Safety**: Strong typing with runtime flexibility
11. **Testability**: Requirements and handlers can be unit tested independently

## Requirement Data Collection

### Overview

Some requirements need user-provided data before they can be evaluated. The system uses a simple pattern:
1. Requirements override `GetRequiredDataTypes()` to specify what data entity types are needed (can be multiple)
2. Data entities implement `IStateMachineTransitionRequirementData` marker interface
3. Application queries `transition.GetRequiredDataTypes()` to discover what data to collect
4. Handlers fetch the data directly from database during evaluation

### Defining a Requirement that Needs Data

```csharp
// Requirement can specify one or multiple data entity types
public record ApprovalJustificationRequirement : StateMachineTransitionRequirement
{
    public int MinimumCharacters { get; init; } = 50;
    public bool RequiresSupportingDocuments { get; init; } = false;

    // Override to specify the data entity types (can return multiple)
    public override IEnumerable<Type> GetRequiredDataTypes() => 
        new[] { typeof(ApprovalJustification), typeof(ApprovalAttachments) };
}

// Or single data type
public record ReviewCommentRequirement : StateMachineTransitionRequirement
{
    public override IEnumerable<Type> GetRequiredDataTypes() => 
        new[] { typeof(ReviewComment) };
}
```

### Defining the Data Entity

```csharp
// Data entity implements marker interface
public class ApprovalJustification : Entity, IStateMachineTransitionRequirementData
{
    public Guid StateMachineId { get; private set; }
    public Guid TransitionId { get; private set; }
    public string Justification { get; private set; } = default!;
    public List<string> SupportingDocumentUrls { get; private set; } = new();
    public DateTime SubmittedAt { get; private set; }

    private ApprovalJustification() { }

    public static ApprovalJustification Create(
        Guid stateMachineId,
        Guid transitionId,
        string justification,
        List<string>? documentUrls = null)
    {
        return new ApprovalJustification
        {
            StateMachineId = stateMachineId,
            TransitionId = transitionId,
            Justification = justification,
            SupportingDocumentUrls = documentUrls ?? new(),
            SubmittedAt = DateTime.UtcNow
        };
    }
}
```

### Discovering Required Data

```csharp
// Check what data is needed for a transition
var transition = await dbContext.StateMachineTransitions
    .FirstAsync(t => t.Id == transitionId);

var requiredDataTypes = transition.GetRequiredDataTypes();

if (transition.RequiresUserData)
{
    // Inform user: "This transition requires you to provide:"
    foreach (var dataType in requiredDataTypes)
    {
        // Present appropriate form based on dataType
        // e.g., if dataType == typeof(ApprovalJustification), show justification form
    }
}
```

### Handler Fetches Data from Database

```csharp
public class ApprovalJustificationRequirementHandler(IApplicationDbContext dbContext) 
    : IStateMachineTransitionRequirementHandler<ApprovalJustificationRequirement>
{
    public async Task<bool> HandleAsync(
        ApprovalJustificationRequirement requirement,
        Guid stateMachineId,
        object? requirementContext)
    {
        // Handler fetches the data directly from database
        var justification = await dbContext.ApprovalJustifications
            .FirstOrDefaultAsync(aj => 
                aj.StateMachineId == stateMachineId && 
                aj.TransitionId == /* get from context or requirement */);

        // If data hasn't been collected yet, requirement not fulfilled
        if (justification == null)
            return false;

        // Validate against requirement configuration
        if (justification.Justification.Length < requirement.MinimumCharacters)
            return false;

        if (requirement.RequiresSupportingDocuments && !justification.SupportingDocumentUrls.Any())
            return false;

        return true;
    }
}
```

## Service Registration

### Basic Registration
```csharp
// Scans the calling assembly for handlers
services.AddStateMachineServices();
```

### Manual Handler Registration
```csharp
// Register specific handlers
services.AddScoped<IStateMachineTransitionRequirementHandler<UserRoleRequirement>, UserRoleRequirementHandler>();
services.AddScoped<IStateMachineTransitionRequirementHandler<ApprovalJustificationRequirement>, ApprovalJustificationHandler>();

// Register generic handlers
services.AddScoped<IStateMachineTransitionHandler, AdminOverrideHandler>();
services.AddScoped<IStateMachineTransitionHandler, LoggingHandler>();
```

## Implementation Notes

- **Entity Operations**: All operations use `StateMachineState` and `StateMachineTrigger` entities
- **No String Operations**: Removed all string-based transition methods
- **Service-Based**: Use `IStateMachineTransitionService` for all transition operations
- **Two-Phase Evaluation**: Specific handlers first, then all registered generic handlers
- **Type-Based Discovery**: Uses reflection to discover handlers by their generic type parameters
- **Configurable Assemblies**: Multiple assemblies can be scanned for handlers
- **Auto-Registration**: Handlers are automatically registered via dependency injection
- **Force Transitions**: Bypass requirements when needed using entity-based force methods
