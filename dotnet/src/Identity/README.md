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

## Sessions and sign-out

An app session is one permanent `OpenIddictAuthorization` (created per authorization-code login by
`ClaimsEnrichmentHandler`) plus its tokens. Each IdP sign-in gets a browser session id (`sid`,
`OpenIddict/Sessions/BrowserSession.cs`): kept in the Identity cookie, copied into access/id tokens
by `/connect/authorize`, and stored in the authorization's `Properties`. `/connect/logout` and
`/auth/logout` call `SessionRevocationService.RevokeForSignOutAsync`, which revokes every app session
with the cookie's `sid` (plus the one named by `id_token_hint`) — single sign-out for that browser,
other devices untouched. The Sessions page, account deletion, the admin revoke API and refresh-token
reuse detection use the same service. Clients can also revoke their own refresh token at
`/connect/revocation`.

**Back-channel logout** (OIDC Back-Channel Logout 1.0, `Sessions/BackchannelLogout.cs`): OpenIddict 7
has no native support (planned for 8.0, openiddict-core#2175), so it is implemented to the spec on
top of OpenIddict's own signing credentials. A client opts in with `BackchannelLogoutUri` (+
`BackchannelLogoutSessionRequired`) in `IdentityClientConfig` or the Manage Clients UI; whenever one
of its sessions is revoked, the IdP POSTs a `logout+jwt` logout token (`iss`, `aud`=client_id, `sub`,
`sid`, `events`, `jti`) with a 5s timeout. Discovery advertises `backchannel_logout_supported` and
`backchannel_logout_session_supported`.

**Audiences**: `/connect/authorize` and `/connect/token` set `SetResources(scopeManager.ListResourcesAsync(scopes))`,
so an access token's `aud` is the resources of its scopes (the app seeds them per scope). APIs must
validate `aud`; the IdP's own bearer endpoints accept `AqIdentityOptions.Audiences`.

**Management API** (`OpenIddict/Management/Endpoints`, under the host's FastEndpoints prefix, e.g.
`/api/manage/*`): machine-to-machine, so every endpoint sets `AuthSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)`
— bearer tokens with the `manage_api` claim; no token is a 401, never a login redirect. The
Razor admin UI under `/manage/*` stays cookie-based with the same `ManageApi` policy.

## Service principals and the audit log

**Service identity.** Every `client_credentials` client gets a stable GUID in its OpenIddict application property `principal_id`, generated when the client is created (config seeding, the admin UI or `POST /manage/clients`). It is kept across updates and secret resets; a client that has none gets one on the next seed (or the first token request). Tokens issued to the client carry `sub = principal_id`, `principal_type=service` and `client_id`. Person tokens carry `principal_type=human`. Resource servers use these to attribute writes to a principal.

**Reserved claims.** A client's `ServiceAccountClaims` may only add permission-style claims. `sub`, `principal_type`, `client_id` and `els_internal_user_id` are rejected (seeding throws, the admin pages and management endpoints return an error), because they define who the caller is.

**Audit log.** `AuditEntry` records the actor separately from the subject: `ActorKind` (`Anonymous | Human | Service | System`), `ActorId` (null only when anonymous), `SubjectUserId` (the user acted on), `ClientId`, `Action`, `IpAddress`, `UserAgent`, `OccurredAt`. Create entries with `AuditEntry.Log(action, actor, subjectUserId)` or `AuditEntry.LogAnonymous(action, subjectUserId)`; `AuditActors.Require(User)` resolves the signed-in admin or service for a request. `GET /manage/audit-log` filters by `SubjectUserId` and `ActorId`. The key rotation worker logs as the `KeyRotation` system actor. The consuming app maps the new columns and migrates (the old `UserId` becomes `SubjectUserId`).
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
