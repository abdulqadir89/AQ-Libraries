using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using AQ.Utilities.Commenting;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Commenting;

public class CommentTests : IDisposable
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
    public async Task Comment_IsAuthoredByTheActor_AndKeepsTheAppsExtraColumn()
    {
        Guid memoId;
        using (var context = CreateContext())
        {
            var memo = Memo.Create("Quarterly");
            var comment = MemoComment.Create(memo, "  Looks right  ");
            comment.Area = "Scope";
            context.AddRange(memo, comment);
            await context.SaveChangesAsync();
            memoId = memo.Id;
        }

        using var reloaded = _database.CreateContext(All, _userId);
        var loaded = await reloaded.MemoComments.ForMemo(memoId).SingleAsync();

        loaded.Text.Should().Be("Looks right");
        loaded.Area.Should().Be("Scope");
        loaded.CreatedById.Should().Be(_userId);
        loaded.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task Reply_IsLinkedToItsTopLevelComment_AndQueriesSplitTheThread()
    {
        using var context = CreateContext();
        var memo = Memo.Create("Threaded");
        var top = MemoComment.Create(memo, "Why this date?");
        var reply = MemoComment.Create(memo, "Contract renewal", top);
        var other = MemoComment.Create(memo, "Unrelated note");
        context.AddRange(memo, top, reply, other);
        await context.SaveChangesAsync();

        var topLevel = await context.MemoComments.ForMemo(memo.Id).TopLevel().ToListAsync();
        var replies = await context.MemoComments.ForMemo(memo.Id).RepliesTo([top.Id]).ToListAsync();

        topLevel.Select(c => c.Id).Should().BeEquivalentTo([top.Id, other.Id]);
        replies.Should().ContainSingle().Which.Id.Should().Be(reply.Id);
        replies[0].ParentId.Should().Be(top.Id);
    }

    [Fact]
    public void Reply_ToADeletedComment_OrAcrossRecords_IsRefused()
    {
        var memo = Memo.Create("Rules");
        var top = MemoComment.Create(memo, "Top");

        var acrossRecords = () => MemoComment.Create(Memo.Create("Other"), "Elsewhere", top);
        top.Delete();
        var toDeleted = () => MemoComment.Create(memo, "Late", top);

        acrossRecords.Should().Throw<InvalidOperationException>();
        toDeleted.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Replies_NestWithoutLimit_WhenTheClassSetsNoMaxDepth()
    {
        var memo = Memo.Create("Deep");
        var top = MemoComment.Create(memo, "Top");
        var reply = MemoComment.Create(memo, "Reply", top);
        var nested = MemoComment.Create(memo, "Nested", reply);

        top.Depth.Should().Be(0);
        reply.Depth.Should().Be(1);
        nested.Depth.Should().Be(2);
        nested.ParentId.Should().Be(reply.Id);
    }

    [Fact]
    public void Replies_StopAtTheClassesMaxDepth()
    {
        var sheet = Sheet.Create("Review");
        var top = SheetComment.Create(sheet, "Top");
        var reply = SheetComment.Create(sheet, "Reply", top);

        var nested = () => SheetComment.Create(sheet, "Nested", reply);

        reply.Depth.Should().Be(1);
        nested.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolve_AndReopen_RecordWhoAndWhen_AndAreRefusedOnReplies()
    {
        var sheet = Sheet.Create("Review");
        var top = SheetComment.Create(sheet, "Top");
        var reply = SheetComment.Create(sheet, "Reply", top);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        top.Resolve(first);
        var resolvedAt = top.ResolutionChangedAt;
        top.Resolve(second);

        top.IsResolved.Should().BeTrue();
        top.ResolutionChangedById.Should().Be(first);
        top.ResolutionChangedAt.Should().Be(resolvedAt);
        top.EditedAt.Should().BeNull();

        top.Reopen(second);

        top.IsResolved.Should().BeFalse();
        top.ResolutionChangedById.Should().Be(second);
        reply.Invoking(c => c.Resolve(first)).Should().Throw<InvalidOperationException>();
        top.Delete();
        top.Invoking(c => c.Resolve(first)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Vote_IsOnePerVoter_CanChange_AndIsRemovedWithTheRecord()
    {
        Guid sheetId;
        using (var context = CreateContext())
        {
            var sheet = Sheet.Create("Votes");
            var comment = SheetComment.Create(sheet, "Top");
            context.AddRange(sheet, comment);
            await context.SaveChangesAsync();
            sheetId = sheet.Id;

            var vote = TestVote.Create(comment, CommentVote<TestUser>.Up);
            context.TestVotes.Add(vote);
            await context.SaveChangesAsync();

            vote.Change(comment, CommentVote<TestUser>.Down);
            await context.SaveChangesAsync();

            context.TestVotes.Add(TestVote.Create(comment, CommentVote<TestUser>.Up));
            var duplicate = async () => await context.SaveChangesAsync();
            await duplicate.Should().ThrowAsync<DbUpdateException>();
        }

        using var reloaded = _database.CreateContext(All, _userId);
        (await reloaded.TestVotes.SingleAsync()).Value.Should().Be(CommentVote<TestUser>.Down);

        reloaded.Remove(await reloaded.Sheets.SingleAsync(s => s.Id == sheetId));
        await reloaded.SaveChangesAsync();

        (await reloaded.TestVotes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void Vote_OnADeletedComment_OrWithAnInvalidValue_IsRefused()
    {
        var sheet = Sheet.Create("Votes");
        var comment = SheetComment.Create(sheet, "Top");

        var invalid = () => TestVote.Create(comment, 2);
        invalid.Should().Throw<ArgumentOutOfRangeException>();

        comment.Delete();
        var deleted = () => TestVote.Create(comment, CommentVote<TestUser>.Up);
        deleted.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Comment_WithBlankText_IsRefused(string text)
    {
        var create = () => MemoComment.Create(Memo.Create("Blank"), text);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Edit_StampsEditedAt_OnlyWhenTheTextChanges()
    {
        using var context = CreateContext();
        var memo = Memo.Create("Edits");
        var comment = MemoComment.Create(memo, "First");
        context.AddRange(memo, comment);
        await context.SaveChangesAsync();

        comment.Edit(" First ");
        comment.EditedAt.Should().BeNull();

        comment.Edit("Second");
        await context.SaveChangesAsync();

        comment.EditedAt.Should().NotBeNull();
        (await context.MemoComments.SingleAsync(c => c.Id == comment.Id)).Text.Should().Be("Second");
    }

    [Fact]
    public async Task Delete_KeepsTheCommentAndItsReplies_AndBlocksFurtherEdits()
    {
        using var context = CreateContext();
        var memo = Memo.Create("Deletes");
        var top = MemoComment.Create(memo, "Top");
        var reply = MemoComment.Create(memo, "Reply", top);
        context.AddRange(memo, top, reply);
        await context.SaveChangesAsync();

        top.Delete();
        var deletedAt = top.DeletedAt;
        top.Delete();
        await context.SaveChangesAsync();

        top.DeletedAt.Should().Be(deletedAt);
        top.IsDeleted.Should().BeTrue();
        top.Invoking(c => c.Edit("Changed")).Should().Throw<InvalidOperationException>();
        (await context.MemoComments.ForMemo(memo.Id).CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task DeletingTheRecord_DeletesItsCommentsAndReplies()
    {
        Guid memoId;
        using (var context = CreateContext())
        {
            var memo = Memo.Create("Gone");
            var top = MemoComment.Create(memo, "Top");
            var reply = MemoComment.Create(memo, "Reply", top);
            context.AddRange(memo, top, reply);
            await context.SaveChangesAsync();
            memoId = memo.Id;
        }

        using var reloaded = _database.CreateContext(All, _userId);
        reloaded.Remove(await reloaded.Memos.SingleAsync(m => m.Id == memoId));
        await reloaded.SaveChangesAsync();

        (await reloaded.MemoComments.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RecordsAreCommentedIndependently()
    {
        using var context = CreateContext();
        var first = Memo.Create("First");
        var second = Memo.Create("Second");
        context.AddRange(first, second, MemoComment.Create(first, "On first"), MemoComment.Create(second, "On second"));
        await context.SaveChangesAsync();

        var comments = await context.MemoComments.ForMemo(first.Id).ToListAsync();

        comments.Should().ContainSingle().Which.Text.Should().Be("On first");
    }
}
