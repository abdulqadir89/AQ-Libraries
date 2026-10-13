using AQ.Actors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AQ.Actors.Tests;

public class ActorContextTests
{
    private static Actor Human() => new(Guid.NewGuid(), PrincipalKind.Human);
    private static Actor System() => new(Guid.NewGuid(), PrincipalKind.System);

    [Fact]
    public void Current_throws_when_no_actor()
    {
        var ctx = new ActorContext();

        var act = () => ctx.Current;

        act.Should().Throw<MissingActorException>();
        ctx.TryGet(out _).Should().BeFalse();
    }

    [Fact]
    public void Begin_sets_actor_and_dispose_restores_previous()
    {
        var ctx = new ActorContext();
        var outer = Human();
        var inner = System();

        using (ctx.Begin(outer))
        {
            ctx.Current.Should().Be(outer);
            using (ctx.Begin(inner))
            {
                ctx.Current.Should().Be(inner);
            }

            ctx.Current.Should().Be(outer);
        }

        ctx.TryGet(out _).Should().BeFalse();
    }

    [Fact]
    public void Out_of_order_dispose_does_not_resurrect_disposed_actor()
    {
        var ctx = new ActorContext();
        var a = Human();
        var b = System();

        var first = ctx.Begin(a);
        var second = ctx.Begin(b);

        first.Dispose();
        ctx.Current.Should().Be(b);

        second.Dispose();
        ctx.TryGet(out _).Should().BeFalse();
    }

    [Fact]
    public void Dispose_twice_is_safe()
    {
        var ctx = new ActorContext();
        var outer = Human();
        using var _ = ctx.Begin(outer);
        var inner = ctx.Begin(System());

        inner.Dispose();
        inner.Dispose();

        ctx.Current.Should().Be(outer);
    }

    [Fact]
    public async Task Parallel_tasks_do_not_leak_actors()
    {
        var ctx = new ActorContext();
        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var actor = Human();
            using var scope = ctx.Begin(actor);
            await Task.Delay(Random.Shared.Next(1, 20));
            ctx.Current.Should().Be(actor);
        });

        await Task.WhenAll(tasks);

        ctx.TryGet(out _).Should().BeFalse();
    }

    [Fact]
    public async Task Actor_set_in_child_task_does_not_leak_to_parent()
    {
        var ctx = new ActorContext();

        await Task.Run(() => ctx.Begin(Human()));

        ctx.TryGet(out _).Should().BeFalse();
    }

    private sealed class Probe : IAsyncDisposable
    {
        public bool Disposed { get; private set; }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }

    [Fact]
    public async Task Scope_factory_runs_work_under_actor_and_disposes_both_scopes()
    {
        var services = new ServiceCollection();
        services.AddAqActors();
        services.AddScoped<Probe>();
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IActorScopeFactory>();
        var ctx = provider.GetRequiredService<IActorContext>();
        var actor = System();
        Probe? probe = null;

        var seen = await factory.RunAsync(actor, (sp, _) =>
        {
            probe = sp.GetRequiredService<Probe>();
            return Task.FromResult(ctx.Current);
        });

        seen.Should().Be(actor);
        probe!.Disposed.Should().BeTrue();
        ctx.TryGet(out _).Should().BeFalse();
    }

    [Fact]
    public async Task Scope_factory_restores_actor_when_work_throws()
    {
        var services = new ServiceCollection();
        services.AddAqActors();
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IActorScopeFactory>();
        var ctx = provider.GetRequiredService<IActorContext>();

        var act = () => factory.RunAsync(System(), (_, _) => throw new InvalidOperationException("boom"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        ctx.TryGet(out _).Should().BeFalse();
    }
}
