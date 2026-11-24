using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Attachments;

public class AttachmentTests : IDisposable
{
    private static readonly AqSavePipelineOptions All = new()
    {
        Lifecycle = true,
        Audit = true,
        Snapshots = true,
        Outbox = true,
    };

    private readonly PipelineTestDatabase _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    private PipelineTestContext CreateContext()
    {
        var context = _database.CreateContext(All, _userId);
        context.Users.Add(new TestUser { Id = _userId });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task Attachment_IsLinkedAndUploadedByTheActor()
    {
        Guid binderId;
        using (var context = CreateContext())
        {
            var binder = Binder.Create("Q1");
            context.AddRange(binder, BinderAttachment.Create(binder, "a.pdf", 10, "application/pdf"));
            await context.SaveChangesAsync();
            binderId = binder.Id;
        }

        using var reloaded = _database.CreateContext(All, _userId);
        var loaded = await reloaded.BinderAttachments.ForBinder(binderId).SingleAsync();

        loaded.BinderId.Should().Be(binderId);
        loaded.IsPending.Should().BeFalse();
        loaded.FileName.Should().Be("a.pdf");
        loaded.CreatedById.Should().Be(_userId);
    }

    [Fact]
    public async Task StagedAttachment_HasNoLinkUntilTheRecordExists_ThenLinks()
    {
        var binder = Binder.Create("Late");
        using (var context = CreateContext())
        {
            context.Add(BinderAttachment.CreateStaged(binder.Id, "draft.pdf", 5));
            await context.SaveChangesAsync();
        }

        using (var context = _database.CreateContext(All, _userId))
        {
            var pending = await context.BinderAttachments.Pending().SingleAsync();
            pending.BinderId.Should().BeNull();
            pending.OwnerId.Should().Be(binder.Id);

            context.Add(binder);
            pending.Link();
            await context.SaveChangesAsync();
        }

        using var check = _database.CreateContext(All, _userId);
        var linked = await check.BinderAttachments.SingleAsync();
        linked.BinderId.Should().Be(binder.Id);
        linked.IsPending.Should().BeFalse();
    }

    [Fact]
    public void Descriptor_CreatesLinkedAndPendingAttachmentsFromTheId()
    {
        var id = Guid.NewGuid();
        var descriptor = AQ.Utilities.Attachments.AttachmentRegistry.Find("Binder")!;

        var linked = descriptor.CreateLinked(id, "a.pdf", 1, null, null);
        var pending = descriptor.CreatePending(id, "b.pdf", 1, null, null);

        ((BinderAttachment)linked).BinderId.Should().Be(id);
        linked.IsPending.Should().BeFalse();
        ((BinderAttachment)pending).BinderId.Should().BeNull();
        pending.IsPending.Should().BeTrue();
        pending.OwnerId.Should().Be(id);
    }

    [Fact]
    public async Task DeletingTheRecord_DeletesItsAttachments()
    {
        Guid binderId;
        using (var context = CreateContext())
        {
            var binder = Binder.Create("Gone");
            context.AddRange(binder, BinderAttachment.Create(binder, "a.pdf", 1, "application/pdf"));
            await context.SaveChangesAsync();
            binderId = binder.Id;
        }

        using (var context = _database.CreateContext(All, _userId))
        {
            context.Binders.Remove(await context.Binders.SingleAsync(b => b.Id == binderId));
            await context.SaveChangesAsync();
        }

        using var check = _database.CreateContext(All, _userId);
        (await check.BinderAttachments.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task SameFileNameTwiceForOneRecord_IsRefusedByTheDatabase()
    {
        using var context = CreateContext();
        var binder = Binder.Create("Dup");
        context.AddRange(binder, BinderAttachment.Create(binder, "a.pdf", 1), BinderAttachment.Create(binder, "a.pdf", 2));

        var save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public void Limits_RefuseALargeFile_AWrongExtension_AndAPathInTheName()
    {
        var binder = Binder.Create("Limits");

        var large = () => BinderAttachment.Create(binder, "big.pdf", 1001);
        var extension = () => BinderAttachment.Create(binder, "a.exe", 1);
        var path = () => BinderAttachment.Create(binder, "../a.pdf", 1);
        var tooMany = () => BinderAttachment.Limits.EnsureCountAllowed(2);
        var allowed = () => BinderAttachment.Limits.EnsureCountAllowed(1);

        large.Should().Throw<InvalidOperationException>();
        extension.Should().Throw<InvalidOperationException>();
        path.Should().Throw<ArgumentException>();
        tooMany.Should().Throw<InvalidOperationException>();
        allowed.Should().NotThrow();
    }
}
