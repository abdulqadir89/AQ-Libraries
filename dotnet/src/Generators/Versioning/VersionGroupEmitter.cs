using System.Collections.Generic;
using System.Linq;
using System.Text;
using AQ.Generators.Snapshots;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AQ.Generators.Versioning
{
    internal static class VersionGroupEmitter
    {
        public static void AddSources(SourceProductionContext context, GenerationInput input)
        {
            foreach (var model in input.VersionGroups.Where(g => g.Diagnostics.Length == 0))
            {
                context.AddSource(Names.HintName(model.FullName) + ".VersionGroup.g.cs", SourceText.From(EmitVersionGroupPartial(model), Encoding.UTF8));
            }
        }

        public static string EmitVersionGroupPartial(VersionGroupModel model)
        {
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    partial class ").Append(model.Name).Append(" : global::AQ.Utilities.Versioning.IVersionGrouped\n    {\n");
            builder.Append("        public global::System.Guid VersionGroupId { get; private set; }\n\n");
            if (model.Verifiable)
            {
                builder.Append("        [global::AQ.Utilities.Verification.Verifiable]\n");
            }

            builder.Append("        public string? Version { get; private set; }\n\n");
            builder.Append("        void global::AQ.Utilities.Versioning.IVersionGrouped.SetVersionGroup(global::System.Guid versionGroupId, string? version)\n        {\n");
            builder.Append("            VersionGroupId = versionGroupId;\n            Version = version;\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static IEnumerable<MemberResult> SnapshotMembers(GenerationInput input)
        {
            // RULE VSN-9
            foreach (var model in input.VersionGroups.Where(g => g.Snapshotted))
            {
                var statements = new[] { "d[\"versionGroupId\"] = e.VersionGroupId;", "d[\"version\"] = e.Version;" };
                yield return new MemberResult(new MemberModel(model.FullName, "~" + model.FullName, statements.ToEquatableArray()), default);
            }
        }
    }
}
