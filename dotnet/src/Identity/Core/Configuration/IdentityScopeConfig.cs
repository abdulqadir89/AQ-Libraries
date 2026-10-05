namespace AQ.Identity.Core.Configuration;

/// <summary>
/// A scope's resources (OpenIddict scope descriptor <c>Resources</c>). The authorize and token
/// endpoints turn the granted scopes' resources into the access token's <c>aud</c> claim, so a
/// token only works at the APIs its scopes were meant for.
/// </summary>
public class IdentityScopeConfig
{
    public string Name { get; set; } = default!;
    public List<string> Resources { get; set; } = [];
}
