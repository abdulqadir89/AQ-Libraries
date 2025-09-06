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

### Entity-Based Operations
- **States**: Use `StateMachineState` entities instead of string names
- **Triggers**: Use `StateMachineTrigger` entities instead of string names
- **Consistent API**: All operations use proper entity references

## Architecture

### Domain Layer
- `IStateMachineTransitionRequirement` - Base requirement interface
- `StateMachineTransitionRequirement` - Abstract base class with common properties
- `IStateMachineTransitionRequirementHandler<TRequirement>` - Typed handler interface
- `IStateMachineTransitionHandler` - Generic handler interface for processing all requirements
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
        StateMachineInstance stateMachine, 
        IDictionary<string, object>? context)
    {
        // Implementation logic here
        return userHasRole;
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
6. **Extensibility**: Easy to add new requirement types and handlers
7. **Separation of Concerns**: Business logic separated from state machine mechanics
8. **Type Safety**: Strong typing with runtime flexibility
9. **Testability**: Requirements and handlers can be unit tested independently

## Service Registration

### Basic Registration (Single Assembly)
```csharp
// Scans the calling assembly for handlers
services.AddStateMachineRequirementEvaluation();
```

### Multiple Assemblies
```csharp
// Scans multiple assemblies for handlers
services.AddStateMachineRequirementEvaluation(
    Assembly.GetExecutingAssembly(),
    typeof(SomeOtherModule).Assembly
);
```

### Custom Configuration
```csharp
services.AddStateMachineRequirementEvaluation(options =>
{
    options.HandlerAssemblies.Add(Assembly.GetExecutingAssembly());
    options.HandlerAssemblies.Add(pluginAssembly);
    options.AutoRegisterHandlers = true;
});
```

### Manual Handler Registration
```csharp
// Register specific handlers
services.AddScoped<IStateMachineTransitionRequirementHandler<UserRoleRequirement>, UserRoleRequirementHandler>();

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
