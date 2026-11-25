namespace AQ.Utilities.Attachments;

/// <summary>
/// Named presets of MIME content types, combinable as flags on <see cref="AttachableAttribute.ContentKind"/>.
/// Attribute arguments must be compile-time constants, so presets are expressed as flags rather than
/// string arrays; resolve the actual MIME list with <see cref="AttachmentLimits"/>.
/// </summary>
[Flags]
public enum AttachmentContentKind
{
    None = 0,
    Images = 1,
    Documents = 2,
    Audio = 4,
    Video = 8,
}
