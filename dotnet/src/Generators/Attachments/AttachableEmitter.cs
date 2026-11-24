using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AQ.Generators.Attachments
{
    internal static class AttachableEmitter
    {
        public static void ReportDuplicateNames(SourceProductionContext context, GenerationInput input)
        {
            // RULE GEN-13
            foreach (var group in input.Attachables.Where(t => t.Diagnostics.Length == 0).GroupBy(t => t.Name, System.StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                var classes = string.Join(", ", group.Select(c => c.FullName.Replace("global::", string.Empty)).OrderBy(n => n, System.StringComparer.Ordinal));
                context.ReportDiagnostic(DiagnosticInfo.Create(
                    "AQGEN007",
                    "Attachable classes " + classes + " share the attachment name '" + group.Key + "'; set a distinct [Attachable(Name = ...)] on all but one.",
                    null).ToDiagnostic());
            }
        }

        public static void AddSources(SourceProductionContext context, GenerationInput input)
        {
            foreach (var model in input.Attachables.Where(t => t.Diagnostics.Length == 0))
            {
                var hint = Names.HintName(model.FullName);
                context.AddSource(hint + ".Attachment.g.cs", SourceText.From(EmitAttachmentClass(model), Encoding.UTF8));
                context.AddSource(hint + ".AttachmentQueries.g.cs", SourceText.From(EmitAttachmentQueries(model), Encoding.UTF8));
            }
        }

        public static string AttachmentTypeName(AttachableModel model)
        {
            return model.Name + "Attachment";
        }

        public static string EmitAttachmentClass(AttachableModel model)
        {
            var type = AttachmentTypeName(model);
            var parameter = Names.ToCamelCase(model.Name);
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    public sealed partial class ").Append(type).Append(" : ").Append(model.AttachmentType).Append("\n    {\n");
            builder.Append("        public static readonly global::AQ.Utilities.Attachments.AttachmentLimits Limits = ").Append(LimitsExpression(model)).Append(";\n\n");
            builder.Append("        public global::System.Guid? ").Append(model.Name).Append("Id { get; private set; }\n");
            builder.Append("        public ").Append(model.FullName).Append("? ").Append(model.Name).Append(" { get; private set; }\n\n");
            builder.Append("        private ").Append(type).Append("() { }\n\n");

            builder.Append("        public static ").Append(type).Append(" Create(").Append(model.FullName).Append(' ').Append(parameter)
                .Append(", string fileName, long size, string? contentType = null, global::System.DateTimeOffset? fileModifiedAt = null)\n        {\n");
            builder.Append("            global::System.ArgumentNullException.ThrowIfNull(").Append(parameter).Append(");\n");
            builder.Append("            Limits.EnsureFileAllowed(fileName, size, contentType);\n");
            builder.Append("            var attachment = new ").Append(type).Append("() { ").Append(model.Name).Append("Id = ").Append(parameter).Append(".Id };\n");
            builder.Append("            attachment.Initialize(").Append(parameter).Append(".Id, false, fileName, size, contentType, fileModifiedAt);\n");
            builder.Append("            return attachment;\n        }\n\n");

            builder.Append("        /// <summary>An attachment of a record that does not exist yet; it stays pending until <see cref=\"Link\"/> is called.</summary>\n");
            builder.Append("        public static ").Append(type).Append(" CreateStaged(global::System.Guid ownerId, string fileName, long size, string? contentType = null, global::System.DateTimeOffset? fileModifiedAt = null)\n        {\n");
            builder.Append("            Limits.EnsureFileAllowed(fileName, size, contentType);\n");
            builder.Append("            var attachment = new ").Append(type).Append("();\n");
            builder.Append("            attachment.Initialize(ownerId, true, fileName, size, contentType, fileModifiedAt);\n");
            builder.Append("            return attachment;\n        }\n\n");

            builder.Append("        /// <summary>A linked attachment when only the id of the record is at hand.</summary>\n");
            builder.Append("        public static ").Append(type).Append(" CreateLinked(global::System.Guid ownerId, string fileName, long size, string? contentType = null, global::System.DateTimeOffset? fileModifiedAt = null)\n        {\n");
            builder.Append("            Limits.EnsureFileAllowed(fileName, size, contentType);\n");
            builder.Append("            var attachment = new ").Append(type).Append("() { ").Append(model.Name).Append("Id = ownerId };\n");
            builder.Append("            attachment.Initialize(ownerId, false, fileName, size, contentType, fileModifiedAt);\n");
            builder.Append("            return attachment;\n        }\n\n");

            // RULE ATT-3
            builder.Append("        public override void Link()\n        {\n");
            builder.Append("            ").Append(model.Name).Append("Id = OwnerId;\n");
            builder.Append("            MarkLinked();\n        }\n\n");

            builder.Append("        public override void Replace(long size, string? contentType, global::System.DateTimeOffset? fileModifiedAt)\n        {\n");
            builder.Append("            Limits.EnsureFileAllowed(FileName, size, contentType);\n");
            builder.Append("            ReplaceFile(size, contentType, fileModifiedAt);\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static string EmitAttachmentQueries(AttachableModel model)
        {
            var type = AttachmentTypeName(model);
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    public static class ").Append(type).Append("Queries\n    {\n");
            builder.Append("        public static global::System.Linq.IQueryable<").Append(type).Append("> For").Append(model.Name).Append("(this global::System.Linq.IQueryable<").Append(type)
                .Append("> query, global::System.Guid ownerId)\n        {\n");
            builder.Append("            return global::System.Linq.Queryable.Where(query, a => a.OwnerId == ownerId);\n        }\n\n");
            builder.Append("        public static global::System.Linq.IQueryable<").Append(type).Append("> Pending(this global::System.Linq.IQueryable<").Append(type).Append("> query)\n        {\n");
            builder.Append("            return global::System.Linq.Queryable.Where(query, a => a.IsPending);\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static void AppendRegistrations(StringBuilder builder, GenerationInput input)
        {
            foreach (var model in input.Attachables.Where(t => t.Diagnostics.Length == 0).OrderBy(c => c.FullName, System.StringComparer.Ordinal))
            {
                var attachmentType = "global::" + (model.Namespace.Length > 0 ? model.Namespace + "." : string.Empty) + AttachmentTypeName(model);
                builder.Append("            global::AQ.Utilities.Attachments.AttachmentRegistry.Register(new global::AQ.Utilities.Attachments.AttachmentDescriptor(\n");
                builder.Append("                \"").Append(model.Name).Append("\", typeof(").Append(model.FullName).Append("), typeof(").Append(attachmentType).Append("), typeof(").Append(model.AttachmentType).Append("),\n");
                builder.Append("                \"").Append(model.Name).Append("Id\", \"").Append(model.Name).Append("\", ").Append(attachmentType).Append(".Limits,\n");
                builder.Append("                ").Append(attachmentType).Append(".CreateLinked, ").Append(attachmentType).Append(".CreateStaged, ").Append(model.CascadeDelete ? "true" : "false").Append("));\n");
            }
        }

        private static string LimitsExpression(AttachableModel model)
        {
            return "new global::AQ.Utilities.Attachments.AttachmentLimits("
                + model.MaxCount + ", " + model.MaxFileSizeBytes + "L, (global::AQ.Utilities.Attachments.AttachmentContentKind)" + model.ContentKind + ", "
                + StringArray(model.ExtraContentTypes) + ", " + StringArray(model.AllowedExtensions) + ")";
        }

        private static string StringArray(EquatableArray<string> values)
        {
            if (values.Length == 0)
            {
                return "null";
            }

            return "new string[] { " + string.Join(", ", values.Select(v => "\"" + v.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")) + " }";
        }
    }
}
