using AQ.Utilities.EntityData;
using AQ.Utilities.Snapshots;
using FluentAssertions;

namespace AQ.Generators.Tests;

public class SnapshotClassTests
{
    private const string Prelude = """
        using System;
        using System.Collections.Generic;
        using AQ.Entities;
        using AQ.Utilities.Snapshots;
        namespace Sample;
        public enum Mode { A, B }
        """;

    [Fact]
    public void Snapshottable_GeneratesSnapshotClassAndRegistersFactory()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Snapshottable(IncludeId = true)]
            public class Widget : Entity { [Snapshot] public string? Title { get; set; } }
            public class SpecialWidget : Widget { }
            """);
        run.CompileErrors.Should().BeEmpty();
        var assembly = run.Load();
        var snapshotType = assembly.GetType("Sample.WidgetSnapshot")!;
        snapshotType.Should().NotBeNull();
        snapshotType.BaseType.Should().Be(typeof(EntitySnapshot));

        var found = SnapshotRegistry.TryGetFactory(assembly.GetType("Sample.SpecialWidget")!, out var factory);
        var id = Guid.NewGuid();
        var snapshot = factory(id, 4, "{\"x\":1}");

        found.Should().BeTrue();
        snapshot.GetType().Should().Be(snapshotType);
        snapshot.EntityId.Should().Be(id);
        snapshot.Version.Should().Be(4);
        snapshot.Data.Should().Be("{\"x\":1}");
    }

    [Fact]
    public void Snapshottable_HonoursSnapshotTypeNameOverride()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Snapshottable(SnapshotTypeName = "WidgetScoreSnapshot")]
            public class Widget : Entity { [Snapshot] public string? Title { get; set; } }
            """);
        run.CompileErrors.Should().BeEmpty();
        var assembly = run.Load();

        assembly.GetType("Sample.WidgetScoreSnapshot").Should().NotBeNull();
        assembly.GetType("Sample.WidgetSnapshot").Should().BeNull();
    }

    [Fact]
    public void TryGetFactory_ReturnsFalseForUnregisteredType()
    {
        SnapshotRegistry.TryGetFactory(typeof(string), out _).Should().BeFalse();
    }

    [Fact]
    public void EnumCollections_WriteStringListsUnlessEnumAsNumber()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Snapshottable]
            public class Widget : Entity
            {
                [Snapshot] public List<Mode> Modes { get; set; } = new();
                [Snapshot] public ICollection<Mode>? Missing { get; set; }
                [Snapshot(EnumAsNumber = true)] public List<Mode> RawModes { get; set; } = new();
            }
            """);
        run.CompileErrors.Should().BeEmpty();
        var assembly = run.Load();
        var type = assembly.GetType("Sample.Widget")!;
        var entity = Activator.CreateInstance(type)!;
        var mode = assembly.GetType("Sample.Mode")!;
        var list = (System.Collections.IList)type.GetProperty("Modes")!.GetValue(entity)!;
        list.Add(Enum.ToObject(mode, 1));
        list.Add(Enum.ToObject(mode, 0));
        ((System.Collections.IList)type.GetProperty("RawModes")!.GetValue(entity)!).Add(Enum.ToObject(mode, 1));

        var data = EntityDataRegistry<SnapshotAttribute>.Build(entity);

        data["modes"].Should().BeEquivalentTo(new List<string> { "B", "A" });
        data["missing"].Should().BeNull();
        EntityDataRegistry<SnapshotAttribute>.Serialize(entity).Should().Contain("\"modes\":[\"B\",\"A\"]").And.Contain("\"rawModes\":[1]");
    }
}
