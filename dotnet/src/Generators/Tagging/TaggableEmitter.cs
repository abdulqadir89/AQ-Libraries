using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AQ.Generators.Tagging
{
    internal static class TaggableEmitter
    {
        public static void ReportDuplicateNames(SourceProductionContext context, GenerationInput input)
        {
            // RULE GEN-9
            foreach (var group in input.Taggables.Where(t => t.Diagnostics.Length == 0).GroupBy(t => t.Name, System.StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                var classes = string.Join(", ", group.Select(c => c.FullName.Replace("global::", string.Empty)).OrderBy(n => n, System.StringComparer.Ordinal));
                context.ReportDiagnostic(DiagnosticInfo.Create(
                    "AQGEN006",
                    "Taggable classes " + classes + " share the tag name '" + group.Key + "'; set a distinct [Taggable(Name = ...)] on all but one.",
                    null).ToDiagnostic());
            }
        }

        public static void AddSources(SourceProductionContext context, GenerationInput input)
        {
            foreach (var model in input.Taggables.Where(t => t.Diagnostics.Length == 0))
            {
                var hint = Names.HintName(model.FullName);
                context.AddSource(hint + ".Taggable.g.cs", SourceText.From(EmitTaggablePartial(model), Encoding.UTF8));
                context.AddSource(hint + ".TagLink.g.cs", SourceText.From(EmitTagLinkClass(model), Encoding.UTF8));
                context.AddSource(hint + ".TagQueries.g.cs", SourceText.From(EmitTagQueries(model), Encoding.UTF8));
            }
        }

        public static string TagLinkTypeName(TaggableModel model)
        {
            return model.Name + "Tag";
        }

        public static string EmitTaggablePartial(TaggableModel model)
        {
            var link = TagLinkTypeName(model);
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    partial class ").Append(model.ClassName).Append(" : global::AQ.Utilities.Tagging.ITaggable\n    {\n");
            builder.Append("        public global::System.Collections.Generic.ICollection<").Append(link).Append("> Tags { get; private set; } = new global::System.Collections.Generic.List<").Append(link).Append(">();\n\n");
            builder.Append("        public global::System.Collections.Generic.IReadOnlyCollection<global::System.Guid> TagIds =>\n");
            builder.Append("            global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(Tags, t => t.TagId));\n\n");
            builder.Append("        void global::AQ.Utilities.Tagging.ITaggable.AddTagId(global::System.Guid tagId)\n        {\n");
            builder.Append("            if (!global::System.Linq.Enumerable.Any(Tags, t => t.TagId == tagId))\n            {\n");
            builder.Append("                Tags.Add(").Append(link).Append(".Create(Id, tagId));\n            }\n        }\n\n");
            builder.Append("        void global::AQ.Utilities.Tagging.ITaggable.RemoveTagId(global::System.Guid tagId)\n        {\n");
            builder.Append("            var link = global::System.Linq.Enumerable.FirstOrDefault(Tags, t => t.TagId == tagId);\n");
            builder.Append("            if (link is not null)\n            {\n                Tags.Remove(link);\n            }\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static string EmitTagLinkClass(TaggableModel model)
        {
            var link = TagLinkTypeName(model);
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    public sealed partial class ").Append(link).Append(" : global::AQ.Utilities.Tagging.EntityTag<").Append(model.TagType).Append(">\n    {\n");
            builder.Append("        public global::System.Guid ").Append(model.Name).Append("Id { get; private set; }\n");
            builder.Append("        public ").Append(model.FullName).Append("? ").Append(model.Name).Append(" { get; private set; }\n\n");
            builder.Append("        private ").Append(link).Append("() { }\n\n");
            builder.Append("        public static ").Append(link).Append(" Create(global::System.Guid entityId, global::System.Guid tagId)\n        {\n");
            builder.Append("            return new ").Append(link).Append("() { EntityId = entityId, ").Append(model.Name).Append("Id = entityId, TagId = tagId };\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static string EmitTagQueries(TaggableModel model)
        {
            var builder = SourceBuilder.Begin(model.Namespace);
            builder.Append("    public static class ").Append(model.Name).Append("TagQueries\n    {\n");
            builder.Append("        public static global::System.Linq.IQueryable<").Append(model.FullName).Append("> WhereHasAnyTag(this global::System.Linq.IQueryable<").Append(model.FullName)
                .Append("> query, global::System.Collections.Generic.IReadOnlyCollection<global::System.Guid> tagIds)\n        {\n");
            builder.Append("            return global::System.Linq.Queryable.Where(query, e => global::System.Linq.Enumerable.Any(e.Tags, t => global::System.Linq.Enumerable.Contains(tagIds, t.TagId)));\n        }\n\n");
            builder.Append("        public static global::System.Linq.IQueryable<").Append(model.FullName).Append("> WhereHasAllTags(this global::System.Linq.IQueryable<").Append(model.FullName)
                .Append("> query, global::System.Collections.Generic.IEnumerable<global::System.Guid> tagIds)\n        {\n");
            builder.Append("            foreach (var id in global::System.Linq.Enumerable.Distinct(tagIds))\n            {\n");
            builder.Append("                var tagId = id;\n");
            builder.Append("                query = global::System.Linq.Queryable.Where(query, e => global::System.Linq.Enumerable.Any(e.Tags, t => t.TagId == tagId));\n            }\n\n");
            builder.Append("            return query;\n        }\n\n");
            builder.Append("        public static global::System.Linq.IQueryable<").Append(model.FullName).Append("> WhereHasTagNamed(this global::System.Linq.IQueryable<").Append(model.FullName)
                .Append("> query, string text)\n        {\n");
            builder.Append("            return global::System.Linq.Queryable.Where(query, e => global::System.Linq.Enumerable.Any(e.Tags, t => t.Tag!.Name.Contains(text)));\n        }\n");
            builder.Append("    }\n");
            return SourceBuilder.End(builder, model.Namespace);
        }

        public static void AppendRegistrations(StringBuilder builder, GenerationInput input)
        {
            foreach (var model in input.Taggables.Where(t => t.Diagnostics.Length == 0).OrderBy(c => c.FullName, System.StringComparer.Ordinal))
            {
                var linkType = "global::" + (model.Namespace.Length > 0 ? model.Namespace + "." : string.Empty) + TagLinkTypeName(model);
                builder.Append("            global::AQ.Utilities.Tagging.TagRegistry.Register(new global::AQ.Utilities.Tagging.TagDescriptor(\n");
                builder.Append("                \"").Append(model.Name).Append("\", typeof(").Append(model.FullName).Append("), typeof(").Append(linkType).Append("), typeof(").Append(model.TagType).Append("),\n");
                builder.Append("                \"").Append(model.Name).Append("Id\", \"").Append(model.Name).Append("\", \"Tags\"));\n");
            }
        }
    }
}
