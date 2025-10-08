using AQ.StateMachine.Services;
using AQ.StateMachine.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AQ.StateMachine.Services.Tests;

public class StateMachineServiceTests
{
    private readonly IStateMachineRequirementEvaluationService _requirementService;
    private readonly IStateMachineEffectExecutionService _effectService;
    private readonly StateMachineService _service;

    public StateMachineServiceTests()
    {
        _requirementService = Substitute.For<IStateMachineRequirementEvaluationService>();
        _effectService = Substitute.For<IStateMachineEffectExecutionService>();
        _service = new StateMachineService(_requirementService, _effectService);
    }

    [Fact]
    public async Task GetFirstValidTransitionAsync_WithNoTransitions_ReturnsFailure()
    {
        // Arrange
        var definition = TestData.CreateSimpleDefinition();
        var instance = TestData.CreateTestInstance();
    var trigger = StateMachineTrigger.Create(definition, "NonExistentTrigger", "Non-existent trigger");

        // Act
        var result = await _service.GetFirstValidTransitionAsync(instance, trigger);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("No valid transitions found");
    }

    [Fact]
    public async Task GetFirstValidTransitionAsync_WithNullInstance_ReturnsFailure()
    {
        // Arrange
        var definition = TestData.CreateSimpleDefinition();
    var trigger = StateMachineTrigger.Create(definition, "TestTrigger", "Test trigger description");

        // Act
        var result = await _service.GetFirstValidTransitionAsync(null!, trigger);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("State machine instance cannot be null");
    }

    [Fact]
    public async Task EvaluateRequirementsAsync_WithRequirements_CallsRequirementService()
    {
        // Arrange
        var definition = TestData.CreateSimpleDefinition();
        var instance = TestData.CreateTestInstance();
    var trigger = StateMachineTrigger.Create(definition, "TestTrigger", "Test trigger description");

        var fromState = definition.InitialState!;
    var toState = StateMachineState.Create(definition, "Active", category: StateMachineStateCategory.Intermediate);
    var requirements = new[] { TestData.CreateValidatorRequirement() };
    var transition = StateMachineTransition.Create(definition, fromState, toState, trigger, requirements: requirements);

        var evaluationSummary = new RequirementEvaluationSummary
        {
            AllRequirementsMet = true
        };

        _requirementService.EvaluateRequirementsAsync(requirements, instance, null)
            .Returns(evaluationSummary);

        // Act
        var result = await _service.EvaluateRequirementsAsync(transition, instance);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _requirementService.Received(1).EvaluateRequirementsAsync(requirements, instance, null);
    }

    [Fact]
    public async Task ExecuteEffectsAsync_WithEffects_CallsEffectService()
    {
        // Arrange
        var definition = TestData.CreateSimpleDefinition();
        var instance = TestData.CreateTestInstance();
    var trigger = StateMachineTrigger.Create(definition, "TestTrigger", "Test trigger description");

        var fromState = definition.InitialState!;
    var toState = StateMachineState.Create(definition, "Active", category: StateMachineStateCategory.Intermediate);
    var effects = new[] { TestData.CreateNotificationEffect() };
    var transition = StateMachineTransition.Create(definition, fromState, toState, trigger, effects: effects);

        var transitionInfo = new StateMachineTransitionInfo
        {
            Success = true,
            TransitionedAt = DateTimeOffset.UtcNow,
            PreviousStateId = fromState.Id,
            NewStateId = toState.Id,
            TriggerId = trigger.Id
        };

        var effectSummary = new EffectExecutionSummary
        {
            AllEffectsExecuted = true,
            EffectResults = []
        };

        _effectService.ExecuteEffectsAsync(effects, instance, transitionInfo)
            .Returns(effectSummary);

        // Act
        var result = await _service.ExecuteEffectsAsync(transition, instance, transitionInfo);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _effectService.Received(1).ExecuteEffectsAsync(effects, instance, transitionInfo);
    }
}
