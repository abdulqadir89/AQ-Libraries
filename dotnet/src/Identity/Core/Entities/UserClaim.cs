namespace AQ.Identity.Core.Entities;

public class UserClaim
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public UserClaim() { }

    private UserClaim(Guid userId, string type, string value)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Value = value;
    }

    public static UserClaim Create(Guid userId, string type, string value)
        => new(userId, type, value);

    public void UpdateValue(string value) => Value = value;
}
