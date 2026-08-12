using AQ.Utilities.Sort;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Sort;

public class SortExpressionParserTests
{
    [Fact]
    public void ParseCondition_BareName_DefaultsAscending()
    {
        var condition = SortExpressionParser.ParseCondition("Name");

        condition.PropertyPath.Should().Be("Name");
        condition.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void ParseCondition_DashPrefix_SetsDescending()
    {
        var condition = SortExpressionParser.ParseCondition("-Name");

        condition.PropertyPath.Should().Be("Name");
        condition.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void ParseCondition_CommaDesc_StillWorks()
    {
        var condition = SortExpressionParser.ParseCondition("Name,desc");

        condition.PropertyPath.Should().Be("Name");
        condition.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void ParseCondition_CommaAsc_StillWorks()
    {
        var condition = SortExpressionParser.ParseCondition("Name,asc");

        condition.PropertyPath.Should().Be("Name");
        condition.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void ParseCondition_DashPrefixWithCommaDirection_Throws()
    {
        var act = () => SortExpressionParser.ParseCondition("-Name,desc");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseCondition_DashOnly_Throws()
    {
        var act = () => SortExpressionParser.ParseCondition("-");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_ComplexExpressionWithDashPrefix_ParsesEachCondition()
    {
        var spec = SortExpressionParser.Parse("-Name;Age,asc");

        var conditions = spec.GetOrderedConditions().ToList();

        conditions.Should().HaveCount(2);
        conditions[0].PropertyPath.Should().Be("Name");
        conditions[0].Direction.Should().Be(SortDirection.Descending);
        conditions[1].PropertyPath.Should().Be("Age");
        conditions[1].Direction.Should().Be(SortDirection.Ascending);
    }
}
