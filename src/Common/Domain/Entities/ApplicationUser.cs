namespace AQ.Common.Domain.Entities;

public abstract class ApplicationUser : Entity
{
    public string DisplayName { get; private set; } = default!;
}
