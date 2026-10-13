namespace AQ.Identity.Core;

/// <summary>Claim names and values that identify the principal behind a token.</summary>
public static class PrincipalClaims
{
    /// <summary>Claim naming the kind of principal: <see cref="Human"/> or <see cref="Service"/>.</summary>
    public const string Type = "principal_type";

    public const string Human = "human";
    public const string Service = "service";

    public const string ClientId = "client_id";

    /// <summary>OpenIddict application property holding a client_credentials client's stable principal id.</summary>
    public const string PrincipalIdProperty = "principal_id";

    /// <summary>Claim types a client's service-account claims may never set: they define who the caller is.</summary>
    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "sub",
        Type,
        ClientId,
        "els_internal_user_id",
    };

    /// <summary>Returns an error message when <paramref name="claimTypes"/> contains a reserved type, otherwise null.</summary>
    public static string? ValidateServiceAccountClaimTypes(IEnumerable<string> claimTypes)
    {
        var reserved = claimTypes.FirstOrDefault(Reserved.Contains);
        return reserved is null
            ? null
            : $"Claim type '{reserved}' is reserved and cannot be set as a service account claim.";
    }
}
