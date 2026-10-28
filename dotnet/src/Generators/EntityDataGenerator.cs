using System.Linq;
using System.Text;
using AQ.Generators.Commenting;
using AQ.Generators.EntityData;
using AQ.Generators.Snapshots;
using AQ.Generators.Tagging;
using AQ.Generators.Versioning;
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
                .ForAttributeWithMetadataName(SnapshotNames.SnapshotAttribute, static (node, _) => node is PropertyDeclarationSyntax, static (ctx, ct) => SnapshotParser.ParseMember(ctx, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var snapshottable = context.SyntaxProvider
                .ForAttributeWithMetadataName(SnapshotNames.SnapshottableAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => SnapshotParser.ParseClass(ctx, SnapshotNames.SnapshotAttribute, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var versionGroups = context.SyntaxProvider
                .ForAttributeWithMetadataName(VersioningNames.VersionGroupAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => VersionGroupParser.Parse(ctx, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var taggables = context.SyntaxProvider
                .ForAttributeWithMetadataName(TaggingNames.TaggableAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => TaggableParser.Parse(ctx, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var commentables = context.SyntaxProvider
                .ForAttributeWithMetadataName(CommentingNames.CommentableAttribute, static (node, _) => node is ClassDeclarationSyntax, static (ctx, ct) => CommentableParser.Parse(ctx, ct))
                .Collect()
                .Select(static (items, _) => items.ToEquatableArray());

            var assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "Assembly");

            var input = assemblyName
                .Combine(snapshotMembers)
                .Combine(snapshottable)
                .Combine(versionGroups)
                .Combine(taggables)
                .Combine(commentables)
                .Select(static (pair, _) => new GenerationInput(pair.Left.Left.Left.Left.Left, pair.Left.Left.Left.Left.Right, pair.Left.Left.Left.Right, pair.Left.Left.Right, pair.Left.Right, pair.Right))
                .WithTrackingName("Input");

            context.RegisterSourceOutput(input, static (spc, model) => Execute(spc, model));
        }

        private static void Execute(SourceProductionContext context, GenerationInput input)
        {
            foreach (var diagnostic in input.SnapshotMembers.SelectMany(m => m.Diagnostics)
                .Concat(input.Snapshottable.SelectMany(c => c.Diagnostics))
                .Concat(input.VersionGroups.SelectMany(c => c.Diagnostics))
                .Concat(input.Taggables.SelectMany(c => c.Diagnostics))
                .Concat(input.Commentables.SelectMany(c => c.Diagnostics)))
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            SnapshotEmitter.ReportDuplicateNames(context, input);
            TaggableEmitter.ReportDuplicateNames(context, input);
            CommentableEmitter.ReportDuplicateNames(context, input);

            if (!RegistrationsEmitter.HasOutput(input))
            {
                return;
            }

            context.AddSource("AQGeneratedRegistrations.g.cs", SourceText.From(RegistrationsEmitter.Emit(input), Encoding.UTF8));

            SnapshotEmitter.AddSources(context, input);
            VersionGroupEmitter.AddSources(context, input);
            TaggableEmitter.AddSources(context, input);
            CommentableEmitter.AddSources(context, input);
        }
    }
}
