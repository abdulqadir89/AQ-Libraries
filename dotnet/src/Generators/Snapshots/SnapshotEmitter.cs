using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AQ.Generators.Snapshots
{
    internal static class SnapshotEmitter
    {
        public static void ReportDuplicateNames(SourceProductionContext context, GenerationInput input)
        {
            // RULE GEN-7
            foreach (var group in input.Snapshottable.GroupBy(c => c.Name, System.StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                var classes = string.Join(", ", group.Select(c => c.FullName.Replace("global::", string.Empty)).OrderBy(n => n, System.StringComparer.Ordinal));
                context.ReportDiagnostic(DiagnosticInfo.Create(
                    "AQGEN005",
                    "Snapshottable classes " + classes + " share the snapshot name '" + group.Key + "'; set a distinct [Snapshottable(Name = ...)] on all but one.",
                    null).ToDiagnostic());
            }
        }

        public static void AddSources(SourceProductionContext context, GenerationInput input)
        {
            foreach (var model in input.Snapshottable)
            {
                // RULE GEN-1
                context.AddSource(Names.HintName(model.FullName) + ".Snapshot.g.cs", SourceText.From(EmitSnapshotClass(model), Encoding.UTF8));
            }
        }

        public static string EmitSnapshotClass(ClassModel model)
        {
            var typeName = SnapshotTypeName(model);
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    public partial class ").Append(typeName).Append(" : global::AQ.Utilities.Snapshots.EntitySnapshot\n    {\n");
            builder.Append("        public global::System.Guid ").Append(model.Name).Append("Id { get; private set; }\n");
            builder.Append("        public ").Append(model.FullName).Append("? ").Append(model.Name).Append(" { get; private set; }\n\n");
            builder.Append("        private ").Append(typeName).Append("() { }\n\n");
            builder.Append("        public static ").Append(typeName).Append(" Create(global::System.Guid entityId, int version, string data)\n");
            builder.Append("        {\n            return new ").Append(typeName).Append("() { EntityId = entityId, ").Append(model.Name).Append("Id = entityId, Version = version, Data = data };\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static string SnapshotTypeName(ClassModel model)
        {
            return model.TypeNameOverride ?? model.Name + "Snapshot";
        }

        public static string SnapshotClassFullName(ClassModel model)
        {
            return "global::" + (model.Namespace.Length > 0 ? model.Namespace + "." : string.Empty) + SnapshotTypeName(model);
        }

        public static void AppendRegistrations(StringBuilder builder, GenerationInput input)
        {
            // RULE GEN-5
            foreach (var model in input.Snapshottable.OrderBy(c => c.FullName, System.StringComparer.Ordinal))
            {
                var snapshotType = SnapshotClassFullName(model);
                builder.Append("            global::AQ.Utilities.Snapshots.SnapshotRegistry.Register(new global::AQ.Utilities.Snapshots.SnapshotDescriptor(\n");
                builder.Append("                \"").Append(model.Name).Append("\", typeof(").Append(model.FullName).Append("), typeof(").Append(snapshotType).Append("),\n");
                builder.Append("                \"").Append(model.Name).Append("Id\", \"").Append(model.Name).Append("\", ").Append(model.KeepLast).Append(", ").Append(model.MaxAgeDays).Append(",\n");
                builder.Append("                static (id, version, data) => ").Append(snapshotType).Append(".Create(id, version, data)));\n");
            }
        }
    }
}
