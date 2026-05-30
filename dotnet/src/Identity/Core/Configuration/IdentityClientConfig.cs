namespace AQ.Identity.Core.Configuration;

public class IdentityClientConfig
{
    public string ClientId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Type { get; set; } = "public";
    public string? ClientSecret { get; set; }
    public bool RequirePkce { get; set; } = false;
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
    /// <summary>
    /// OAuth grant type: "authorization_code" (default) or "client_credentials" (service-to-service).
    /// </summary>
    public string GrantType { get; set; } = "authorization_code";

    /// <summary>
    /// Extra claims to embed in tokens issued to this client (e.g. for service accounts).
    /// Key = claim type, Value = claim value.
    /// </summary>
    public Dictionary<string, string> ServiceAccountClaims { get; set; } = [];
}
