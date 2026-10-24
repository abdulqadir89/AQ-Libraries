using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using AQ.Utilities.Snapshots;
using AQ.Utilities.Verification;
using AQ.Utilities.Versioning;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Versioning;

public class VersionGroupTests : IDisposable
{
    private static readonly AqSavePipelineOptions All = new()
    {
        Lifecycle = true,
        VersionGroups = true,
        VerificationReset = true,
        Audit = true,
        Snapshots = true,
    };

    private readonly PipelineTestDatabase _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    private PipelineTestContext CreateContext(AqSavePipelineOptions? options = null)
    {
        var context = _database.CreateContext(options ?? All, _userId);
        if (!context.Users.Any(u => u.Id == _userId))
        {
            context.Users.Add(new TestUser { Id = _userId });
            context.SaveChanges();
        }

        return context;
    }

    private static async Task<Manual> AddAsync(PipelineTestContext context, string title, string? version = null)
    {
        var manual = Manual.Create(title);
        context.Manuals.Add(manual);
        await context.SaveChangesAsync();
        if (version is not null)
        {
            manual.SetVersionLabel(version);
            await context.SaveChangesAsync();
        }

        return manual;
    }

    // RULE EFC-26
    [Fact]
    public async Task Save_GivesAddedEntityItsOwnGroup()
    {
        using var context = CreateContext();

        var first = await AddAsync(context, "A");
        var second = await AddAsync(context, "B");

        first.VersionGroupId.Should().NotBeEmpty();
        second.VersionGroupId.Should().NotBeEmpty().And.NotBe(first.VersionGroupId);
        first.Version.Should().BeNull();
    }

    [Fact]
    public async Task Save_KeepsGroupWhenEntityIsAddedWithOne()
    {
        using var context = CreateContext();
        var source = await AddAsync(context, "A", "1.0");
        var next = Manual.Create("A");
        next.JoinVersionGroup(source, "2.0");
        context.Manuals.Add(next);

        await context.SaveChangesAsync();

        next.VersionGroupId.Should().Be(source.VersionGroupId);
    }

    [Fact]
    public async Task Save_WithoutStepLeavesGroupEmpty()
    {
        using var context = CreateContext(new AqSavePipelineOptions { Audit = true });
        var manual = Manual.Create("A");
        context.Manuals.Add(manual);

        await context.SaveChangesAsync();

        manual.VersionGroupId.Should().BeEmpty();
    }

    // RULE EFC-25
    [Fact]
    public async Task Index_RejectsSameVersionInOneGroup()
    {
        using var context = CreateContext();
        var source = await AddAsync(context, "A", "1.0");
        var duplicate = Manual.Create("A copy");
        duplicate.JoinVersionGroup(source, "1.0");
        context.Manuals.Add(duplicate);

        var save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Index_AllowsSameVersionInDifferentGroups()
    {
        using var context = CreateContext();

        await AddAsync(context, "A", "1.0");
        await AddAsync(context, "B", "1.0");

        (await context.Manuals.CountAsync(m => m.Version == "1.0")).Should().Be(2);
    }

    // RULE VSN-4
    [Fact]
    public async Task Join_MovesEntityIntoTargetGroup()
    {
        using var context = CreateContext();
        var target = await AddAsync(context, "A", "1.0");
        var mover = await AddAsync(context, "B");

        mover.JoinVersionGroup(target, " 2.0 ");
        await context.SaveChangesAsync();

        var reloaded = await context.Manuals.AsNoTracking().SingleAsync(m => m.Id == mover.Id);
        reloaded.VersionGroupId.Should().Be(target.VersionGroupId);
        reloaded.Version.Should().Be("2.0");
    }

    [Fact]
    public async Task Join_RefusesSameGroupAndSelf()
    {
        using var context = CreateContext();
        var target = await AddAsync(context, "A", "1.0");
        var sibling = Manual.Create("B");
        sibling.JoinVersionGroup(target, "2.0");

        var sameGroup = () => sibling.JoinVersionGroup(target, "3.0");
        var self = () => target.JoinVersionGroup(target, "3.0");

        sameGroup.Should().Throw<InvalidOperationException>();
        self.Should().Throw<InvalidOperationException>();
    }

    // RULE VSN-5
    [Fact]
    public async Task Leave_MakesEntityIndependentAndKeepsOthersTogether()
    {
        using var context = CreateContext();
        var anchor = await AddAsync(context, "A", "1.0");
        var second = Manual.Create("A2");
        second.JoinVersionGroup(anchor, "2.0");
        var third = Manual.Create("A3");
        third.JoinVersionGroup(anchor, "3.0");
        context.Manuals.AddRange(second, third);
        await context.SaveChangesAsync();
        var group = anchor.VersionGroupId;

        anchor.LeaveVersionGroup();
        await context.SaveChangesAsync();

        anchor.VersionGroupId.Should().NotBe(group);
        anchor.Version.Should().Be("1.0");
        (await context.Manuals.InVersionGroup(group).Select(m => m.Id).ToListAsync()).Should().BeEquivalentTo([second.Id, third.Id]);
    }

    // RULE VSN-6
    [Fact]
    public async Task SetVersionLabel_RenamesWithinGroup()
    {
        using var context = CreateContext();
        var manual = await AddAsync(context, "A", "1.0");
        var group = manual.VersionGroupId;

        manual.SetVersionLabel("1.1");
        await context.SaveChangesAsync();

        manual.VersionGroupId.Should().Be(group);
        manual.Version.Should().Be("1.1");
    }

    // RULE VSN-7
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeVersion_RefusesBlank(string version)
    {
        var act = () => VersionGroupOperations.NormalizeVersion(version);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NormalizeVersion_RefusesTooLongAndTrims()
    {
        var tooLong = () => VersionGroupOperations.NormalizeVersion(new string('x', VersionGroupOperations.MaxVersionLength + 1));

        tooLong.Should().Throw<ArgumentException>();
        VersionGroupOperations.NormalizeVersion("  v1 ").Should().Be("v1");
    }

    // RULE VSN-8
    [Fact]
    public async Task WithVersion_FindsTakenLabelAndCanExcludeOneRecord()
    {
        using var context = CreateContext();
        var manual = await AddAsync(context, "A", "1.0");

        (await context.Manuals.WithVersion(manual.VersionGroupId, "1.0").AnyAsync()).Should().BeTrue();
        (await context.Manuals.WithVersion(manual.VersionGroupId, "1.0", manual.Id).AnyAsync()).Should().BeFalse();
        (await context.Manuals.WithVersion(manual.VersionGroupId, "2.0").AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task WithSiblingsAndGroupBy_ReturnWholeGroup()
    {
        using var context = CreateContext();
        var anchor = await AddAsync(context, "A", "1.0");
        var other = Manual.Create("A2");
        other.JoinVersionGroup(anchor, "2.0");
        context.Manuals.Add(other);
        await AddAsync(context, "Unrelated", "1.0");
        await context.SaveChangesAsync();

        (await context.Manuals.WithSiblings(anchor).CountAsync()).Should().Be(2);
        var groups = await context.Manuals.GroupByVersionGroup().Select(g => g.Count()).ToListAsync();
        groups.Should().BeEquivalentTo([2, 1]);
    }

    // RULE VSN-9
    [Fact]
    public async Task Snapshot_RecordsVersionAndGroup()
    {
        using var context = CreateContext();
        var manual = await AddAsync(context, "A", "1.0");

        var snapshots = await context.Set<EntitySnapshot>()
            .Where(s => s.EntityId == manual.Id).OrderBy(s => s.Version).ToListAsync();

        var latest = snapshots.Last().Data;
        latest.Should().Contain("\"version\":\"1.0\"").And.Contain($"\"versionGroupId\":\"{manual.VersionGroupId}\"");
    }

    [Fact]
    public async Task Version_ChangeResetsVerificationButGroupMoveDoesNot()
    {
        using var context = CreateContext();
        var manual = await AddAsync(context, "A", "1.0");
        manual.Verification.Add(Guid.NewGuid());
        await context.SaveChangesAsync();
        manual.Verification.Count.Should().Be(1);

        manual.LeaveVersionGroup();
        await context.SaveChangesAsync();
        manual.Verification.Count.Should().Be(1);

        manual.SetVersionLabel("1.1");
        await context.SaveChangesAsync();
        manual.Verification.Count.Should().Be(0);
    }
}
