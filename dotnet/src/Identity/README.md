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

## Localization

`UI` ships localized login/register/MFA/account/apps pages (not `Pages/Manage/**`, which stays
English) via `services.AddAqIdentityLocalization()` / `app.UseAqIdentityLocalization()`
(`Identity/UI/Localization/`). Supported cultures: `en`, `zh-CN`, `zh-TW`, `zh-HK`, resolved in
order from the OIDC `ui_locales` param (direct, or embedded in `ReturnUrl`'s query string, via
`UiLocalesRequestCultureProvider`/`UiLocaleMapper`), then a `CookieRequestCultureProvider` cookie
(set for a year once a `ui_locales` match lands, so register/forgot-password/MFA keep whatever
language the user arrived in), then `Accept-Language`, then `en`. Page text and validation come
from `IdentityUIResource` (`.resx`, `.zh-Hans.resx`, `.zh-Hant.resx`, `.zh-HK.resx` — keys are the
English source text) and a registered `LocalizedIdentityErrorDescriber` (localizes ASP.NET Core
Identity's built-in `IdentityError` messages). Consuming apps (ELS's `backend/src/Identity`) just
call the two extension methods — no per-app localization wiring needed, though an app-owned layout
override (see `UI/README.md`) must re-declare `<html lang="@CultureInfo.CurrentUICulture.Name">`
and its own `:lang()` CJK font rules if it replaces the shared layout.
