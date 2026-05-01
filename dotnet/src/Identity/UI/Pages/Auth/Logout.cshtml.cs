using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using OpenIddict.Server.AspNetCore;

namespace AQ.Identity.UI.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly ILogger<LogoutModel> _logger;

    public string AppName { get; set; } = default!;

    public LogoutModel(
        SignInManager<ApplicationUser> signInManager,
        IOptions<AqIdentityOptions> options,
        ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _options = options;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        AppName = _options.Value.AppName;

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Auth/Login");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        AppName = _options.Value.AppName;

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Auth/Login");
        }

        await _signInManager.SignOutAsync();
        await HttpContext.SignOutAsync();

        return RedirectToPage("/Auth/Login");
    }
}
