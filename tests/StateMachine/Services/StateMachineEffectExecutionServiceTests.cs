using AQ.StateMachine.Services;
using AQ.StateMachine.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AQ.StateMachine.Services.Tests;

public class StateMachineEffectExecutionServiceTests
{
    private readonly StateMachineEffectExecutionService _service;
    private readonly IServiceProvider _serviceProvider;

    public StateMachineEffectExecutionServiceTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        var options = new StateMachineEffectExecutionOptions
        {
            AutoRegisterHandlers = false // Disable auto registration for tests
        };
        _service = new StateMachineEffectExecutionService(_serviceProvider, options);
    }

    [Fact]
    public async Task ExecuteEffectsAsync_WithNoEffects_ReturnsAllExecuted()
    {
        // Arrange
        var instance = TestData.CreateTestInstance();
        var effects = Array.Empty<IStateMachineTransitionEffect>();
        var transitionInfo = new StateMachineTransitionInfo
        {
            Success = true,
            TransitionedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _service.ExecuteEffectsAsync(effects, instance, transitionInfo);

        // Assert
        result.AllEffectsExecuted.Should().BeTrue();
        result.EffectResults.Should().BeEmpty();
        result.TotalEffects.Should().Be(0);
        result.SuccessfulEffects.Should().Be(0);
        result.FailedEffects.Should().Be(0);
    }

    [Fact]
    public void RegisterSpecificHandler_ValidHandler_AddsToCollection()
    {
        // Arrange
        var handler = new TestEffectHandler();

        // Act
        _service.RegisterSpecificHandler(handler);

        // Assert
        var handlers = _service.GetSpecificHandlersForEffectType<TestEffect>();
        handlers.Should().HaveCount(1);
        handlers.First().EffectTypeName.Should().Be("TestEffect");
        handlers.First().IsGeneric.Should().BeFalse();
    }

    [Fact]
    public void RegisterGenericHandler_ValidHandler_AddsToCollection()
    {
        // Arrange
        var handler = Substitute.For<IStateMachineTransitionEffectHandler>();

        // Act
        _service.RegisterGenericHandler(handler);

        // Assert
        var handlers = _service.GetGenericHandlers();
        handlers.Should().HaveCount(1);
        handlers.First().EffectTypeName.Should().Be("*");
        handlers.First().IsGeneric.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteEffectsAsync_WithRegisteredHandler_CallsHandler()
    {
        // Arrange
        var instance = TestData.CreateTestInstance();
        var effect = TestData.CreateNotificationEffect();
        var effects = new[] { effect };
        var transitionInfo = new StateMachineTransitionInfo
        {
            Success = true,
            TransitionedAt = DateTimeOffset.UtcNow
        };

        var handler = new TestEffectHandler();
        _service.RegisterSpecificHandler(handler);

        // Act
        var result = await _service.ExecuteEffectsAsync(effects, instance, transitionInfo);

        // Assert
        result.AllEffectsExecuted.Should().BeTrue();
        result.EffectResults.Should().HaveCount(1);
        result.EffectResults.First().IsExecuted.Should().BeTrue();
        result.EffectResults.First().WasProcessedBySpecificHandler.Should().BeTrue();
        handler.WasExecuted.Should().BeTrue();
        handler.LastEffect.Should().Be(effect);
    }

    [Fact]
    public void GetSpecificHandlersForEffectType_WithType_ReturnsHandlers()
    {
        // Arrange
        var handler = new TestEffectHandler();
        _service.RegisterSpecificHandler(handler);

        // Act
        var handlers = _service.GetSpecificHandlersForEffectType(typeof(TestEffect));

        // Assert
        handlers.Should().HaveCount(1);
        handlers.First().EffectTypeName.Should().Be("TestEffect");
    }

    [Fact]
    public void GetSpecificHandlersForEffectType_NoHandlers_ReturnsEmpty()
    {
        // Act
        var handlers = _service.GetSpecificHandlersForEffectType<TestEffect>();

        // Assert
        handlers.Should().BeEmpty();
    }
}
