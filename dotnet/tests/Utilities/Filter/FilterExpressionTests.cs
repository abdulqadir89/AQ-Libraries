using AQ.Utilities.Filter;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Filter;

public class FilterExpressionTests
{
    private class Label
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Item
    {
        public string Name { get; set; } = string.Empty;
        public int Rank { get; set; }
        public List<Label> Labels { get; set; } = [];
        public List<int> Scores { get; set; } = [];
    }

    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    private static IQueryable<Item> Items() => new List<Item>
    {
        new() { Name = "Both", Rank = 1, Labels = [new() { Id = A, Name = "Alpha" }, new() { Id = B, Name = "Beta" }], Scores = [1, 2] },
        new() { Name = "OnlyA", Rank = 2, Labels = [new() { Id = A, Name = "Alpha" }], Scores = [2, 3] },
        new() { Name = "None", Rank = 3 },
        new() { Name = "R&D (old)", Rank = 4 },
        new() { Name = "O'Brien", Rank = 5 },
    }.AsQueryable();

    private static string[] Names(string expression) => Items().ApplyFilter(expression).Select(i => i.Name).ToArray();

    [Fact]
    public void ApplyFilter_AndBindsTighterThanOr()
    {
        // Rank 4 or (Rank 1 and Name None): the old parser read this as one flat OR group.
        Names("Rank,eq,4 || Rank,eq,1 && Name,eq,None").Should().Equal("R&D (old)");
        Names("Rank,eq,4 || Rank,eq,1 && Name,eq,Both").Should().BeEquivalentTo("Both", "R&D (old)");
    }

    [Fact]
    public void ApplyFilter_NegatedGroup_InvertsTheGroup()
    {
        Names("!(Rank,gt,2)").Should().BeEquivalentTo("Both", "OnlyA");
        Names("Rank,lt,4 && !(Name,eq,None || Name,eq,OnlyA)").Should().Equal("Both");
    }

    [Fact]
    public void ApplyFilter_NotEqualSymbol_IsNotTreatedAsNegation()
    {
        Names("Name,!=,None && Rank,lt,3").Should().BeEquivalentTo("Both", "OnlyA");
    }

    [Fact]
    public void ApplyFilter_QuotedValue_MayContainBracketsAndAmpersands()
    {
        Names("Name,eq,\"R&D (old)\"").Should().Equal("R&D (old)");
        Names("Name,eq,'R&D (old)' || Name,eq,None").Should().BeEquivalentTo("R&D (old)", "None");
    }

    [Fact]
    public void ApplyFilter_ApostropheInsideAWord_IsPlainText()
    {
        Names("Name,eq,O'Brien && Rank,eq,5").Should().Equal("O'Brien");
    }

    [Fact]
    public void ParseComplexExpression_SimpleAnd_KeepsAFlatRootGroup()
    {
        var root = FilterExpressionParser.ParseComplexExpression("Name,eq,x && Rank,gt,1").RootGroup;

        root.LogicalOperator.Should().Be(LogicalOperator.And);
        root.Conditions.Should().HaveCount(2);
        root.Groups.Should().BeEmpty();
    }

    [Fact]
    public void ApplyFilter_CollectionPath_EqualMeansSomeElementMatches()
    {
        Names($"Labels.Id,eq,{A}").Should().BeEquivalentTo("Both", "OnlyA");
        Names($"Labels.Id,eq,{A} && Labels.Id,eq,{B}").Should().Equal("Both");
        Names($"(Labels.Id,eq,{B} || Labels.Id,eq,{A})").Should().BeEquivalentTo("Both", "OnlyA");
        Names($"Labels.Id,in,{A},{B}").Should().BeEquivalentTo("Both", "OnlyA");
    }

    [Fact]
    public void ApplyFilter_CollectionPath_NotEqualMeansNoElementMatches()
    {
        Names($"Labels.Id,ne,{B}").Should().BeEquivalentTo("OnlyA", "None", "R&D (old)", "O'Brien");
        Names($"Labels.Id,ne,{A} && Labels.Id,ne,{B}").Should().BeEquivalentTo("None", "R&D (old)", "O'Brien");
        Names($"Labels.Id,notin,{A},{B}").Should().BeEquivalentTo("None", "R&D (old)", "O'Brien");
    }

    [Fact]
    public void ApplyFilter_CollectionPath_StringConditionsOnElements()
    {
        Names("Labels.Name,contains,bet").Should().Equal("Both");
        Names("Labels.Name,notcontains,bet").Should().BeEquivalentTo("OnlyA", "None", "R&D (old)", "O'Brien");
    }

    [Fact]
    public void ApplyFilter_CollectionItself_NullChecksTestForEmpty()
    {
        Names("Labels,isnull").Should().BeEquivalentTo("None", "R&D (old)", "O'Brien");
        Names("Labels,isnotnull").Should().BeEquivalentTo("Both", "OnlyA");
    }

    [Fact]
    public void ApplyFilter_UnknownCollectionMember_DropsTheCondition()
    {
        Names("Labels.Nope,eq,1").Should().HaveCount(5);
    }

    [Fact]
    public void ParseComplexExpression_NestingBeyondTheLimit_ThrowsInsteadOfOverflowingTheStack()
    {
        var atLimit = string.Concat(Enumerable.Repeat("(", 32)) + "Rank,eq,1" + string.Concat(Enumerable.Repeat(")", 32));
        var tooDeep = string.Concat(Enumerable.Repeat("(", 33)) + "Rank,eq,1" + string.Concat(Enumerable.Repeat(")", 33));
        var manyNots = string.Concat(Enumerable.Repeat("!", 100000)) + "(Rank,eq,1)";
        var manyBrackets = string.Concat(Enumerable.Repeat("(", 100000)) + "Rank,eq,1";

        Names(atLimit).Should().Equal("Both");
        FluentActions.Invoking(() => FilterExpressionParser.ParseComplexExpression(tooDeep)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => FilterExpressionParser.ParseComplexExpression(manyNots)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => FilterExpressionParser.ParseComplexExpression(manyBrackets)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApplyFilter_ListOfPlainValues_ComparesTheElements()
    {
        Names("Scores,eq,2").Should().BeEquivalentTo("Both", "OnlyA");
        Names("Scores,eq,1 && Scores,eq,2").Should().Equal("Both");
        Names("Scores,in,3,9").Should().Equal("OnlyA");
        Names("Scores,ne,2").Should().BeEquivalentTo("None", "R&D (old)", "O'Brien");
        Names("Scores,gt,2").Should().Equal("OnlyA");
    }

    [Fact]
    public void ApplyFilter_NullCheckOnAPropertyThatCannotBeNull_MatchesNothingOrEverything()
    {
        Names("Rank,isnull").Should().BeEmpty();
        Names("Rank,isnotnull").Should().HaveCount(5);
        Names("Rank,isnull || Name,eq,None").Should().Equal("None");
    }
}
