using AQ.Identity.Core.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AQ.Identity.UI.Pages.Apps;

[Authorize]
public class AppsIndexModel : PageModel
{
    public IReadOnlyList<AppEntry> Apps { get; private set; } = [];

    public IActionResult OnGet([FromServices] IReadOnlyList<IdentityClientConfig> clients)
    {
        Apps = clients
            .Where(c => c.RedirectUris.Any(IsWebUri))
            .Select(c => new AppEntry(c.DisplayName, GetHomeUri(c)))
            .ToList();

        // Single app — redirect straight there without showing the picker
        if (Apps.Count == 1)
            return Redirect(Apps[0].HomeUrl);

        return Page();
    }

    private static bool IsWebUri(string uri) =>
        uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    // Use the first redirect URI as a proxy for the app's home (strip the callback path)
    private static string GetHomeUri(IdentityClientConfig client)
    {
        var first = client.RedirectUris.First(IsWebUri);
        var uri = new Uri(first);
        return $"{uri.Scheme}://{uri.Authority}";
    }

    public record AppEntry(string Name, string HomeUrl);
}
