using AQ.StateMachine.Entities;
using AQ.Entities;

namespace AQ.StateMachine.Services.Tests;

/// <summary>
/// Simple test data for state machine tests
/// </summary>
public static class TestData
{
    public static TestRequirement CreateValidatorRequirement() =>
        new("ValidatorRequired");

    public static TestRequirement CreateApprovalRequirement() =>
        new("ApprovalRequired");

    public static TestEffect CreateNotificationEffect() =>
        new("SendNotification");

    public static TestEffect CreateUpdateEffect() =>
        new("UpdateRecord");

    public static TestStateMachineDefinition CreateSimpleDefinition()
    {
        return new TestStateMachineDefinition("Draft");
    }

    public static TestStateMachineInstance CreateTestInstance()
    {
        var definition = CreateSimpleDefinition();
        return new TestStateMachineInstance(definition);
    }
}

// Test entity for StateMachineDefinition<TEntity>
public class TestEntity : Entity
{
    public string Name { get; set; } = "TestEntity";
}

// Test implementation of StateMachineDefinition
public class TestStateMachineDefinition : StateMachineDefinition<TestEntity>
{
    public TestStateMachineDefinition(string initialStateName) : base(new TestEntity(), initialStateName)
    {
    }

    public override StateMachineDefinition CreateNewVersion(int version)
    {
        return new TestStateMachineDefinition("Draft");
    }
}

// Test implementation of StateMachineInstance  
public class TestStateMachineInstance : StateMachineInstance<TestEntity>
{
    public TestStateMachineInstance(StateMachineDefinition definition) : base(definition, new TestEntity())
    {
    }
}

// Simple test requirement record
public record TestRequirement(string Name) : StateMachineTransitionRequirement
{
    public override string GetRequirementTypeName() => Name;
}

// Simple test effect record  
public record TestEffect(string Name) : StateMachineTransitionEffect
{
    public override string GetEffectTypeName() => Name;
}

// Simple test handlers
public class TestRequirementHandler : IStateMachineTransitionRequirementHandler<TestRequirement>
{
    public bool ShouldApprove { get; set; } = true;

    public Task<bool> HandleAsync(TestRequirement requirement, StateMachineInstance stateMachine, object? context)
    {
        return Task.FromResult(ShouldApprove);
    }
}

public class TestEffectHandler : IStateMachineTransitionEffectHandler<TestEffect>
{
    public bool WasExecuted { get; private set; }
    public TestEffect? LastEffect { get; private set; }

    public Task<bool> HandleAsync(TestEffect effect, StateMachineInstance stateMachine, TransitionExecutionInfo transitionInfo)
    {
        WasExecuted = true;
        LastEffect = effect;
        return Task.FromResult(true);
    }
}
