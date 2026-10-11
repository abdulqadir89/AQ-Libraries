using AQ.Utilities.EntityData;
using AQ.Utilities.Versioning;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace AQ.Generators.Tests;

public class VersionClassTests
{
    private const string Prelude = """
        using System;
        using AQ.Entities;
        using AQ.Utilities.Versioning;
        namespace Sample;
        public class AppUser { public Guid Id { get; set; } }
        """;

    [Fact]
    public void Versionable_GeneratesVersionClassAndRegistersDescriptor()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Versionable(IncludeId = true)]
            public class Order : Entity<AppUser> { [Versioned] public string? Title { get; set; } }
            """);

        run.CompileErrors.Should().BeEmpty();
        var assembly = run.Load();
        var orderType = assembly.GetType("Sample.Order")!;
        var versionType = assembly.GetType("Sample.OrderVersion")!;
        versionType.Should().NotBeNull();
        versionType.BaseType!.GetGenericTypeDefinition().Should().Be(typeof(EntityVersion<>));
        versionType.BaseType.GetGenericArguments()[0].Should().Be(assembly.GetType("Sample.AppUser"));

        var descriptor = VersionRegistry.TryGet(orderType)!;
        descriptor.EntityTypeName.Should().Be("Order");
        descriptor.VersionType.Should().Be(versionType);
        descriptor.ForeignKeyPropertyName.Should().Be("OrderId");
        descriptor.NavigationPropertyName.Should().Be("Order");
        VersionRegistry.TryGet("Order").Should().BeSameAs(descriptor);

        var id = Guid.NewGuid();
        var version = descriptor.Factory(id, 3, "lbl", "{}", 2);
        version.GetType().Should().Be(versionType);
        versionType.GetProperty("OrderId")!.GetValue(version).Should().Be(id);
        versionType.GetProperty("VersionNumber")!.GetValue(version).Should().Be(3);
        versionType.GetProperty("Label")!.GetValue(version).Should().Be("lbl");
        versionType.GetProperty("SourceVersionNumber")!.GetValue(version).Should().Be(2);
        versionType.GetMethod("GetEntityId")!.Invoke(version, null).Should().Be(id);
    }

    [Fact]
    public void Versionable_WritesVersionedMembersIncludingId()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Versionable(IncludeId = true)]
            public class Order : Entity<AppUser> { [Versioned] public string? Title { get; set; } }
            """);
        var assembly = run.Load();
        var order = Activator.CreateInstance(assembly.GetType("Sample.Order")!)!;
        order.GetType().GetProperty("Title")!.SetValue(order, "T");

        var data = EntityDataRegistry<VersionedAttribute>.Build(order);

        data.Keys.Should().Equal("id", "title");
    }

    [Fact]
    public void Diagnostic_AQGEN002_WhenNotAuditable()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Versionable]
            public class Plain { [Versioned] public string? Title { get; set; } }
            """);

        run.GeneratorDiagnostics.Should().ContainSingle(d => d.Id == "AQGEN002" && d.Severity == DiagnosticSeverity.Error);
    }
}
