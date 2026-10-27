using AQ.Entities;

namespace AQ.Utilities.Commenting;

/// <summary>
/// Base of an app's comment type. The author and time of a comment are its audit creator and creation time.
/// The generated per-record comment class derives from the app's type and adds the link to the commented record.
/// </summary>
public abstract class Comment<TPrincipal> : Entity<TPrincipal>, IComment where TPrincipal : class
{
    public Guid? ParentId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTimeOffset? EditedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt is not null;

    protected Comment() { }

    /// <summary>Sets the text and, for a reply, the comment it answers. The generated create method calls this after checking that both comments belong to the same record.</summary>
    protected void Initialize(string text, IComment? parent)
    {
        Text = Normalize(text);

        if (parent is null)
        {
            return;
        }

        // RULE CMT-2
        if (parent.ParentId is not null)
        {
            throw new InvalidOperationException("A reply answers a top-level comment, not another reply.");
        }

        // RULE CMT-2
        if (parent.IsDeleted)
        {
            throw new InvalidOperationException("A deleted comment cannot be replied to.");
        }

        ParentId = parent.Id;
    }

    // RULE CMT-3
    public void Edit(string text)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("A deleted comment cannot be edited.");
        }

        var normalized = Normalize(text);
        if (normalized == Text)
        {
            return;
        }

        Text = normalized;
        EditedAt = DateTimeOffset.UtcNow;
    }

    // RULE CMT-4
    public void Delete() => DeletedAt ??= DateTimeOffset.UtcNow;

    // RULE CMT-1
    private static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A comment cannot be blank.", nameof(text));
        }

        return trimmed;
    }
}
