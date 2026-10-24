using AQ.Utilities.EntityData;
using AQ.Utilities.Snapshots;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace AQ.Generators.Tests.Snapshots;

public class SnapshotMemberTests
{
    private const string Prelude = """
        using System;
        using AQ.Utilities.Snapshots;
                namespace Sample;
        public enum Color { Red, Green }
        public class Money { public string? Currency { get; set; } public int Amount { get; set; } }
        public struct Triple { public int A { get; set; } public int B { get; set; } }
        public class Holder { public string? Value { get; set; } public int Number { get; set; } }
        public class Product { public string? Vendor { get; set; } public string? Name { get; set; } public int Rank { get; set; } }
        public class Tag { public string Text { get; set; } = ""; public override string ToString() => "tag:" + Text; }
        """;

    private static (object Entity, Type Type, GeneratorRun Run) Build(string classes, string typeName, Action<object>? configure = null)
    {
        var run = GeneratorHarness.Run(Prelude + "\n" + classes);
        run.CompileErrors.Should().BeEmpty();
        var assembly = run.Load();
        var type = assembly.GetType("Sample." + typeName)!;
        var entity = Activator.CreateInstance(type)!;
        configure?.Invoke(entity);
        return (entity, type, run);
    }

    private static void Set(object target, string property, object? value) =>
        target.GetType().GetProperty(property)!.SetValue(target, value);

    private static object New(object sibling, string typeName, Action<object>? configure = null)
    {
        var instance = Activator.CreateInstance(sibling.GetType().Assembly.GetType("Sample." + typeName)!)!;
        configure?.Invoke(instance);
        return instance;
    }

    [Fact]
    public void Scalar_UsesCamelCasePropertyName()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Widget { [Snapshot] public string? Title { get; set; } [Snapshot] public int Count { get; set; } [Snapshot] public Guid RefId { get; set; } public string? Ignored { get; set; } }
            """, "Widget", e => { Set(e, "Title", "T"); Set(e, "Count", 3); });

        var data = EntityDataRegistry<SnapshotAttribute>.Build(entity);

        data.Keys.Should().Equal("title", "count", "refId");
        data["title"].Should().Be("T");
        data["count"].Should().Be(3);
    }

    [Fact]
    public void Name_OverridesKey()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Widget { [Snapshot(Name = "assetTag")] public string? Tag { get; set; } }
            """, "Widget", e => Set(e, "Tag", "A1"));

        EntityDataRegistry<SnapshotAttribute>.Build(entity).Should().ContainKey("assetTag").WhoseValue.Should().Be("A1");
    }

    [Fact]
    public void Enum_WritesStringByDefault_AndNumberWhenRequested()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Widget
            {
                [Snapshot] public Color Shade { get; set; }
                [Snapshot] public Color? Maybe { get; set; }
                [Snapshot] public Color? MaybeNull { get; set; }
                [Snapshot(EnumAsNumber = true)] public Color Raw { get; set; }
                [Snapshot(EnumAsNumber = true)] public Color? RawNullable { get; set; }
            }
            """, "Widget", e =>
        {
            var color = e.GetType().Assembly.GetType("Sample.Color")!;
            Set(e, "Shade", Enum.ToObject(color, 1));
            Set(e, "Maybe", Enum.ToObject(color, 1));
            Set(e, "Raw", Enum.ToObject(color, 1));
            Set(e, "RawNullable", Enum.ToObject(color, 1));
        });

        var data = EntityDataRegistry<SnapshotAttribute>.Build(entity);

        data["shade"].Should().Be("Green");
        data["maybe"].Should().Be("Green");
        data["maybeNull"].Should().BeNull();
        data["raw"].Should().NotBeOfType<string>();
        data["rawNullable"].Should().NotBeOfType<string>();
        EntityDataRegistry<SnapshotAttribute>.Serialize(entity).Should().Contain("\"raw\":1").And.Contain("\"rawNullable\":1").And.Contain("\"shade\":\"Green\"");
    }

    [Fact]
    public void AsString_UsesToString_WithNullPropagationOnlyWhenNullable()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Widget
            {
                [Snapshot(AsString = true)] public Tag? Label { get; set; }
                [Snapshot(AsString = true)] public Tag? Missing { get; set; }
                [Snapshot(AsString = true)] public int Number { get; set; }
                [Snapshot(AsString = true)] public int? MaybeNumber { get; set; }
            }
            """, "Widget", e =>
        {
            Set(e, "Label", New(e, "Tag", t => Set(t, "Text", "x")));
            Set(e, "Number", 7);
        });

        var data = EntityDataRegistry<SnapshotAttribute>.Build(entity);

        data["label"].Should().Be("tag:x");
        data["missing"].Should().BeNull();
        data["number"].Should().Be("7");
        data["maybeNumber"].Should().BeNull();
    }

    [Fact]
    public void Member_WritesMemberOfValue()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Widget
            {
                [Snapshot(Member = "Value")] public Holder? Holder { get; set; }
                [Snapshot(Member = "Value")] public Holder? Absent { get; set; }
                [Snapshot(Member = "A")] public Triple Triple { get; set; }
                [Snapshot(Member = "A")] public Triple? MaybeTriple { get; set; }
            }
            """, "Widget", e =>
        {
            Set(e, "Holder", New(e, "Holder", h => Set(h, "Value", "v")));
            Set(e, "Triple", New(e, "Triple", t => Set(t, "A", 4)));
            Set(e, "MaybeTriple", New(e, "Triple", t => Set(t, "A", 5)));
        });

        var data = EntityDataRegistry<SnapshotAttribute>.Build(entity);

        data["holder"].Should().Be("v");
        data["absent"].Should().BeNull();
        data["triple"].Should().Be(4);
        data["maybeTriple"].Should().Be(5);
    }

    [Fact]
    public void Include_WritesPrefixedKeysAndNullPropagates()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Widget
            {
                [Snapshot(Include = ["Vendor", "Name"])] public Product? Product { get; set; }
                [Snapshot(Include = ["Name"])] public Product? Other { get; set; }
            }
            """, "Widget", e => Set(e, "Product", New(e, "Product", p => { Set(p, "Vendor", "V"); Set(p, "Name", "N"); })));

        var data = EntityDataRegistry<SnapshotAttribute>.Build(entity);

        data.Keys.Should().Equal("productVendor", "productName", "otherName");
        data["productVendor"].Should().Be("V");
        data["productName"].Should().Be("N");
        data["otherName"].Should().BeNull();
    }

    [Fact]
    public void Nested_WritesDictionary_NullCheckedOnlyWhenNullable()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Widget
            {
                [Snapshot(Nested = ["Currency", "Amount"])] public Money? Price { get; set; }
                [Snapshot(Nested = ["Currency"])] public Money? NoPrice { get; set; }
                [Snapshot(Nested = ["A", "B"])] public Triple Pair { get; set; }
                [Snapshot(Nested = ["A"])] public Triple? MaybePair { get; set; }
                [Snapshot(Nested = ["A"])] public Triple? NoPair { get; set; }
            }
            """, "Widget", e =>
        {
            Set(e, "Price", New(e, "Money", m => { Set(m, "Currency", "USD"); Set(m, "Amount", 9); }));
            Set(e, "Pair", New(e, "Triple", t => { Set(t, "A", 1); Set(t, "B", 2); }));
            Set(e, "MaybePair", New(e, "Triple", t => Set(t, "A", 3)));
        });

        var data = EntityDataRegistry<SnapshotAttribute>.Build(entity);

        data["price"].Should().BeEquivalentTo(new Dictionary<string, object?> { ["currency"] = "USD", ["amount"] = 9 });
        data["noPrice"].Should().BeNull();
        data["pair"].Should().BeEquivalentTo(new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 });
        data["maybePair"].Should().BeEquivalentTo(new Dictionary<string, object?> { ["a"] = 3 });
        data["noPair"].Should().BeNull();
    }

    [Fact]
    public void Inheritance_WritesBaseMembersBeforeDerived()
    {
        var (entity, _, _) = Build("""
            public class Root { [Snapshot] public string? RootValue { get; set; } }
            public class Middle : Root { [Snapshot] public string? MiddleValue { get; set; } }
            [Snapshottable]
            public class Leaf : Middle { [Snapshot] public string? LeafValue { get; set; } }
            """, "Leaf");

        EntityDataRegistry<SnapshotAttribute>.Build(entity).Keys.Should().Equal("rootValue", "middleValue", "leafValue");
    }

    [Fact]
    public void Inheritance_DerivedRuntimeTypeUsesRegisteredBaseWriters()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Baseline { [Snapshot] public string? BaseValue { get; set; } }
            public class Special : Baseline { [Snapshot] public string? SpecialValue { get; set; } }
            """, "Special");

        EntityDataRegistry<SnapshotAttribute>.Build(entity).Keys.Should().Equal("baseValue", "specialValue");
        EntityDataRegistry<SnapshotAttribute>.HasWriters(entity.GetType()).Should().BeTrue();
    }

    [Fact]
    public void IncludeId_WritesIdFirst_EvenWithoutOwnMembers()
    {
        var (entity, _, _) = Build("""
            [Snapshottable(IncludeId = true)]
            public class Widget { public Guid Id { get; set; } [Snapshot] public string? Title { get; set; } }
            """, "Widget", e => Set(e, "Id", Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var data = EntityDataRegistry<SnapshotAttribute>.Build(entity);

        data.Keys.Should().Equal("id", "title");
        data["id"].Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public void Serialize_UsesCamelCaseJson()
    {
        var (entity, _, _) = Build("""
            [Snapshottable]
            public class Widget { [Snapshot(Nested = ["Currency"])] public Money? Price { get; set; } [Snapshot] public int Count { get; set; } }
            """, "Widget", e => { Set(e, "Count", 2); Set(e, "Price", New(e, "Money", m => Set(m, "Currency", "EUR"))); });

        EntityDataRegistry<SnapshotAttribute>.Serialize(entity).Should().Be("{\"price\":{\"currency\":\"EUR\"},\"count\":2}");
    }

    [Fact]
    public void Diagnostic_AQGEN001_WhenNoDecoratedMembers()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Snapshottable]
            public class Empty { public string? Title { get; set; } }
            """);

        run.GeneratorDiagnostics.Should().ContainSingle(d => d.Id == "AQGEN001" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Diagnostic_AQGEN001_NotReportedWhenBaseHasMembers()
    {
        var run = GeneratorHarness.Run(Prelude + """
            public class Root { [Snapshot] public string? Title { get; set; } }
            [Snapshottable]
            public class Leaf : Root { }
            """);

        run.GeneratorDiagnostics.Should().NotContain(d => d.Id == "AQGEN001");
    }

    [Theory]
    [InlineData("[Snapshot(Include = [\"Missing\"])] public Product? P { get; set; }")]
    [InlineData("[Snapshot(Member = \"Missing\")] public Holder? P { get; set; }")]
    [InlineData("[Snapshot(Nested = [\"Missing\"])] public Money? P { get; set; }")]
    public void Diagnostic_AQGEN003_WhenReferencedMemberMissing(string property)
    {
        var run = GeneratorHarness.Run(Prelude + $$"""
            [Snapshottable]
            public class Widget { {{property}} }
            """);

        run.GeneratorDiagnostics.Should().ContainSingle(d => d.Id == "AQGEN003" && d.Severity == DiagnosticSeverity.Error);
    }
}
