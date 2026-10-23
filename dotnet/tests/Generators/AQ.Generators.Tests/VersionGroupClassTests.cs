using AQ.Utilities.EntityData;
using AQ.Utilities.Snapshots;
using AQ.Utilities.Verification;
using AQ.Utilities.Versioning;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace AQ.Generators.Tests;

public class VersionGroupClassTests
{
    private const string Prelude = """
        using System;
        using AQ.Entities;
        using AQ.Utilities.Snapshots;
        using AQ.Utilities.Verification;
        using AQ.Utilities.Versioning;
        namespace Sample;
        public class AppUser { public Guid Id { get; set; } }
        """;

    [Fact]
    public void VersionGroup_EmitsMembersAndInterface()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [VersionGroup]
            public partial class Manual : Entity<AppUser> { }
            """);

        run.CompileErrors.Should().BeEmpty();
        var type = run.Load().GetType("Sample.Manual")!;
        type.GetInterfaces().Should().Contain(typeof(IVersionGrouped));
        type.GetProperty("VersionGroupId")!.PropertyType.Should().Be<Guid>();
        type.GetProperty("Version")!.PropertyType.Should().Be<string>();
        type.GetProperty("VersionGroupId")!.SetMethod!.IsPrivate.Should().BeTrue();
        type.GetProperty("Version")!.GetCustomAttributes(typeof(VerifiableAttribute), false).Should().BeEmpty();
    }

    [Fact]
    public void VersionGroup_SetVersionGroupWritesBothMembers()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [VersionGroup]
            public partial class Manual : Entity<AppUser> { }
            """);
        var manual = (IVersionGrouped)Activator.CreateInstance(run.Load().GetType("Sample.Manual")!)!;
        var group = Guid.NewGuid();

        manual.SetVersionGroup(group, "2.0");

        manual.VersionGroupId.Should().Be(group);
        manual.Version.Should().Be("2.0");
    }

    [Fact]
    public void VersionGroup_VersionIsVerifiableWhenEntityIsVerifiable()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [VersionGroup]
            public partial class Manual : Entity<AppUser>, IVerifiable { public VerificationState Verification { get; private set; } = new(); }
            """);

        run.CompileErrors.Should().BeEmpty();
        var type = run.Load().GetType("Sample.Manual")!;
        type.GetProperty("Version")!.GetCustomAttributes(typeof(VerifiableAttribute), false).Should().HaveCount(1);
        type.GetProperty("VersionGroupId")!.GetCustomAttributes(typeof(VerifiableAttribute), false).Should().BeEmpty();
    }

    [Fact]
    public void VersionGroup_AddsMembersToSnapshotWhenSnapshottable()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [VersionGroup]
            [Snapshottable]
            public partial class Manual : Entity<AppUser> { [Snapshot] public string? Title { get; set; } }
            """);

        run.CompileErrors.Should().BeEmpty();
        var manual = Activator.CreateInstance(run.Load().GetType("Sample.Manual")!)!;
        ((IVersionGrouped)manual).SetVersionGroup(Guid.Parse("11111111-1111-1111-1111-111111111111"), "v1");

        var data = EntityDataRegistry<SnapshotAttribute>.Build(manual);

        data["versionGroupId"].Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        data["version"].Should().Be("v1");
        data.Should().ContainKey("title");
    }

    [Fact]
    public void VersionGroup_AddsMembersToSnapshotOfBaseHierarchy()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Snapshottable]
            public abstract class Item : Entity<AppUser> { [Snapshot] public string? Name { get; set; } }
            [VersionGroup]
            public partial class Manual : Item { }
            """);

        run.CompileErrors.Should().BeEmpty();
        var manual = Activator.CreateInstance(run.Load().GetType("Sample.Manual")!)!;
        ((IVersionGrouped)manual).SetVersionGroup(Guid.NewGuid(), "v9");

        EntityDataRegistry<SnapshotAttribute>.Build(manual)["version"].Should().Be("v9");
    }

    [Fact]
    public void VersionGroup_WithoutSnapshottableLeavesSnapshotsAlone()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [VersionGroup]
            public partial class Manual : Entity<AppUser> { }
            """);

        run.CompileErrors.Should().BeEmpty();
        run.RunResult.GeneratedTrees.Select(t => t.FilePath).Should().NotContain(p => p.Contains("Snapshot"));
    }

    [Theory]
    [InlineData("[VersionGroup] public class Manual : Entity<AppUser> { }")]
    [InlineData("[VersionGroup] public partial class Manual<T> : Entity<AppUser> { }")]
    [InlineData("public partial class Outer { [VersionGroup] public partial class Manual : Entity<AppUser> { } }")]
    [InlineData("[VersionGroup] public partial class Manual { }")]
    public void VersionGroup_InvalidClassReportsAqgen002(string declaration)
    {
        var run = GeneratorHarness.Run(Prelude + "\n" + declaration);

        run.GeneratorDiagnostics.Should().Contain(d => d.Id == "AQGEN002" && d.Severity == DiagnosticSeverity.Error);
    }
}
