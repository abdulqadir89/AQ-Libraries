namespace AQ.Identity.OpenIddict.Management.Dto;

public class IdentityScopeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ClaimTypes { get; set; } = [];
}

public class UpsertIdentityScopeRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ClaimTypes { get; set; } = [];
}

public class UpdateIdentityScopeRequest : UpsertIdentityScopeRequest
{
    public string Id { get; set; } = string.Empty;
}
