namespace AQ.Entities;

/// <summary>
/// Marks a property as bookkeeping that is derived or recomputed (a validity flag, a last-checked time), not part of
/// what a person edits. A save that changes only such properties of an entity leaves its revision, update time and
/// updater alone, so it neither makes an open edit form stale nor shows up as "last edited". The save still compares
/// against the revision it loaded (when the type uses a revision concurrency token), so it cannot overwrite a newer
/// edit.
/// <para>
/// Use only for a value with one logical writer or one that is recomputed in full: two neutral saves of the same row
/// do not conflict with each other, so a counter or a read-modify-write value would lose updates. Those belong in an
/// atomic update.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
// RULE ENT-9
public sealed class RevisionNeutralAttribute : Attribute;
