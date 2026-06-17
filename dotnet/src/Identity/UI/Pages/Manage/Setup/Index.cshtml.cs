using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Extensions.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using System.Text.Json;

namespace AQ.Identity.UI.Pages.Manage.Setup;

public class SetupAdminModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IIdentityDbContext context,
    IOpenIddictScopeManager scopeManager,
    ISetupStateService setupState) : PageModel
{
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string FullName { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty] public string ConfirmPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await setupState.IsSetupRequiredAsync(HttpContext.RequestAborted))
            return RedirectToPage("/Manage/Clients/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await setupState.IsSetupRequiredAsync(HttpContext.RequestAborted))
            return RedirectToPage("/Manage/Clients/Index");

        if (Password != ConfirmPassword)
            ModelState.AddModelError(nameof(ConfirmPassword), "Passwords do not match.");

        if (!ModelState.IsValid)
            return Page();

        // Create the admin user
        var user = ApplicationUser.Create(Email, FullName);
        var createResult = await userManager.CreateAsync(user, Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        // Ensure the manage_api scope exists in OpenIddictScopes with its claim type mapping
        var existingScope = await scopeManager.FindByNameAsync("manage_api", HttpContext.RequestAborted);
        if (existingScope is null)
        {
            var descriptor = new OpenIddictScopeDescriptor
            {
                Name = "manage_api",
                DisplayName = "Manage API",
                Description = "Grants access to the identity management API"
            };
            descriptor.Properties["claim_types"] = JsonSerializer.SerializeToElement(new[] { "manage_api" });
            await scopeManager.CreateAsync(descriptor, HttpContext.RequestAborted);
        }

        // Grant the admin user the manage_api stored claim
        context.StoredClaims.Add(UserClaim.Create(user.Id, "manage_api", "true"));

        // Audit
        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.AdminUserCreated, user.Id, null, null));

        await context.SaveChangesAsync(HttpContext.RequestAborted);

        // Sign in and redirect to management dashboard
        await signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToPage("/Manage/Clients/Index");
    }
}
