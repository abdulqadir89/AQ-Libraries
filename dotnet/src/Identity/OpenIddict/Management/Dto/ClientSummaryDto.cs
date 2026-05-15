namespace AQ.Identity.OpenIddict.Management.Dto;

public class ClientSummaryDto
{
    public string ClientId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ClientType { get; set; }
    public IEnumerable<string> RedirectUris { get; set; } = [];
    public IEnumerable<string> Scopes { get; set; } = [];
}
