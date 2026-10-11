using System.Collections.Immutable;
using System.Reflection;
using AQ.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AQ.Generators.Tests;

internal sealed class GeneratorRun
{
    public required string AssemblyName { get; init; }
    public required Compilation Output { get; init; }
    public required ImmutableArray<Diagnostic> GeneratorDiagnostics { get; init; }
    public required GeneratorDriverRunResult RunResult { get; init; }

    public IEnumerable<Diagnostic> CompileErrors =>
        Output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);

    public Assembly Load()
    {
        using var stream = new MemoryStream();
        var emit = Output.Emit(stream);
        if (!emit.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        }

        var assembly = Assembly.Load(stream.ToArray());
        var registrations = assembly.GetType($"{AssemblyName}.Generated.AQGeneratedRegistrations")
            ?? throw new InvalidOperationException("AQGeneratedRegistrations not generated");
        registrations.GetMethod("EnsureRegistered")!.Invoke(null, null);
        return assembly;
    }
}

internal static class GeneratorHarness
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> References = new(() =>
    {
        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return [.. trusted.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    });

    public static CSharpCompilation CreateCompilation(string source, string assemblyName) =>
        CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    public static GeneratorRun Run(string source, string? assemblyName = null)
    {
        assemblyName ??= "Gen" + Guid.NewGuid().ToString("N");
        var compilation = CreateCompilation(source, assemblyName);
        var driver = CSharpGeneratorDriver.Create(
            [new EntityDataGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        return new GeneratorRun
        {
            AssemblyName = assemblyName,
            Output = output,
            GeneratorDiagnostics = diagnostics,
            RunResult = driver.GetRunResult(),
        };
    }
}
