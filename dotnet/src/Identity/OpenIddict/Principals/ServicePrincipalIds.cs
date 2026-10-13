using System.Collections.Immutable;
using System.Text.Json;
using AQ.Identity.Core;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Principals;

/// <summary>
/// A client_credentials client's stable principal id, kept in the OpenIddict application's
/// <c>principal_id</c> property. It becomes the token <c>sub</c>, so resource servers can attribute writes to
/// the service as a real principal that survives secret resets and client_id changes.
/// </summary>
public static class ServicePrincipalIds
{
    public static Guid? Read(ImmutableDictionary<string, JsonElement> properties) =>
        properties.TryGetValue(PrincipalClaims.PrincipalIdProperty, out var value)
        && value.ValueKind == JsonValueKind.String
        && Guid.TryParse(value.GetString(), out var id)
            ? id
            : null;

    public static async Task<Guid?> ReadAsync(IOpenIddictApplicationManager manager, object application, CancellationToken ct) =>
        Read(await manager.GetPropertiesAsync(application, ct));

    public static void Set(OpenIddictApplicationDescriptor descriptor, Guid id) =>
        descriptor.Properties[PrincipalClaims.PrincipalIdProperty] = JsonSerializer.SerializeToElement(id.ToString());

    /// <summary>
    /// Returns the client's principal id, assigning and persisting one if it has none (clients created before
    /// principal ids existed). Existing settings, including the hashed secret, are carried through.
    /// </summary>
    public static async Task<Guid> EnsureAsync(IOpenIddictApplicationManager manager, object application, CancellationToken ct)
    {
        if (await ReadAsync(manager, application, ct) is { } existing)
        {
            return existing;
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await manager.PopulateAsync(descriptor, application, ct);
        var id = Guid.NewGuid();
        Set(descriptor, id);
        await manager.UpdateAsync(application, descriptor, ct);
        return id;
    }
}
