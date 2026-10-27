using AQ.Generators.Commenting;
using AQ.Generators.Snapshots;
using AQ.Generators.Tagging;
using AQ.Generators.Versioning;

namespace AQ.Generators
{
    internal sealed record GenerationInput(
        string AssemblyName,
        EquatableArray<MemberResult> SnapshotMembers,
        EquatableArray<ClassModel> Snapshottable,
        EquatableArray<VersionGroupModel> VersionGroups,
        EquatableArray<TaggableModel> Taggables,
        EquatableArray<CommentableModel> Commentables);
}
