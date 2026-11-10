using AQ.EntityFramework.Extensions;
using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Pipeline;

public class RevisionNeutralTests : IDisposable
{
    private static readonly AqSavePipelineOptions Options = new() { Audit = true, Snapshots = true };

    private readonly PipelineTestDatabase _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    private PipelineTestContext CreateContext() => _database.CreateContext(Options, _userId);

    private async Task<Guid> SeedInvoiceAsync()
    {
        using var context = CreateContext();
        context.Users.Add(new TestUser { Id = _userId });
        var invoice = Invoice.Create("INV-1");
        context.Add(invoice);
        await context.SaveChangesAsync();
        return invoice.Id;
    }

    [Fact]
    public async Task Save_ChangingOnlyNeutralProperties_KeepsRevisionAndUpdateStamp()
    {
        var id = await SeedInvoiceAsync();
        DateTimeOffset? updatedAt;
        using (var read = CreateContext())
        {
            updatedAt = (await read.Invoices.SingleAsync(i => i.Id == id)).UpdatedAt;
        }

        using (var context = CreateContext())
        {
            var invoice = await context.Invoices.SingleAsync(i => i.Id == id);
            invoice.SetFlag("valid");
            invoice.CountCheck();
            await context.SaveChangesAsync();
        }

        using var verify = CreateContext();
        var stored = await verify.Invoices.SingleAsync(i => i.Id == id);
        stored.Flag.Should().Be("valid");
        stored.Checks.Should().Be(1);
        stored.Revision.Should().Be(0);
        stored.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public async Task Save_ChangingNeutralAndOtherProperties_RaisesRevisionOnce()
    {
        var id = await SeedInvoiceAsync();

        using (var context = CreateContext())
        {
            var invoice = await context.Invoices.SingleAsync(i => i.Id == id);
            invoice.Rename("INV-2");
            invoice.SetFlag("valid");
            await context.SaveChangesAsync();
        }

        using var verify = CreateContext();
        (await verify.Invoices.SingleAsync(i => i.Id == id)).Revision.Should().Be(1);
    }

    [Fact]
    public async Task Save_ChangingOnlyNeutralPropertiesAfterTouch_RaisesRevision()
    {
        var id = await SeedInvoiceAsync();

        using (var context = CreateContext())
        {
            var invoice = await context.Invoices.SingleAsync(i => i.Id == id);
            context.TouchRevision(invoice);
            invoice.SetFlag("valid");
            await context.SaveChangesAsync();
        }

        using var verify = CreateContext();
        (await verify.Invoices.SingleAsync(i => i.Id == id)).Revision.Should().Be(1);
    }

    [Fact]
    public async Task Save_ChangingOnlyNeutralProperties_WritesNoSnapshot()
    {
        var id = await SeedInvoiceAsync();

        using (var context = CreateContext())
        {
            var invoice = await context.Invoices.SingleAsync(i => i.Id == id);
            invoice.SetFlag("valid");
            await context.SaveChangesAsync();
        }

        using var verify = CreateContext();
        (await verify.InvoiceSnapshots.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Save_ChangingOnlyNeutralProperties_StillFailsWhenTheRowWasEditedSinceItWasLoaded()
    {
        var id = await SeedInvoiceAsync();
        using var context = CreateContext();
        var invoice = await context.Invoices.SingleAsync(i => i.Id == id);

        using (var other = CreateContext())
        {
            var edited = await other.Invoices.SingleAsync(i => i.Id == id);
            edited.Rename("edited elsewhere");
            await other.SaveChangesAsync();
        }

        invoice.SetFlag("valid");
        var save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Save_EditingAfterAnotherSaveChangedOnlyNeutralProperties_Succeeds()
    {
        var id = await SeedInvoiceAsync();
        using var context = CreateContext();
        var invoice = await context.Invoices.SingleAsync(i => i.Id == id);

        using (var other = CreateContext())
        {
            var checkedRow = await other.Invoices.SingleAsync(i => i.Id == id);
            checkedRow.SetFlag("valid");
            await other.SaveChangesAsync();
        }

        context.ExpectRevision(invoice, 0);
        invoice.Rename("INV-2");
        await context.SaveChangesAsync();

        invoice.Revision.Should().Be(1);
    }

    [Fact]
    public async Task Save_OfTwoNeutralChangesToTheSameRow_BothSucceed()
    {
        // Documented limit: neutral saves do not conflict with each other, so the attribute is for recomputed values only.
        var id = await SeedInvoiceAsync();
        using var first = CreateContext();
        using var second = CreateContext();
        var a = await first.Invoices.SingleAsync(i => i.Id == id);
        var b = await second.Invoices.SingleAsync(i => i.Id == id);

        a.SetFlag("first");
        await first.SaveChangesAsync();
        b.SetFlag("second");
        var save = () => second.SaveChangesAsync();

        await save.Should().NotThrowAsync();
    }
}
