using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FluentAssertions;

namespace AQ.Generators.Tests;

public class EntityDataGeneratorTests
{
    [Fact]
    public void Generator_IsIncremental_SecondRunWithUnrelatedChangeIsCached()
    {
        var assemblyName = "Gen" + Guid.NewGuid().ToString("N");
        var compilation = GeneratorHarness.CreateCompilation("""
            using System;
            using AQ.Utilities.Snapshots;
            namespace Sample;
            [Snapshottable]
            public class Widget { [Snapshot] public string? Title { get; set; } }
            """, assemblyName);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new EntityDataGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        var changed = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("namespace Other; public class Unrelated { }"));
        driver = driver.RunGenerators(changed);

        var steps = driver.GetRunResult().Results[0].TrackedSteps["Input"];
        steps.SelectMany(s => s.Outputs).Select(o => o.Reason).Should().OnlyContain(r => r == IncrementalStepRunReason.Cached || r == IncrementalStepRunReason.Unchanged);
    }
}
