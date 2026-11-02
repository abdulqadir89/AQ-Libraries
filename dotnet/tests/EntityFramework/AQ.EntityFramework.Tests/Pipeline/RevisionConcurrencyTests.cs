using AQ.EntityFramework.Extensions;
using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Pipeline;

public class RevisionConcurrencyTests : IDisposable
{
    private static readonly AqSavePipelineOptions Audit = new() { Audit = true };

    private readonly PipelineTestDatabase _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    private PipelineTestContext CreateContext() => _database.CreateContext(Audit, _userId);

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
    public async Task Save_WithExpectedRevisionMatchingTheRow_Succeeds()
    {
        var id = await SeedInvoiceAsync();
        using var context = CreateContext();
        var invoice = await context.Invoices.SingleAsync(i => i.Id == id);

        context.ExpectRevision(invoice, invoice.Revision);
        invoice.Rename("INV-2");
        await context.SaveChangesAsync();

        invoice.Revision.Should().Be(1);
    }

    [Fact]
    public async Task Save_WithExpectedRevisionOlderThanTheRow_ThrowsConcurrencyException()
    {
        var id = await SeedInvoiceAsync();
        using (var other = CreateContext())
        {
            var changed = await other.Invoices.SingleAsync(i => i.Id == id);
            changed.Rename("changed elsewhere");
            await other.SaveChangesAsync();
        }

        using var context = CreateContext();
        var invoice = await context.Invoices.SingleAsync(i => i.Id == id);
        context.ExpectRevision(invoice, 0);
        invoice.Rename("INV-2");

        var save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Save_WithExpectedRevisionOlderThanTheRowAndNoOtherChange_ThrowsConcurrencyException()
    {
        var id = await SeedInvoiceAsync();
        using (var other = CreateContext())
        {
            var changed = await other.Invoices.SingleAsync(i => i.Id == id);
            changed.Rename("changed elsewhere");
            await other.SaveChangesAsync();
        }

        using var context = CreateContext();
        var invoice = await context.Invoices.SingleAsync(i => i.Id == id);
        context.ExpectRevision(invoice, 0);

        var save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task TwoContextsLoadingTheSameRow_SecondSaveThrowsConcurrencyException()
    {
        var id = await SeedInvoiceAsync();
        using var first = CreateContext();
        using var second = CreateContext();
        var a = await first.Invoices.SingleAsync(i => i.Id == id);
        var b = await second.Invoices.SingleAsync(i => i.Id == id);

        a.Rename("first");
        await first.SaveChangesAsync();
        b.Rename("second");
        var save = () => second.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
