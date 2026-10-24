namespace AQ.Utilities.Tagging;

/// <summary>Marks a generated tag link row; links are immutable, so the save pipeline never treats one as an update.</summary>
public interface IEntityTag
{
    Guid EntityId { get; }
    Guid TagId { get; }
}
