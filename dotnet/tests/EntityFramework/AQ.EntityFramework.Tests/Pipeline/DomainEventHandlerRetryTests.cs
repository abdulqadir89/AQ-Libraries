using AQ.Events.Dispatchers;
using AQ.Events.Domain;
using AQ.EntityFramework.Extensions;
using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AQ.EntityFramework.Tests.Pipeline;

public class DomainEventHandlerRetryTests : IDisposable
{
    private static readonly AqSavePipelineOptions Audit = new() { Audit = true };

    private sealed record InvoiceChanged(Guid InvoiceId) : DomainEvent(InvoiceId);

    private sealed class Calls
    {
        public int First;
        public int Second;
        public Func<int, Task> SecondBehaviour = _ => Task.CompletedTask;
    }

    private sealed class FirstHandler(Calls calls) : IDomainEventHandler<InvoiceChanged>
    {
        public Task HandleAsync(InvoiceChanged domainEvent, CancellationToken cancellationToken = default)
        {
            calls.First++;
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler(Calls calls) : IDomainEventHandler<InvoiceChanged>
    {
        public Task HandleAsync(InvoiceChanged domainEvent, CancellationToken cancellationToken = default) =>
            calls.SecondBehaviour(++calls.Second);
    }

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

    private static ServiceProvider Provider(PipelineTestContext context, Calls calls, bool withRetry, int maxAttempts = 3, bool useTransaction = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(calls);
        services.AddSingleton(context);
        services.AddSingleton<IDomainEventDispatcher>(provider => new DomainEventDispatcher(
            provider, NullLogger<DomainEventDispatcher>.Instance, provider.GetService<IDomainEventHandlerInvoker>()));
        services.AddSingleton<IDomainEventHandler<InvoiceChanged>, FirstHandler>();
        services.AddSingleton<IDomainEventHandler<InvoiceChanged>, SecondHandler>();
        if (withRetry)
        {
            services.AddDomainEventHandlerConcurrencyRetry<PipelineTestContext>(maxAttempts, useTransaction);
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task HandlerLosingARace_IsRunAgainAloneAndTheHandlersBeforeItAreNot()
    {
        var calls = new Calls { SecondBehaviour = call => call == 1 ? throw new DbUpdateConcurrencyException("lost") : Task.CompletedTask };
        using var context = CreateContext();
        using var provider = Provider(context, calls, withRetry: true);

        await provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));

        calls.First.Should().Be(1);
        calls.Second.Should().Be(2);
    }

    [Fact]
    public async Task HandlerLosingEveryRace_IsRunUpToTheLimitThenTheFailureIsRethrown()
    {
        var calls = new Calls { SecondBehaviour = _ => throw new DbUpdateConcurrencyException("lost") };
        using var context = CreateContext();
        using var provider = Provider(context, calls, withRetry: true, maxAttempts: 4);

        var dispatch = () => provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));

        await dispatch.Should().ThrowAsync<DbUpdateConcurrencyException>();
        calls.First.Should().Be(1);
        calls.Second.Should().Be(4);
    }

    [Fact]
    public async Task HandlerFailingForAnotherReason_IsNotRunAgain()
    {
        var calls = new Calls { SecondBehaviour = _ => throw new InvalidOperationException("not a race") };
        using var context = CreateContext();
        using var provider = Provider(context, calls, withRetry: true);

        var dispatch = () => provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));

        await dispatch.Should().ThrowAsync<InvalidOperationException>();
        calls.Second.Should().Be(1);
    }

    [Fact]
    public async Task WithoutTheRetryRegistered_AHandlerLosingARace_FailsTheDispatchAtOnce()
    {
        var calls = new Calls { SecondBehaviour = _ => throw new DbUpdateConcurrencyException("lost") };
        using var context = CreateContext();
        using var provider = Provider(context, calls, withRetry: false);

        var dispatch = () => provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));

        await dispatch.Should().ThrowAsync<DbUpdateConcurrencyException>();
        calls.Second.Should().Be(1);
    }

    [Fact]
    public async Task HandlerLosingARealRace_SeesTheWinnersValuesWhenItRunsAgain()
    {
        var id = await SeedInvoiceAsync();
        using var context = CreateContext();
        var seenRevisions = new List<int>();
        var calls = new Calls();
        calls.SecondBehaviour = async call =>
        {
            var invoice = await context.Invoices.SingleAsync(i => i.Id == id);
            seenRevisions.Add(invoice.Revision);
            if (call == 1)
            {
                using var other = CreateContext();
                var changed = await other.Invoices.SingleAsync(i => i.Id == id);
                changed.Rename("changed elsewhere");
                await other.SaveChangesAsync();
            }

            invoice.Rename($"handler {call}");
            await context.SaveChangesAsync();
        };
        using var provider = Provider(context, calls, withRetry: true);

        await provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(id));

        calls.Second.Should().Be(2);
        seenRevisions.Should().Equal(0, 1);
        using var check = CreateContext();
        (await check.Invoices.SingleAsync(i => i.Id == id)).Revision.Should().Be(2);
    }

    [Fact]
    public void RetryLimitBelowOne_IsRefused()
    {
        var register = () => new ServiceCollection().AddDomainEventHandlerConcurrencyRetry<PipelineTestContext>(0);

        register.Should().Throw<ArgumentOutOfRangeException>();
    }

    private async Task SeedUserAsync()
    {
        using var context = CreateContext();
        context.Users.Add(new TestUser { Id = _userId });
        await context.SaveChangesAsync();
    }

    private static async Task SaveNewInvoiceAsync(PipelineTestContext context, string number)
    {
        context.Add(Invoice.Create(number));
        await context.SaveChangesAsync();
    }

    private int InvoicesNumbered(string number)
    {
        using var context = CreateContext();
        return context.Invoices.Count(i => i.Number == number);
    }

    [Fact]
    public async Task WithATransaction_AHandlerThatSavedThenLostARace_LeavesNothingFromItsFirstRun()
    {
        await SeedUserAsync();
        var calls = new Calls();
        using var context = CreateContext();
        calls.SecondBehaviour = async call =>
        {
            await SaveNewInvoiceAsync(context, "tx-race");
            if (call == 1)
            {
                throw new DbUpdateConcurrencyException("lost");
            }
        };
        using var provider = Provider(context, calls, withRetry: true, useTransaction: true);

        await provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));

        calls.Second.Should().Be(2);
        InvoicesNumbered("tx-race").Should().Be(1);
    }

    [Fact]
    public async Task WithoutATransaction_AHandlerThatSavedThenLostARace_LeavesTheSaveOfItsFirstRun()
    {
        await SeedUserAsync();
        var calls = new Calls();
        using var context = CreateContext();
        calls.SecondBehaviour = async call =>
        {
            await SaveNewInvoiceAsync(context, "no-tx-race");
            if (call == 1)
            {
                throw new DbUpdateConcurrencyException("lost");
            }
        };
        using var provider = Provider(context, calls, withRetry: true, useTransaction: false);

        await provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));

        InvoicesNumbered("no-tx-race").Should().Be(2);
    }

    [Fact]
    public async Task WithATransaction_AHandlerThatSavedThenFailedForAnotherReason_RollsBackItsSave()
    {
        await SeedUserAsync();
        var calls = new Calls();
        using var context = CreateContext();
        calls.SecondBehaviour = async _ =>
        {
            await SaveNewInvoiceAsync(context, "tx-failure");
            throw new InvalidOperationException("not a race");
        };
        using var provider = Provider(context, calls, withRetry: true, useTransaction: true);

        var dispatch = () => provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));

        await dispatch.Should().ThrowAsync<InvalidOperationException>();
        InvoicesNumbered("tx-failure").Should().Be(0);
    }

    [Fact]
    public async Task WithATransaction_AHandlerThatSucceeds_CommitsItsSaves()
    {
        await SeedUserAsync();
        var calls = new Calls();
        using var context = CreateContext();
        calls.SecondBehaviour = _ => SaveNewInvoiceAsync(context, "tx-ok");
        using var provider = Provider(context, calls, withRetry: true, useTransaction: true);

        await provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));

        InvoicesNumbered("tx-ok").Should().Be(1);
    }

    [Fact]
    public async Task WithATransaction_ATransactionAlreadyOpen_IsJoinedAndLeftToItsOwner()
    {
        await SeedUserAsync();
        var calls = new Calls();
        using var context = CreateContext();
        calls.SecondBehaviour = _ => SaveNewInvoiceAsync(context, "outer-tx");
        using var provider = Provider(context, calls, withRetry: true, useTransaction: true);

        await using (var outer = await context.Database.BeginTransactionAsync())
        {
            await provider.GetRequiredService<IDomainEventDispatcher>().DispatchAsync(new InvoiceChanged(Guid.NewGuid()));
            context.Database.CurrentTransaction.Should().BeSameAs(outer);
            await outer.RollbackAsync();
        }

        InvoicesNumbered("outer-tx").Should().Be(0);
    }
}
