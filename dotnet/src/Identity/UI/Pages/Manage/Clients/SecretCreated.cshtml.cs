using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AQ.Identity.UI.Pages.Manage.Clients;

[Authorize(Policy = "ManageApi")]
public class SecretCreatedModel : PageModel
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        var secret = TempData["NewSecret"]?.ToString();
        var clientId = TempData["NewClientId"]?.ToString();

        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(clientId))
            return RedirectToPage("./Index");

        ClientId = clientId;
        ClientSecret = secret;
        return Page();
    }
}
