using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using AQ.Utilities.Tagging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Tagging;

public class TagTests : IDisposable
{
    private static readonly AqSavePipelineOptions All = new()
    {
        Lifecycle = true,
        VersionGroups = true,
        Tags = true,
        VerificationReset = true,
        Audit = true,
        Snapshots = true,
    };

    private readonly PipelineTestDatabase _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    private PipelineTestContext CreateContext()
    {
        var context = _database.CreateContext(All, _userId);
        if (!context.Users.Any(u => u.Id == _userId))
        {
            context.Users.Add(new TestUser { Id = _userId });
            context.SaveChanges();
        }

        return context;
    }

    private static async Task<TestTag> AddTagAsync(PipelineTestContext context, string name)
    {
        var tag = new TestTag { Name = name };
        context.TestTags.Add(tag);
        await context.SaveChangesAsync();
        return tag;
    }

    private static async Task<Manual> AddManualAsync(PipelineTestContext context, string title, params TestTag[] tags)
    {
        var manual = Manual.Create(title);
        manual.SetTags(tags.Select(t => t.Id));
        context.Manuals.Add(manual);
        await context.SaveChangesAsync();
        return manual;
    }

    // RULE TAG-1
    [Fact]
    public async Task SetTags_AddsAndRemovesToMatchTheSet()
    {
        using var context = CreateContext();
        var red = await AddTagAsync(context, "red");
        var blue = await AddTagAsync(context, "blue");
        var green = await AddTagAsync(context, "green");
        var manual = await AddManualAsync(context, "A", red, blue);

        manual.SetTags([blue.Id, green.Id]);
        await context.SaveChangesAsync();

        var loaded = await context.Manuals.AsNoTracking().Include(m => m.Tags).SingleAsync(m => m.Id == manual.Id);
        loaded.Tags.Select(t => t.TagId).Should().BeEquivalentTo([blue.Id, green.Id]);
        (await context.Set<EntityTag<TestTag>>().CountAsync()).Should().Be(2);
    }

    // RULE TAG-2
    [Fact]
    public async Task AddTags_KeepsExistingAndSkipsDuplicates()
    {
        using var context = CreateContext();
        var red = await AddTagAsync(context, "red");
        var blue = await AddTagAsync(context, "blue");
        var manual = await AddManualAsync(context, "A", red);

        manual.AddTags([red.Id, blue.Id, blue.Id, Guid.Empty]);
        await context.SaveChangesAsync();

        manual.TagIds.Should().BeEquivalentTo([red.Id, blue.Id]);
        (await context.Set<EntityTag<TestTag>>().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task RemoveTags_RemovesOnlyThePresentOnes()
    {
        using var context = CreateContext();
        var red = await AddTagAsync(context, "red");
        var blue = await AddTagAsync(context, "blue");
        var manual = await AddManualAsync(context, "A", red, blue);

        manual.RemoveTags([red.Id, Guid.NewGuid()]);
        await context.SaveChangesAsync();

        var loaded = await context.Manuals.AsNoTracking().Include(m => m.Tags).SingleAsync(m => m.Id == manual.Id);
        loaded.Tags.Select(t => t.TagId).Should().BeEquivalentTo([blue.Id]);
    }

    // RULE EFC-27
    [Fact]
    public async Task Index_RejectsTheSameTagTwiceOnOneRecord()
    {
        using var context = CreateContext();
        var red = await AddTagAsync(context, "red");
        var manual = await AddManualAsync(context, "A", red);
        manual.Tags.Add(ManualTag.Create(manual.Id, red.Id));

        var save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    // RULE EFC-28
    [Fact]
    public async Task DeletingAnUsedTag_IsRefused()
    {
        Guid tagId;
        using (var seed = CreateContext())
        {
            var red = await AddTagAsync(seed, "red");
            await AddManualAsync(seed, "A", red);
            tagId = red.Id;
        }

        using var context = CreateContext();
        context.TestTags.Remove(await context.TestTags.SingleAsync(t => t.Id == tagId));

        var save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    // RULE EFC-30
    [Fact]
    public async Task LinkAddedThroughTheCollectionOfATrackedRecord_IsInserted()
    {
        using var context = CreateContext();
        var red = await AddTagAsync(context, "red");
        var manual = await AddManualAsync(context, "A");

        manual.AddTags([red.Id]);
        await context.SaveChangesAsync();

        using var check = CreateContext();
        (await check.Set<EntityTag<TestTag>>().CountAsync()).Should().Be(1);
    }

    // RULE EFC-29
    [Fact]
    public async Task DeletingTheRecord_RemovesItsLinks()
    {
        using var context = CreateContext();
        var red = await AddTagAsync(context, "red");
        var manual = await AddManualAsync(context, "A", red);

        context.Manuals.Remove(manual);
        await context.SaveChangesAsync();

        (await context.Set<EntityTag<TestTag>>().CountAsync()).Should().Be(0);
        (await context.TestTags.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task WhereHasAnyTag_AndAllTags_FilterInTheDatabase()
    {
        using var context = CreateContext();
        var red = await AddTagAsync(context, "red");
        var blue = await AddTagAsync(context, "blue");
        await AddManualAsync(context, "Both", red, blue);
        await AddManualAsync(context, "Red", red);
        await AddManualAsync(context, "None");

        var any = await context.Manuals.WhereHasAnyTag([red.Id, blue.Id]).Select(m => m.Title).ToListAsync();
        var all = await context.Manuals.WhereHasAllTags([red.Id, blue.Id]).Select(m => m.Title).ToListAsync();
        var onlyBlue = await context.Manuals.WhereHasAnyTag([blue.Id]).Select(m => m.Title).ToListAsync();

        any.Should().BeEquivalentTo(["Both", "Red"]);
        all.Should().BeEquivalentTo(["Both"]);
        onlyBlue.Should().BeEquivalentTo(["Both"]);
    }

    [Fact]
    public async Task WhereHasTagNamed_MatchesPartOfTheTagName()
    {
        using var context = CreateContext();
        var policy = await AddTagAsync(context, "Safety policy");
        var other = await AddTagAsync(context, "Archive");
        await AddManualAsync(context, "A", policy);
        await AddManualAsync(context, "B", other);

        var found = await context.Manuals.WhereHasTagNamed("policy").Select(m => m.Title).ToListAsync();

        found.Should().BeEquivalentTo(["A"]);
    }

    // RULE EFC-30
    [Fact]
    public async Task ExistingLinkMarkedModified_IsNotReinserted()
    {
        using var context = CreateContext();
        var red = await AddTagAsync(context, "red");
        var manual = await AddManualAsync(context, "A", red);
        var link = (await context.Manuals.Include(m => m.Tags).SingleAsync(m => m.Id == manual.Id)).Tags.Single();

        context.Entry(link).State = EntityState.Modified;
        await context.SaveChangesAsync();

        (await context.Set<EntityTag<TestTag>>().CountAsync()).Should().Be(1);
    }
}
