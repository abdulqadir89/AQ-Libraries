using AQ.Entities;
using AQ.Utilities.Merging;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Merging;

public class MergeOperationsTests
{
    private sealed class Node : IEntity, IMergeable<Node>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Guid? PrimaryId { get; private set; }

        void IMergeable<Node>.SetPrimaryId(Guid? primaryId) => PrimaryId = primaryId;
    }

    [Fact]
    public void Merge_SetsPrimaryIdOnEachChild()
    {
        var primary = new Node();
        var first = new Node();
        var second = new Node();

        primary.Merge([first, second]);

        first.PrimaryId.Should().Be(primary.Id);
        second.PrimaryId.Should().Be(primary.Id);
        primary.IsPrimaryNode().Should().BeTrue();
    }

    [Fact]
    public void Merge_IgnoresDuplicateChildren()
    {
        var primary = new Node();
        var child = new Node();

        var act = () => primary.Merge([child, child]);

        act.Should().NotThrow();
        child.PrimaryId.Should().Be(primary.Id);
    }

    [Fact]
    public void Merge_Throws_WhenPrimaryIsAlreadyMerged()
    {
        var other = new Node();
        var primary = new Node();
        other.Merge([primary]);

        var act = () => primary.Merge([new Node()]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Merge_Throws_WhenChildIsThePrimaryItself()
    {
        var primary = new Node();

        var act = () => primary.Merge([primary]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Merge_Throws_WhenChildIsAlreadyMerged_AndLeavesOtherChildrenUntouched()
    {
        var firstPrimary = new Node();
        var secondPrimary = new Node();
        var alreadyMerged = new Node();
        var fresh = new Node();
        firstPrimary.Merge([alreadyMerged]);

        var act = () => secondPrimary.Merge([fresh, alreadyMerged]);

        act.Should().Throw<InvalidOperationException>();
        fresh.PrimaryId.Should().BeNull();
        alreadyMerged.PrimaryId.Should().Be(firstPrimary.Id);
    }

    [Fact]
    public void Unmerge_ClearsPrimaryId()
    {
        var primary = new Node();
        var child = new Node();
        primary.Merge([child]);

        child.Unmerge();

        child.PrimaryId.Should().BeNull();
    }

    [Fact]
    public void Unmerge_Throws_WhenEntityIsAlreadyPrimary()
    {
        var node = new Node();

        var act = () => node.Unmerge();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithMerged_ReturnsPrimaryAndItsChildren()
    {
        var primary = new Node();
        var child = new Node();
        var unrelated = new Node();
        primary.Merge([child]);

        var result = new[] { primary, child, unrelated }.AsQueryable().WithMerged(primary.Id).ToList();

        result.Should().BeEquivalentTo(new[] { primary, child });
    }

    [Fact]
    public void GetEffectivePrimary_ReturnsSelfForPrimaryAndPrimaryForChild()
    {
        var primary = new Node();
        var child = new Node();
        primary.Merge([child]);
        var queryable = new[] { primary, child }.AsQueryable();

        primary.GetEffectivePrimary(queryable).Should().BeSameAs(primary);
        child.GetEffectivePrimary(queryable).Should().BeSameAs(primary);
    }

    [Fact]
    public void GroupByPrimary_GroupsChildrenUnderTheirPrimary()
    {
        var primary = new Node();
        var child = new Node();
        var alone = new Node();
        primary.Merge([child]);

        var groups = new[] { primary, child, alone }.AsQueryable().GroupByPrimary().ToList();

        groups.Should().HaveCount(2);
        groups.Single(g => g.Key == primary.Id).Should().HaveCount(2);
        groups.Single(g => g.Key == alone.Id).Should().ContainSingle();
    }
}

internal static class NodeExtensions
{
    public static bool IsPrimaryNode<T>(this T node) where T : IMergeable<T>, IEntity => node.IsPrimary;
}
