using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AQ.Generators
{
    [Generator(LanguageNames.CSharp)]
    public sealed class EntityDataGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var snapshotMembers = context.SyntaxProvider
                .ForAttributeWithMetadataName(Names.SnapshotAttribute, static (node, _) => node is PropertyDeclarationSyntax, static (ctx, ct) => Parser.ParseMember(ctx, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var versionedMembers = context.SyntaxProvider
                .ForAttributeWithMetadataName(Names.VersionedAttribute, static (node, _) => node is PropertyDeclarationSyntax, static (ctx, ct) => Parser.ParseMember(ctx, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var snapshottable = context.SyntaxProvider
                .ForAttributeWithMetadataName(Names.SnapshottableAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => Parser.ParseClass(ctx, Names.SnapshotAttribute, false, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var versionable = context.SyntaxProvider
                .ForAttributeWithMetadataName(Names.VersionableAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => Parser.ParseClass(ctx, Names.VersionedAttribute, true, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "Assembly");

            var input = assemblyName
                .Combine(snapshotMembers)
                .Combine(versionedMembers)
                .Combine(snapshottable)
                .Combine(versionable)
                .Select(static (pair, _) => new GenerationInput(pair.Left.Left.Left.Left, pair.Left.Left.Left.Right, pair.Left.Left.Right, pair.Left.Right, pair.Right))
                .WithTrackingName("Input");

            context.RegisterSourceOutput(input, static (spc, model) => Execute(spc, model));
        }

        private static void Execute(SourceProductionContext context, GenerationInput input)
        {
            foreach (var diagnostic in input.SnapshotMembers.SelectMany(m => m.Diagnostics)
                .Concat(input.VersionedMembers.SelectMany(m => m.Diagnostics))
                .Concat(input.Snapshottable.SelectMany(c => c.Diagnostics))
                .Concat(input.Versionable.SelectMany(c => c.Diagnostics)))
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            if (!Emitter.HasOutput(input))
            {
                return;
            }

            context.AddSource("AQGeneratedRegistrations.g.cs", SourceText.From(Emitter.EmitRegistrations(input), Encoding.UTF8));
        }
    }
}
