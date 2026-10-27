using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AQ.Generators.Commenting
{
    internal static class CommentableEmitter
    {
        public static void ReportDuplicateNames(SourceProductionContext context, GenerationInput input)
        {
            // RULE GEN-11
            foreach (var group in input.Commentables.Where(t => t.Diagnostics.Length == 0).GroupBy(t => t.Name, System.StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                var classes = string.Join(", ", group.Select(c => c.FullName.Replace("global::", string.Empty)).OrderBy(n => n, System.StringComparer.Ordinal));
                context.ReportDiagnostic(DiagnosticInfo.Create(
                    "AQGEN007",
                    "Commentable classes " + classes + " share the comment name '" + group.Key + "'; set a distinct [Commentable(Name = ...)] on all but one.",
                    null).ToDiagnostic());
            }
        }

        public static void AddSources(SourceProductionContext context, GenerationInput input)
        {
            foreach (var model in input.Commentables.Where(t => t.Diagnostics.Length == 0))
            {
                var hint = Names.HintName(model.FullName);
                context.AddSource(hint + ".Comment.g.cs", SourceText.From(EmitCommentClass(model), Encoding.UTF8));
                context.AddSource(hint + ".CommentQueries.g.cs", SourceText.From(EmitCommentQueries(model), Encoding.UTF8));
            }
        }

        public static string CommentTypeName(CommentableModel model)
        {
            return model.Name + "Comment";
        }

        public static string EmitCommentClass(CommentableModel model)
        {
            var type = CommentTypeName(model);
            var parameter = Names.ToCamelCase(model.Name);
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    public sealed partial class ").Append(type).Append(" : ").Append(model.CommentType).Append("\n    {\n");
            builder.Append("        public global::System.Guid ").Append(model.Name).Append("Id { get; private set; }\n");
            builder.Append("        public ").Append(model.FullName).Append("? ").Append(model.Name).Append(" { get; private set; }\n\n");
            builder.Append("        private ").Append(type).Append("() { }\n\n");
            builder.Append("        public static ").Append(type).Append(" Create(").Append(model.FullName).Append(' ').Append(parameter).Append(", string text, ").Append(type).Append("? parent = null)\n        {\n");
            builder.Append("            global::System.ArgumentNullException.ThrowIfNull(").Append(parameter).Append(");\n");
            // RULE CMT-2
            builder.Append("            if (parent is not null && parent.").Append(model.Name).Append("Id != ").Append(parameter).Append(".Id)\n            {\n");
            builder.Append("                throw new global::System.InvalidOperationException(\"A reply belongs to the same record as the comment it answers.\");\n            }\n\n");
            builder.Append("            var comment = new ").Append(type).Append("() { ").Append(model.Name).Append("Id = ").Append(parameter).Append(".Id };\n");
            builder.Append("            comment.Initialize(text, parent);\n");
            builder.Append("            return comment;\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static string EmitCommentQueries(CommentableModel model)
        {
            var type = CommentTypeName(model);
            var idParameter = Names.ToCamelCase(model.Name) + "Id";
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    public static class ").Append(type).Append("Queries\n    {\n");
            builder.Append("        public static global::System.Linq.IQueryable<").Append(type).Append("> For").Append(model.Name).Append("(this global::System.Linq.IQueryable<").Append(type)
                .Append("> query, global::System.Guid ").Append(idParameter).Append(")\n        {\n");
            builder.Append("            return global::System.Linq.Queryable.Where(query, c => c.").Append(model.Name).Append("Id == ").Append(idParameter).Append(");\n        }\n\n");
            builder.Append("        public static global::System.Linq.IQueryable<").Append(type).Append("> TopLevel(this global::System.Linq.IQueryable<").Append(type).Append("> query)\n        {\n");
            builder.Append("            return global::System.Linq.Queryable.Where(query, c => c.ParentId == null);\n        }\n\n");
            builder.Append("        public static global::System.Linq.IQueryable<").Append(type).Append("> RepliesTo(this global::System.Linq.IQueryable<").Append(type)
                .Append("> query, global::System.Collections.Generic.IReadOnlyCollection<global::System.Guid> parentIds)\n        {\n");
            builder.Append("            return global::System.Linq.Queryable.Where(query, c => c.ParentId != null && global::System.Linq.Enumerable.Contains(parentIds, c.ParentId.Value));\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static void AppendRegistrations(StringBuilder builder, GenerationInput input)
        {
            foreach (var model in input.Commentables.Where(t => t.Diagnostics.Length == 0).OrderBy(c => c.FullName, System.StringComparer.Ordinal))
            {
                var commentType = "global::" + (model.Namespace.Length > 0 ? model.Namespace + "." : string.Empty) + CommentTypeName(model);
                builder.Append("            global::AQ.Utilities.Commenting.CommentRegistry.Register(new global::AQ.Utilities.Commenting.CommentDescriptor(\n");
                builder.Append("                \"").Append(model.Name).Append("\", typeof(").Append(model.FullName).Append("), typeof(").Append(commentType).Append("), typeof(").Append(model.CommentType).Append("),\n");
                builder.Append("                \"").Append(model.Name).Append("Id\", \"").Append(model.Name).Append("\"));\n");
            }
        }
    }
}
