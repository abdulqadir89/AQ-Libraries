using AQ.Entities;

namespace AQ.Utilities.Tagging;

public interface ITag : IEntity
{
    string Name { get; }
}
