namespace AQ.Identity.Core.Entities;

public class SigningKey
{
    public Guid Id { get; private set; }

    public string KeyId { get; set; } = string.Empty;

    public string EncryptedKeyXml { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RetiredAt { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    public bool IsRetired => RetiredAt.HasValue && DateTimeOffset.UtcNow > RetiredAt;

    public SigningKey() { }

    public SigningKey(string keyId, string encryptedKeyXml, DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        KeyId = keyId;
        EncryptedKeyXml = encryptedKeyXml;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }
}
