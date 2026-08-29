namespace AQ.Identity.Core.Configuration;

public class IdentityClientConfig
{
    public string ClientId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Type { get; set; } = "public";
    public string? ClientSecret { get; set; }
    public bool RequirePkce { get; set; } = true;

    /// <summary>
    /// True for clients owned by the same organization operating this IdP (e.g. the IdP's own
    /// web/mobile apps). First-party clients skip the OAuth consent screen — per OIDC Core the
    /// consent step exists to inform the end user which third party is requesting access, which
    /// doesn't apply to the operator's own applications. Defaults to false (secure by default);
    /// any newly registered or externally-owned client shows consent.
    /// </summary>
    public bool IsFirstParty { get; set; }

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
