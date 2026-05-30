using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AQ.Identity.UI.Pages.Manage.Scopes;

[Authorize(Policy = "ManageApi")]
public class ScopesIndexModel(
    IIdentityDbContext context,
    IOpenIddictScopeManager scopeManager) : PageModel
{
    public List<ScopeRow> Scopes { get; set; } = [];

    [BindProperty] public string NewName { get; set; } = string.Empty;
    [BindProperty] public string NewDisplayName { get; set; } = string.Empty;
    [BindProperty] public string NewDescription { get; set; } = string.Empty;
    [BindProperty] public string NewClaimTypesRaw { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadScopesAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            ModelState.AddModelError(nameof(NewName), "Scope name is required.");
            await LoadScopesAsync();
            return Page();
        }

        var exists = await context.IdentityScopes.AnyAsync(s => s.Name == NewName, HttpContext.RequestAborted);
        if (exists)
        {
            ModelState.AddModelError(nameof(NewName), $"Scope '{NewName}' already exists.");
            await LoadScopesAsync();
            return Page();
        }

        var scope = IdentityScope.Create(NewName, NewDisplayName, NewDescription);
        context.IdentityScopes.Add(scope);

        var claimTypes = ParseLines(NewClaimTypesRaw);
        foreach (var ct in claimTypes)
            context.ScopeClaimTypes.Add(ScopeClaimType.Create(scope.Id, ct));

        // Also register with OpenIddict so it appears in discovery
        var existingOidc = await scopeManager.FindByNameAsync(NewName, HttpContext.RequestAborted);
        if (existingOidc == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = NewName,
                DisplayName = string.IsNullOrEmpty(NewDisplayName) ? NewName : NewDisplayName
            }, HttpContext.RequestAborted);
        }

        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["Success"] = $"Scope '{NewName}' has been created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var scope = await context.IdentityScopes
            .Include(s => s.ClaimTypes)
            .FirstOrDefaultAsync(s => s.Id == id, HttpContext.RequestAborted);

        if (scope == null) return NotFound();

        context.ScopeClaimTypes.RemoveRange(scope.ClaimTypes);
        context.IdentityScopes.Remove(scope);
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["Success"] = $"Scope '{scope.Name}' has been deleted.";
        return RedirectToPage();
    }

    private async Task LoadScopesAsync()
    {
        Scopes = await context.IdentityScopes
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ScopeRow
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Description = s.Description,
                ClaimTypes = context.ScopeClaimTypes
                    .Where(sct => sct.ScopeId == s.Id)
                    .Select(sct => sct.ClaimType)
                    .ToList()
            })
            .ToListAsync(HttpContext.RequestAborted);
    }

    private static List<string> ParseLines(string raw) =>
        raw.Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Where(s => !string.IsNullOrEmpty(s))
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .ToList();
}

public class ScopeRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ClaimTypes { get; set; } = [];
}
