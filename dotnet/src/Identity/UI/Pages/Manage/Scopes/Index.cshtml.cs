using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using System.Text.Json;

namespace AQ.Identity.UI.Pages.Manage.Scopes;

[Authorize(Policy = "ManageApi")]
public class ScopesIndexModel(
    IOpenIddictScopeManager scopeManager,
    IOpenIddictApplicationManager applicationManager,
    IIdentityDbContext context) : PageModel
{
    /// <summary>
    /// Scopes the OIDC flow and Identity UI itself depend on to function. Deleting one of
    /// these would break login for every client, with no recovery short of re-seeding.
    /// </summary>
    private static readonly HashSet<string> ProtectedScopeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "openid", "profile", "email", "offline_access", "manage_api",
    };

    public List<ScopeRow> Scopes { get; set; } = [];

    [BindProperty] public string NewName { get; set; } = string.Empty;
    [BindProperty] public string NewDisplayName { get; set; } = string.Empty;
    [BindProperty] public string NewDescription { get; set; } = string.Empty;
    [BindProperty] public string NewClaimTypesRaw { get; set; } = string.Empty;

    [BindProperty] public string EditId { get; set; } = string.Empty;
    [BindProperty] public string EditDisplayName { get; set; } = string.Empty;
    [BindProperty] public string EditDescription { get; set; } = string.Empty;
    [BindProperty] public string EditClaimTypesRaw { get; set; } = string.Empty;

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

        var existing = await scopeManager.FindByNameAsync(NewName, HttpContext.RequestAborted);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(NewName), $"Scope '{NewName}' already exists.");
            await LoadScopesAsync();
            return Page();
        }

        var claimTypes = ParseLines(NewClaimTypesRaw);
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = NewName,
            DisplayName = string.IsNullOrWhiteSpace(NewDisplayName) ? NewName : NewDisplayName,
            Description = string.IsNullOrWhiteSpace(NewDescription) ? null : NewDescription
        };
        if (claimTypes.Count > 0)
            descriptor.Properties["claim_types"] = JsonSerializer.SerializeToElement(claimTypes);

        await scopeManager.CreateAsync(descriptor, HttpContext.RequestAborted);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.ScopeCreated, userId: null, ip: null, ua: null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["Success"] = $"Scope '{NewName}' has been created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var scope = await scopeManager.FindByIdAsync(id, HttpContext.RequestAborted);
        if (scope is null) return NotFound();

        var name = await scopeManager.GetNameAsync(scope, HttpContext.RequestAborted) ?? string.Empty;

        if (ProtectedScopeNames.Contains(name))
        {
            TempData["Error"] = $"'{name}' is a protected scope required for sign-in and cannot be deleted.";
            await LoadScopesAsync();
            return Page();
        }

        var scopePermission = OpenIddictConstants.Permissions.Prefixes.Scope + name;
        var clientsUsingScope = new List<string>();
        await foreach (var app in applicationManager.ListAsync(cancellationToken: HttpContext.RequestAborted))
        {
            var permissions = await applicationManager.GetPermissionsAsync(app, HttpContext.RequestAborted);
            if (permissions.Contains(scopePermission))
            {
                var clientId = await applicationManager.GetClientIdAsync(app, HttpContext.RequestAborted);
                if (!string.IsNullOrEmpty(clientId)) clientsUsingScope.Add(clientId);
            }
        }

        if (clientsUsingScope.Count > 0)
        {
            TempData["Error"] = $"Cannot delete '{name}' — it's still granted to: {string.Join(", ", clientsUsingScope)}. Remove it from those clients first.";
            await LoadScopesAsync();
            return Page();
        }

        await scopeManager.DeleteAsync(scope, HttpContext.RequestAborted);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.ScopeDeleted, userId: null, ip: null, ua: null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["Success"] = $"Scope '{name}' has been deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        var scope = await scopeManager.FindByIdAsync(EditId, HttpContext.RequestAborted);
        if (scope is null) return NotFound();

        var name = await scopeManager.GetNameAsync(scope, HttpContext.RequestAborted) ?? string.Empty;
        var claimTypes = ParseLines(EditClaimTypesRaw);

        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = name,
            DisplayName = string.IsNullOrWhiteSpace(EditDisplayName) ? name : EditDisplayName,
            Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription
        };
        if (claimTypes.Count > 0)
            descriptor.Properties["claim_types"] = JsonSerializer.SerializeToElement(claimTypes);

        await scopeManager.UpdateAsync(scope, descriptor, HttpContext.RequestAborted);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.ScopeUpdated, userId: null, ip: null, ua: null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["Success"] = $"Scope '{name}' has been updated.";
        return RedirectToPage();
    }

    private async Task LoadScopesAsync()
    {
        var rows = new List<ScopeRow>();

        await foreach (var scope in scopeManager.ListAsync(cancellationToken: HttpContext.RequestAborted))
        {
            var id = await scopeManager.GetIdAsync(scope, HttpContext.RequestAborted) ?? string.Empty;
            var name = await scopeManager.GetNameAsync(scope, HttpContext.RequestAborted) ?? string.Empty;
            var displayName = await scopeManager.GetDisplayNameAsync(scope, HttpContext.RequestAborted) ?? string.Empty;
            var description = await scopeManager.GetDescriptionAsync(scope, HttpContext.RequestAborted) ?? string.Empty;
            var props = await scopeManager.GetPropertiesAsync(scope, HttpContext.RequestAborted);

            var claimTypes = new List<string>();
            if (props.TryGetValue("claim_types", out var val) && val.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in val.EnumerateArray())
                {
                    var ct = item.GetString();
                    if (!string.IsNullOrWhiteSpace(ct))
                        claimTypes.Add(ct);
                }
            }

            rows.Add(new ScopeRow
            {
                Id = id,
                Name = name,
                DisplayName = displayName,
                Description = description,
                ClaimTypes = claimTypes
            });
        }

        Scopes = [.. rows.OrderBy(s => s.Name)];
    }

    private static List<string> ParseLines(string? raw) =>
        (raw ?? string.Empty).Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Where(s => !string.IsNullOrEmpty(s))
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .ToList();
}

public class ScopeRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ClaimTypes { get; set; } = [];
}
