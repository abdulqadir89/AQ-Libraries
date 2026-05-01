namespace AQ.Identity.Core.Configuration;

public class IdentityClientConfig
{
    public string ClientId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Type { get; set; } = "public";
    public string? ClientSecret { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
}
