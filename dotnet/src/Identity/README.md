# AQ.Identity

Config-driven, reusable OpenIddict-based IdP library. Split into:

- `Core` — options classes, entities, abstractions
- `OpenIddict` — server wiring, key management, admin management API
- `UI` — Razor Pages login/register/MFA/account/admin UI

A consuming app registers via `AddAqIdentity<TContext>(options, clients)` / `UseAqIdentity()`,
supplying its own `DbContext` and per-client `IdentityClientConfig` entries.

## Multi-tenancy

No tenant/organization concept exists in the schema, by design. The supported model is **one IdP
deployment per consuming app** — each app (ELS today, others later) runs its own instance/DB,
configured independently via `AddAqIdentity<TContext>`, and registers as many OAuth clients as it
needs (e.g. ELS's `els-web`, `els-mobile`, `question-generator`, `els-api`) within that one
deployment.

True shared multi-tenancy (one deployment serving multiple, isolated organizations/user pools)
would require a tenant concept in `AQ.Identity.Core.Entities`, tenant-scoped claims/scopes, and
tenant-resolution middleware — a materially larger change. Don't build this speculatively; revisit
only if a second, genuinely separate tenant needs to share one IdP deployment rather than running
its own.
