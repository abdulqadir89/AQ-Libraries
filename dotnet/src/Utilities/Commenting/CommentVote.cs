using AQ.Entities;

namespace AQ.Utilities.Commenting;

/// <summary>
/// Base of an app's vote type: one up or down vote on a comment. The voter is the audit creator, so a voter has at most one vote per comment.
/// </summary>
public abstract class CommentVote<TPrincipal> : Entity<TPrincipal> where TPrincipal : class
{
    public const int Up = 1;
    public const int Down = -1;

    public Guid CommentId { get; private set; }
    public int Value { get; private set; }

    protected CommentVote() { }

    /// <summary>Sets the comment and the value. The app's create method calls this.</summary>
    protected void Initialize(IComment comment, int value)
    {
        CheckVotable(comment);

        CommentId = comment.Id;
        Value = Validate(value);
    }

    public void Change(IComment comment, int value)
    {
        CheckVotable(comment);

        if (comment.Id != CommentId)
        {
            throw new InvalidOperationException("The vote belongs to another comment.");
        }

        Value = Validate(value);
    }

    private static void CheckVotable(IComment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        // RULE CMT-8
        if (comment.IsDeleted)
        {
            throw new InvalidOperationException("A deleted comment cannot be voted on.");
        }
    }

    // RULE CMT-7
    private static int Validate(int value) =>
        value is Up or Down ? value : throw new ArgumentOutOfRangeException(nameof(value), "A vote is up (1) or down (-1).");
}
