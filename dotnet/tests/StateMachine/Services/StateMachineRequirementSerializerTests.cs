using Xunit;
using AQ.StateMachine.Entities;
using FluentAssertions;

namespace AQ.StateMachine.Services.Tests;

public record SerializerOptionalRequirement : StateMachineTransitionRequirement;

public record SerializerThresholdRequirement : StateMachineTransitionRequirement
{
    public int Threshold { get; init; }
}

public class StateMachineRequirementSerializerTests
{
    public StateMachineRequirementSerializerTests()
    {
        StateMachineRequirementTypes.Register(typeof(SerializerOptionalRequirement));
        StateMachineRequirementTypes.Register(typeof(SerializerThresholdRequirement));
    }

    [Fact]
    public void RoundTrip_ReturnsTheSameConcreteTypesWithAllProperties()
    {
        IStateMachineTransitionRequirement[] requirements =
        [
            new SerializerOptionalRequirement { Description = "needs sign-off", IsOptional = true },
            new SerializerThresholdRequirement { Threshold = 7 }
        ];

        var json = StateMachineRequirementSerializer.Serialize(requirements);
        var result = StateMachineRequirementSerializer.Deserialize(json)!.ToList();

        result.Should().Equal(requirements);
        result.Should().AllSatisfy(r => r.Should().NotBeOfType<UnresolvedStateMachineTransitionRequirement>());
    }

    [Fact]
    public void Serialize_EmptyOrNull_StoresNothing()
    {
        StateMachineRequirementSerializer.Serialize(null).Should().BeNull();
        StateMachineRequirementSerializer.Serialize([]).Should().BeNull();
    }

    [Fact]
    public void Serialize_TwoRequirementsOfOneType_Throws()
    {
        var act = () => StateMachineRequirementSerializer.Serialize(
            [new SerializerOptionalRequirement(), new SerializerOptionalRequirement()]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deserialize_LegacyEmptyPayload_ReturnsDefaults()
    {
        var result = StateMachineRequirementSerializer.Deserialize("{\"SerializerOptionalRequirement\":{}}")!.ToList();

        result.Should().ContainSingle().Which.Should().Be(new SerializerOptionalRequirement());
    }

    [Fact]
    public void Deserialize_UnregisteredType_KeepsNameAndDataAndWritesThemBack()
    {
        const string json = "{\"RemovedRequirement\":{\"Reason\":\"x\"}}";

        var result = StateMachineRequirementSerializer.Deserialize(json)!.ToList();

        result.Should().ContainSingle().Which.Should().BeOfType<UnresolvedStateMachineTransitionRequirement>()
            .Which.RequirementTypeName.Should().Be("RemovedRequirement");
        StateMachineRequirementSerializer.Serialize(result).Should().Be(json);
    }

    [Fact]
    public void Deserialize_MalformedJson_Throws()
    {
        var act = () => StateMachineRequirementSerializer.Deserialize("[1,2]");

        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void Register_TwoTypesWithOneName_Throws()
    {
        var act = () => StateMachineRequirementTypes.Register(typeof(UnresolvedStateMachineTransitionRequirement));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithEmptyList_ClearsRequirements()
    {
        var transition = StateMachineTransition.Create(
            TestData.CreateSimpleDefinition(),
            null,
            null,
            StateMachineTrigger.Create(TestData.CreateSimpleDefinition(), "T"),
            requirements: [new SerializerOptionalRequirement()]);

        transition.Update(requirements: []);

        transition.Requirements.Should().BeNull();
        transition.HasRequirements.Should().BeFalse();
    }
}
