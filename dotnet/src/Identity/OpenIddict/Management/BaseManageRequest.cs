using FastEndpoints;

namespace AQ.Identity.OpenIddict.Management;

public abstract class BaseManageRequest
{
    [QueryParam] public int? Skip { get; set; } = 0;
    [QueryParam] public int? Take { get; set; } = 50;
    [QueryParam] public string? SearchTerm { get; set; }
}
