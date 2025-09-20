using AQ.StateMachine.Services;
using AQ.StateMachine.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AQ.StateMachine.Services.Tests;

public class StateMachineRequirementEvaluationServiceTests
{
    private readonly StateMachineRequirementEvaluationService _service;
    private readonly IServiceProvider _serviceProvider;

    public StateMachineRequirementEvaluationServiceTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        var options = new StateMachineRequirementEvaluationOptions
        {
            AutoRegisterHandlers = false // Disable auto registration for tests
        };
        _service = new StateMachineRequirementEvaluationService(_serviceProvider, options);
    }

    [Fact]
    public async Task EvaluateRequirementsAsync_WithNoRequirements_ReturnsAllMet()
    {
        // Arrange
        var instance = TestData.CreateTestInstance();
        var requirements = Array.Empty<IStateMachineTransitionRequirement>();

        // Act
        var result = await _service.EvaluateRequirementsAsync(requirements, instance);

        // Assert
        result.AllRequirementsMet.Should().BeTrue();
        result.RequirementResults.Should().BeEmpty();
        result.FailureReasons.Should().BeEmpty();
    }

    [Fact]
    public void RegisterSpecificHandler_ValidHandler_AddsToCollection()
    {
        // Arrange
        var handler = new TestRequirementHandler();

        // Act
        _service.RegisterSpecificHandler(handler);

        // Assert
        var handlers = _service.GetSpecificHandlersForRequirementType<TestRequirement>();
        handlers.Should().HaveCount(1);
        handlers.First().RequirementTypeName.Should().Be("TestRequirement");
        handlers.First().IsGeneric.Should().BeFalse();
    }

    [Fact]
    public void RegisterGenericHandler_ValidHandler_AddsToCollection()
    {
        // Arrange
        var handler = Substitute.For<IStateMachineTransitionHandler>();

        // Act
        _service.RegisterGenericHandler(handler);

        // Assert
        var handlers = _service.GetGenericHandlers();
        handlers.Should().HaveCount(1);
        handlers.First().RequirementTypeName.Should().Be("*");
        handlers.First().IsGeneric.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRequirementsAsync_WithRegisteredHandler_CallsHandler()
    {
        // Arrange
        var instance = TestData.CreateTestInstance();
        var requirement = TestData.CreateValidatorRequirement();
        var requirements = new[] { requirement };

        var handler = new TestRequirementHandler { ShouldApprove = true };
        _service.RegisterSpecificHandler(handler);

        // Act
        var result = await _service.EvaluateRequirementsAsync(requirements, instance);

        // Assert
        result.AllRequirementsMet.Should().BeTrue();
        result.RequirementResults.Should().HaveCount(1);
        result.RequirementResults.First().IsFulfilled.Should().BeTrue();
        result.RequirementResults.First().WasProcessedBySpecificHandler.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateRequirementsAsync_WithFailingHandler_ReturnsFalse()
    {
        // Arrange
        var instance = TestData.CreateTestInstance();
        var requirement = TestData.CreateValidatorRequirement();
        var requirements = new[] { requirement };

        var handler = new TestRequirementHandler { ShouldApprove = false };
        _service.RegisterSpecificHandler(handler);

        // Act
        var result = await _service.EvaluateRequirementsAsync(requirements, instance);

        // Assert
        result.AllRequirementsMet.Should().BeFalse();
        result.RequirementResults.Should().HaveCount(1);
        result.RequirementResults.First().IsFulfilled.Should().BeFalse();
    }

    [Fact]
    public void GetSpecificHandlersForRequirementType_WithType_ReturnsHandlers()
    {
        // Arrange
        var handler = new TestRequirementHandler();
        _service.RegisterSpecificHandler(handler);

        // Act
        var handlers = _service.GetSpecificHandlersForRequirementType(typeof(TestRequirement));

        // Assert
        handlers.Should().HaveCount(1);
        handlers.First().RequirementTypeName.Should().Be("TestRequirement");
    }

    [Fact]
    public void GetSpecificHandlersForRequirementType_NoHandlers_ReturnsEmpty()
    {
        // Act
        var handlers = _service.GetSpecificHandlersForRequirementType<TestRequirement>();

        // Assert
        handlers.Should().BeEmpty();
    }
}
