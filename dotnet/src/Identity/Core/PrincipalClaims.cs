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

    /// <summary>
    /// Claim types no app may let a service account set: they define who the caller is.
    /// Apps add their own through <see cref="ReservedClaimPolicy"/>.
    /// </summary>
    public static readonly IReadOnlyList<string> BaseReserved = ["sub", Type, ClientId];
}
