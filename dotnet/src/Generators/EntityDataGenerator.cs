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

            var snapshottable = context.SyntaxProvider
                .ForAttributeWithMetadataName(Names.SnapshottableAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => Parser.ParseClass(ctx, Names.SnapshotAttribute, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var versionGroups = context.SyntaxProvider
                .ForAttributeWithMetadataName(Names.VersionGroupAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => Parser.ParseVersionGroup(ctx, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var taggables = context.SyntaxProvider
                .ForAttributeWithMetadataName(Names.TaggableAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => Parser.ParseTaggable(ctx, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "Assembly");

            var input = assemblyName
                .Combine(snapshotMembers)
                .Combine(snapshottable)
                .Combine(versionGroups)
                .Combine(taggables)
                .Select(static (pair, _) => new GenerationInput(pair.Left.Left.Left.Left, pair.Left.Left.Left.Right, pair.Left.Left.Right, pair.Left.Right, pair.Right))
                .WithTrackingName("Input");

            context.RegisterSourceOutput(input, static (spc, model) => Execute(spc, model));
        }

        private static string HintName(string fullName)
        {
            return fullName.Replace("global::", string.Empty).Replace('<', '_').Replace('>', '_');
        }

        private static void Execute(SourceProductionContext context, GenerationInput input)
        {
            foreach (var diagnostic in input.SnapshotMembers.SelectMany(m => m.Diagnostics)
                .Concat(input.Snapshottable.SelectMany(c => c.Diagnostics))
                .Concat(input.VersionGroups.SelectMany(c => c.Diagnostics))
                .Concat(input.Taggables.SelectMany(c => c.Diagnostics)))
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            // RULE GEN-7
            foreach (var group in input.Snapshottable.GroupBy(c => c.Name, System.StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                var classes = string.Join(", ", group.Select(c => c.FullName.Replace("global::", string.Empty)).OrderBy(n => n, System.StringComparer.Ordinal));
                context.ReportDiagnostic(DiagnosticInfo.Create(
                    "AQGEN005",
                    "Snapshottable classes " + classes + " share the snapshot name '" + group.Key + "'; set a distinct [Snapshottable(Name = ...)] on all but one.",
                    null).ToDiagnostic());
            }

            // RULE GEN-9
            foreach (var group in input.Taggables.Where(t => t.Diagnostics.Length == 0).GroupBy(t => t.Name, System.StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                var classes = string.Join(", ", group.Select(c => c.FullName.Replace("global::", string.Empty)).OrderBy(n => n, System.StringComparer.Ordinal));
                context.ReportDiagnostic(DiagnosticInfo.Create(
                    "AQGEN006",
                    "Taggable classes " + classes + " share the tag name '" + group.Key + "'; set a distinct [Taggable(Name = ...)] on all but one.",
                    null).ToDiagnostic());
            }

            if (!Emitter.HasOutput(input))
            {
                return;
            }

            context.AddSource("AQGeneratedRegistrations.g.cs", SourceText.From(Emitter.EmitRegistrations(input), Encoding.UTF8));

            foreach (var model in input.Snapshottable)
            {
                // RULE GEN-1
                context.AddSource(HintName(model.FullName) + ".Snapshot.g.cs", SourceText.From(Emitter.EmitSnapshotClass(model), Encoding.UTF8));
            }

            foreach (var model in input.VersionGroups.Where(g => g.Diagnostics.Length == 0))
            {
                context.AddSource(HintName(model.FullName) + ".VersionGroup.g.cs", SourceText.From(Emitter.EmitVersionGroupPartial(model), Encoding.UTF8));
            }

            foreach (var model in input.Taggables.Where(t => t.Diagnostics.Length == 0))
            {
                var hint = HintName(model.FullName);
                context.AddSource(hint + ".Taggable.g.cs", SourceText.From(Emitter.EmitTaggablePartial(model), Encoding.UTF8));
                context.AddSource(hint + ".TagLink.g.cs", SourceText.From(Emitter.EmitTagLinkClass(model), Encoding.UTF8));
                context.AddSource(hint + ".TagQueries.g.cs", SourceText.From(Emitter.EmitTagQueries(model), Encoding.UTF8));
            }
        }
    }
}
