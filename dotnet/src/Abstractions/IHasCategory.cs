namespace AQ.Abstractions;

public interface IHasCategory<TCategory>
{
    /// <summary>
    /// Current category of the entity.
    /// </summary>
    TCategory Category { get; }

    /// <summary>
    /// Sets the category of the entity.
    /// </summary>
    /// <param name="category">The category to set.</param>
    void SetCategory(TCategory category);
}
