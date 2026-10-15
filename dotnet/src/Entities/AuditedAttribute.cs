namespace AQ.Entities;

/// <summary>
/// Marks an entity class whose inserts, updates and deletes are written to the append-only audit log by the
/// save pipeline (<c>AqSavePipelineOptions.AuditLog</c>). Inherited by derived types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
// RULE ENT-8
public sealed class AuditedAttribute : Attribute;

/// <summary>
/// Keeps a property out of the audit log's recorded changes (password hashes, large HTML bodies, secrets).
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
// RULE ENT-8
public sealed class AuditIgnoreAttribute : Attribute;
