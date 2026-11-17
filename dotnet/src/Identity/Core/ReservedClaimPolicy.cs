namespace AQ.Identity.Core;

/// <summary>
/// Claim types a client's service-account claims may never set: they define who the caller is.
/// The library's own names (<see cref="PrincipalClaims.BaseReserved"/>) are always included; an app adds
/// its own through <c>AqIdentityOptions.ReservedServiceAccountClaims</c>.
/// </summary>
public sealed class ReservedClaimPolicy
{
    private readonly HashSet<string> _reserved;

    private ReservedClaimPolicy(HashSet<string> reserved) => _reserved = reserved;

    /// <summary>Base names plus <paramref name="extra"/> (case-insensitive); the base names cannot be removed.</summary>
    public static ReservedClaimPolicy Create(IEnumerable<string>? extra = null)
    {
        var reserved = new HashSet<string>(PrincipalClaims.BaseReserved, StringComparer.OrdinalIgnoreCase);
        foreach (var name in extra ?? [])
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Reserved claim names must not be blank.", nameof(extra));
            }

            reserved.Add(name.Trim());
        }

        return new ReservedClaimPolicy(reserved);
    }

    public IReadOnlySet<string> Reserved => _reserved;

    public bool IsReserved(string claimType) => _reserved.Contains(claimType);

    /// <summary>Returns an error message when <paramref name="claimTypes"/> contains a reserved type, otherwise null.</summary>
    // RULE IDC-1
    public string? Validate(IEnumerable<string> claimTypes)
    {
        var reserved = claimTypes.FirstOrDefault(IsReserved);
        return reserved is null
            ? null
            : $"Claim type '{reserved}' is reserved and cannot be set as a service account claim.";
    }
}
