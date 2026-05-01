# AQ.Identity — Implementation Plan

A reusable .NET library suite that provides all the building blocks for an OpenID Connect / OAuth2 identity server. Any application platform imports `AQ.Identity.*` NuGet packages and assembles its own identity server — with its own database, its own branding, and direct access to its own domain data for custom claims.

This plan is structured for LLM-assisted development. Each phase is an isolated, well-scoped task a single agent can execute end-to-end. Phases build sequentially; steps within a phase can often run in parallel across agents.

---

## Design Philosophy

### Why a library, not a deployed server

A shared central IdP couples all your applications to one deployment: one database, one set of app registrations, one team owning it. That is fine for a commercial SaaS platform, but it is the wrong model when your applications are independent platforms (ELS, and future XYZ solutions).

Instead, `AQ.Identity` is a **library**. Each platform builds its own identity server by importing it:

```
AQ.Identity.*  (NuGet packages — this repo)
      │
      ▼
ELS.Identity   (ASP.NET Core app — in ELS repo)
      │  direct in-process call
      ▼
ELS CoreDbContext  (reads user permissions, workspaces, etc.)
```

```
AQ.Identity.*  (same packages)
      │
      ▼
XYZ.Identity   (separate app — in XYZ repo, no relation to ELS)
      │  direct in-process call
      ▼
XYZ DbContext
```

Each identity server is isolated: separate codebase, separate database, separate deployment. No cross-platform coupling.

### Why this beats Entra External ID for this use case

| Pain point with Entra | How the library model fixes it |
|-----------------------|-------------------------------|
| Custom claims require an HTTP webhook to your API, reachable by Microsoft | `ELS.Identity` calls `CoreDbContext` directly — same process, no HTTP roundtrip |
| Dev tunnel required for local development | No external calls; everything runs locally |
| Microsoft-proprietary webhook JSON contract | No contract — it's a method call |
| Three Azure app registrations per environment | Zero Azure config; clients defined in `config/clients.json` |
| Claims embedded at issuance, can be stale | Same `claims_version` + revocation mechanism works identically |

### Shared database instance, separate logical databases

ELS uses one Postgres server. `ELS.Identity` adds a **second database** on the same server — not a second schema, a second `CREATE DATABASE`. This keeps identity data (credentials, sessions, TOTP keys) completely separate from domain data (courses, workspaces, etc.) while still being trivial to run locally.

```
Postgres server
  ├── els_core      ← CoreDbContext (existing)
  └── els_identity  ← IdentityDbContext (new, owned by ELS.Identity)
```

The library is DB-provider agnostic: it ships EF Core abstractions. Each host app wires in Npgsql, SQL Server, or SQLite.

---

## Architecture: Two Layers

### Layer 1 — AQ.Identity library packages (this repo, `AQ-Libraries`)

These packages contain all the reusable logic. They have no opinion about which database provider you use, what your connection string is, or what your application's domain model looks like.

```
AQ.Identity.Core           ← domain entities, interfaces, abstractions
AQ.Identity.OpenIddict     ← OpenIddict server configuration extensions
AQ.Identity.UI             ← Razor Class Library (login/register/MFA pages + Tailwind CSS)
AQ.Identity.Email          ← IEmailService abstraction + SMTP implementation
```

### Layer 2 — ELS.Identity host app (ELS repo)

A thin ASP.NET Core project that composes the library packages, wires in ELS's database provider and connection string, injects custom claims from `CoreDbContext`, and configures branding.

```
ELS.Identity               ← host app (ELS repo: backend/src/Identity/)
  ├── references AQ.Identity.Core
  ├── references AQ.Identity.OpenIddict
  ├── references AQ.Identity.UI
  ├── references AQ.Identity.Email
  ├── references ELS.Core.Infrastructure  (for CoreDbContext — custom claims)
  ├── IdentityDbContext     ← ELS-specific, extends AQ base context
  ├── ClaimsEnricher.cs     ← implements IClaimsEnricher, queries CoreDbContext
  ├── Program.cs            ← wires everything together
  └── appsettings.json
```

---

## Package Structure (`AQ-Libraries/dotnet/src/Identity/`)

```
dotnet/src/Identity/
├── AQ.Identity.Core/
│   ├── AQ.Identity.Core.csproj
│   ├── Entities/
│   │   ├── ApplicationUser.cs       ← IdentityUser<Guid> + FullName, CreatedAt, IsActive
│   │   ├── AuditEntry.cs            ← append-only sign-in audit
│   │   └── SigningKey.cs            ← RSA key storage entity
│   ├── Abstractions/
│   │   ├── IIdentityDbContext.cs    ← interface the host app's DbContext must implement
│   │   ├── IClaimsEnricher.cs       ← hook for injecting custom claims at token issuance
│   │   ├── IEmailService.cs
│   │   └── IEmailTemplateService.cs
│   └── Configuration/
│       ├── IdentityOptions.cs       ← password policy, lockout, token lifetimes
│       └── IdentityClientConfig.cs  ← OIDC client registration model
│
├── AQ.Identity.OpenIddict/
│   ├── AQ.Identity.OpenIddict.csproj
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs  ← AddAqIdentity(options => ...)
│   ├── Handlers/
│   │   └── ClaimsEnrichmentHandler.cs      ← OpenIddict handler, calls IClaimsEnricher
│   ├── Seeding/
│   │   └── ClientSeeder.cs                 ← upserts clients from config on startup
│   └── KeyManagement/
│       └── SigningKeyManager.cs            ← RSA key lifecycle, rotation
│
├── AQ.Identity.UI/
│   ├── AQ.Identity.UI.csproj               ← Razor Class Library
│   ├── Pages/
│   │   ├── Auth/                           ← Login, Register, VerifyEmail, etc.
│   │   ├── Mfa/                            ← Setup, Challenge, Disable
│   │   └── Account/                        ← Profile, Security, Sessions
│   ├── Shared/
│   │   └── _AuthLayout.cshtml
│   └── wwwroot/
│       └── css/identity.css                ← compiled Tailwind output (bundled with package)
│
└── AQ.Identity.Email/
    ├── AQ.Identity.Email.csproj
    ├── SmtpEmailService.cs
    ├── ConsoleEmailService.cs
    └── EmailTemplates.cs
```

---

## Technology Stack

| Concern | Library | Notes |
|---------|---------|-------|
| OIDC/OAuth2 server | `OpenIddict.AspNetCore` | Full protocol stack |
| Identity / passwords | `Microsoft.AspNetCore.Identity` | Password hashing, lockout, TOTP |
| EF Core | `Microsoft.EntityFrameworkCore` | Abstractions only in library; provider added by host |
| Razor Class Library UI | ASP.NET Core RCL | Pages overridable by host app |
| Tailwind CSS | CLI, bundled output | Compiled CSS shipped with the RCL |
| Email | `MailKit` | In `AQ.Identity.Email`; swappable |
| TOTP QR | `QRCoder` | In UI package |
| Signing keys | OpenIddict + Data Protection | Stored encrypted in identity DB |
| Social login | `AspNet.Security.OAuth.Google` | Optional; added by host app |

---

## Phase 0 — Library Project Scaffold

**Goal:** All four library projects exist in `AQ-Libraries`, compile cleanly, and are added to `AQ.sln`. No logic yet.

**Prompt for agent:**
> In `d:\Repositories\AQ-Libraries\dotnet\`, add four new class library projects under `src/Identity/`:
> - `AQ.Identity.Core` — class library, .NET 10, no external dependencies yet
> - `AQ.Identity.OpenIddict` — class library, references `AQ.Identity.Core`
> - `AQ.Identity.UI` — **Razor Class Library** (`<Project Sdk="Microsoft.NET.Sdk.Razor">`), references `AQ.Identity.Core`
> - `AQ.Identity.Email` — class library, references `AQ.Identity.Core`
>
> Add all four to `AQ.sln`. Add their NuGet dependencies to `Directory.Packages.props` (do not specify versions in `.csproj` files — use central package management). Add packages:
> - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (latest .NET 10 compatible)
> - `OpenIddict.AspNetCore`, `OpenIddict.EntityFrameworkCore`
> - `MailKit`
> - `QRCoder`
> - `AspNet.Security.OAuth.Google` (optional reference, added to OpenIddict project)
>
> Configure all projects with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`. Run `dotnet build AQ.sln` — must exit 0.

**Deliverables:**
- Four new projects in `dotnet/src/Identity/`
- All added to `AQ.sln`
- `dotnet build AQ.sln` exits 0

---

## Phase 1 — Core Entities and Abstractions

**Goal:** All shared entities, interfaces, and configuration models that both the library and host apps depend on.

### 1.1 — Entities

**Prompt for agent:**
> In `AQ.Identity.Core/Entities/`, implement:
>
> **`ApplicationUser : IdentityUser<Guid>`**
> - `FullName` (string, required)
> - `CreatedAt` (DateTimeOffset, set in constructor)
> - `LastLoginAt` (DateTimeOffset?, nullable)
> - `IsActive` (bool, default `true`)
> - Static factory: `ApplicationUser Create(string email, string fullName)`
>
> **`AuditEntry`** (not an IdentityUser — plain entity)
> - `Id` (Guid, `init`)
> - `UserId` (Guid?, nullable — some events like failed logins may not resolve a user)
> - `Action` (string — constants defined in a nested `Actions` static class: `LoginSuccess`, `LoginFailed`, `LoginLocked`, `PasswordChanged`, `PasswordReset`, `EmailVerified`, `MfaEnabled`, `MfaDisabled`, `KeyRotated`)
> - `IpAddress` (string?)
> - `UserAgent` (string?)
> - `OccurredAt` (DateTimeOffset, set in constructor)
> - No navigation properties. Static factory: `AuditEntry Log(string action, Guid? userId, string? ip, string? ua)`
>
> **`SigningKey`**
> - `Id` (Guid)
> - `KeyId` (string — the `kid` JWT header value)
> - `EncryptedKeyXml` (string — RSA private key XML, encrypted at rest)
> - `CreatedAt` (DateTimeOffset)
> - `ExpiresAt` (DateTimeOffset)
> - `RetiredAt` (DateTimeOffset?)
> - `IsExpired` (computed: `DateTimeOffset.UtcNow > ExpiresAt`)
> - `IsRetired` (computed: `RetiredAt.HasValue && DateTimeOffset.UtcNow > RetiredAt`)

### 1.2 — Abstractions

**Prompt for agent:**
> In `AQ.Identity.Core/Abstractions/`, implement:
>
> **`IIdentityDbContext`** — interface the host app's DbContext must satisfy:
> ```csharp
> public interface IIdentityDbContext
> {
>     DbSet<ApplicationUser> Users { get; }
>     DbSet<AuditEntry> AuditLog { get; }
>     DbSet<SigningKey> SigningKeys { get; }
>     Task<int> SaveChangesAsync(CancellationToken ct = default);
> }
> ```
>
> **`IClaimsEnricher`** — the hook each host app implements to inject custom claims:
> ```csharp
> public interface IClaimsEnricher
> {
>     /// Called during token issuance. Return additional claims to merge into the JWT.
>     /// subject: the IdP's user ID (Guid, maps to ApplicationUser.Id)
>     /// clientId: the OIDC client application ID
>     /// Returning an empty dictionary is valid (no extra claims).
>     /// Never throw — return empty dict on error and log internally.
>     Task<IReadOnlyDictionary<string, string>> EnrichAsync(
>         Guid subject, string clientId, IEnumerable<string> scopes, CancellationToken ct);
> }
> ```
>
> **`IEmailService`**:
> ```csharp
> public interface IEmailService
> {
>     Task SendAsync(EmailMessage message, CancellationToken ct = default);
> }
>
> public sealed record EmailMessage(
>     string To, string Subject, string HtmlBody, string TextBody);
> ```
>
> **`IEmailTemplateService`**:
> ```csharp
> public interface IEmailTemplateService
> {
>     EmailMessage BuildVerificationEmail(string toEmail, string verificationUrl, string appName);
>     EmailMessage BuildPasswordResetEmail(string toEmail, string resetUrl, string appName);
> }
> ```

### 1.3 — Configuration models

**Prompt for agent:**
> In `AQ.Identity.Core/Configuration/`, implement:
>
> **`AqIdentityOptions`** — the root options class passed to `AddAqIdentity(options => ...)`:
> ```csharp
> public class AqIdentityOptions
> {
>     public string Issuer { get; set; } = default!;
>     public string AppName { get; set; } = "AQ Identity"; // used in UI + emails
>     public TokenLifetimeOptions Tokens { get; set; } = new();
>     public PasswordPolicyOptions Password { get; set; } = new();
>     public LockoutPolicyOptions Lockout { get; set; } = new();
>     public KeyManagementOptions Keys { get; set; } = new();
>     public EmailOptions Email { get; set; } = new();
>     public GoogleOptions? Google { get; set; } // null = Google login disabled
> }
>
> public class TokenLifetimeOptions
> {
>     public TimeSpan AccessToken { get; set; } = TimeSpan.FromHours(1);
>     public TimeSpan RefreshToken { get; set; } = TimeSpan.FromDays(14);
> }
>
> public class PasswordPolicyOptions
> {
>     public int MinLength { get; set; } = 8;
>     public bool RequireDigit { get; set; } = true;
>     public bool RequireUppercase { get; set; } = false;
>     public bool RequireNonAlphanumeric { get; set; } = false;
> }
>
> public class LockoutPolicyOptions
> {
>     public int MaxFailedAttempts { get; set; } = 5;
>     public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
> }
>
> public class KeyManagementOptions
> {
>     public TimeSpan RotationPeriod { get; set; } = TimeSpan.FromDays(90);
>     public TimeSpan RetirementOverlap { get; set; } = TimeSpan.FromDays(30);
> }
>
> public class EmailOptions
> {
>     public string Host { get; set; } = "localhost";
>     public int Port { get; set; } = 1025;
>     public bool UseSsl { get; set; } = false;
>     public string? Username { get; set; }
>     public string? Password { get; set; }
>     public string FromAddress { get; set; } = "noreply@localhost";
>     public string FromName { get; set; } = "Identity";
> }
>
> public class GoogleOptions
> {
>     public string ClientId { get; set; } = default!;
>     public string ClientSecret { get; set; } = default!;
> }
> ```
>
> **`IdentityClientConfig`** — represents one OIDC client registration:
> ```csharp
> public class IdentityClientConfig
> {
>     public string ClientId { get; set; } = default!;
>     public string DisplayName { get; set; } = default!;
>     public string Type { get; set; } = "public"; // "public" | "confidential"
>     public string? ClientSecret { get; set; }    // required when Type = "confidential"
>     public List<string> RedirectUris { get; set; } = [];
>     public List<string> PostLogoutRedirectUris { get; set; } = [];
>     public List<string> Scopes { get; set; } = [];
> }
> ```

**Deliverables:**
- All entities and abstractions compile
- `IClaimsEnricher` is the only interface the host app must implement to get custom claims

---

## Phase 2 — OpenIddict Package

**Goal:** `AQ.Identity.OpenIddict` provides the single extension method `AddAqIdentity<TContext>()` that wires up everything. Host apps call this once.

### 2.1 — Main registration extension

**Prompt for agent:**
> In `AQ.Identity.OpenIddict/Extensions/ServiceCollectionExtensions.cs`, implement:
>
> ```csharp
> public static IServiceCollection AddAqIdentity<TContext>(
>     this IServiceCollection services,
>     AqIdentityOptions options,
>     IReadOnlyList<IdentityClientConfig> clients)
>     where TContext : DbContext, IIdentityDbContext
> ```
>
> This method must:
> 1. Call `services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()` configured with `options.Password` and `options.Lockout` values.
> 2. Call `services.AddOpenIddict()` with:
>    - `.AddCore(o => o.UseEntityFrameworkCore().UseDbContext<TContext>())`
>    - `.AddServer(o => { ... })` configured with:
>      - Issuer = `options.Issuer`
>      - Authorization Code + PKCE required, Refresh Token flows
>      - All standard OIDC endpoints enabled (authorize, token, userinfo, end-session, introspection, revocation, discovery)
>      - Token lifetimes from `options.Tokens`
>      - Development signing cert in Development environment; persistent RSA key (via `SigningKeyManager`) otherwise
>    - `.AddValidation(o => o.UseLocalServer().UseAspNetCore())`
> 3. Register `ClientSeeder` as a hosted service (runs once on startup, upserts clients from the provided list).
> 4. Register `ClaimsEnrichmentHandler` as a scoped `IOpenIddictServerHandler`.
> 5. Register `SigningKeyManager` as a singleton.
> 6. Conditionally register Google OAuth2 if `options.Google != null`.
>
> **Important:** Do NOT register `IEmailService` or `IEmailTemplateService` — email is optional and registered separately via `AddAqIdentityEmail` (Phase 4.1).
>
> Also add:
> ```csharp
> public static IApplicationBuilder UseAqIdentity(this IApplicationBuilder app)
> ```
> which calls `app.UseAuthentication()`, `app.UseAuthorization()`, and maps the Razor Class Library routes.

### 2.2 — Claims enrichment handler

**Prompt for agent:**
> In `AQ.Identity.OpenIddict/Handlers/ClaimsEnrichmentHandler.cs`, implement an OpenIddict `IOpenIddictServerHandler<ProcessSignInContext>`:
>
> - Resolve `IClaimsEnricher` from DI.
> - Extract `subject` from the token principal (`ClaimTypes.NameIdentifier` or `"sub"`), parse as `Guid`.
> - Extract `clientId` from the context's application.
> - Extract granted scopes from the principal.
> - Call `IClaimsEnricher.EnrichAsync(subject, clientId, scopes, ct)`.
> - For each returned key-value pair, call `principal.SetClaim(key, value)` to merge into the token.
> - If `EnrichAsync` throws, catch the exception, log a warning (`ILogger`), and continue without crashing — the token is still issued without enrichment claims.
> - The handler must be registered with `OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>().UseScopedHandler<ClaimsEnrichmentHandler>()`.

### 2.3 — Client seeder

**Prompt for agent:**
> In `AQ.Identity.OpenIddict/Seeding/ClientSeeder.cs`, implement `IHostedService`:
>
> On `StartAsync`, using `IOpenIddictApplicationManager`:
> - For each `IdentityClientConfig` in the injected list:
>   - Look up the client by `ClientId`.
>   - If not found: create it with all configured properties.
>   - If found: update it (so config changes take effect on restart without manual DB edits).
> - Register required scopes (`openid`, `profile`, `email`, `offline_access`, plus any custom scopes from client configs) using `IOpenIddictScopeManager`.
> - Log each create/update at `Information` level.

### 2.4 — Key manager

**Prompt for agent:**
> In `AQ.Identity.OpenIddict/KeyManagement/SigningKeyManager.cs`, implement:
>
> - On startup, query `IIdentityDbContext.SigningKeys` for all non-retired keys.
> - If none exist (first run): generate a new 2048-bit RSA key, encrypt the private key XML using `IDataProtector` (purpose: `"AQ.Identity.SigningKey"`), persist as `SigningKey` entity, log `AuditEntry.Actions.KeyRotated`.
> - If the newest non-expired key is within 30 days of expiry: generate and persist a new key alongside (overlap period).
> - Return the newest non-expired key as the active signing key.
> - Return all non-retired keys as validation keys (so tokens signed by the previous key remain valid during the overlap period).
> - Expose `IReadOnlyList<SigningKey> GetValidationKeys()` and `SigningKey GetActiveSigningKey()`.
> - Keys older than `RetiredAt` are excluded from both lists.

**Deliverables:**
- `services.AddAqIdentity<TContext>(options, clients)` is the complete setup call
- Host app only needs to implement `IClaimsEnricher`
- No HTTP webhook — enrichment is a direct in-process method call

---

## Phase 3 — Login UI (Razor Class Library)

**Goal:** All authentication pages are bundled in `AQ.Identity.UI`. Host apps get them for free by referencing the package. Each page can be overridden by placing a file at the same path in the host app — standard Razor Class Library override mechanism.

### Design system

All pages follow these rules — include them verbatim at the top of every agent prompt in this phase:

> **UI rules (apply to every page in this phase):**
> - Tailwind CSS utility classes only. No `<style>` tags, no custom CSS files.
> - Single-column centered layout: `min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950`
> - Auth card: `bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-100 dark:border-slate-800 p-8 w-full max-w-sm mx-4`
> - App name wordmark at top of card: `text-xl font-bold text-indigo-600 dark:text-indigo-400 mb-6 text-center`
> - Input fields: `w-full rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent`
> - Primary button: `w-full bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed`
> - Secondary/outline button: `w-full border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 font-medium py-2 px-4 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors`
> - Error message: `<p role="alert" class="text-sm text-red-600 dark:text-red-400 mt-1">`
> - Success/info message: `<p class="text-sm text-green-700 dark:text-green-400 bg-green-50 dark:bg-green-900/20 rounded-lg p-3">`
> - Links: `text-indigo-600 dark:text-indigo-400 hover:underline text-sm`
> - Field labels: `block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1`
> - Section divider: `<div class="relative my-4"><div class="absolute inset-0 flex items-center"><div class="w-full border-t border-slate-200 dark:border-slate-700"></div></div><div class="relative flex justify-center text-sm text-slate-500">or</div></div>`
> - Dark mode: respects `prefers-color-scheme` via Tailwind's `darkMode: 'media'` config
> - Submit button shows a spinner and is disabled on form submit (vanilla JS — 3 lines max)
> - All forms have `asp-antiforgery="true"`
> - Every input has an associated `<label>` (accessibility)
> - `AppName` is injected via `IOptions<AqIdentityOptions>` — never hardcoded

### 3.1 — Shared layout and partials

**Prompt for agent:**
> Create `Pages/Shared/_AuthLayout.cshtml` in `AQ.Identity.UI`. Apply all UI rules above. The layout:
> - Renders the full page shell (Tailwind base classes, dark mode support)
> - Injects `AppName` from `IOptions<AqIdentityOptions>` into `ViewData["AppName"]`
> - Renders the wordmark, card wrapper, `@RenderBody()`, and a footer: `<p class="text-xs text-center text-slate-400 mt-6">Powered by AQ Identity</p>`
>
> Create these partials:
> - `_FieldError.cshtml` — renders `<p role="alert" ...>` for a named ModelState key; renders nothing if no error
> - `_SubmitButton.cshtml` — accepts `text` parameter (default "Continue"); renders the primary button with inline JS: on `form.submit`, disable the button and show a 16px inline SVG spinner
> - `_Divider.cshtml` — the "or" section divider

### 3.2 — Login page

**Prompt for agent:**
> [Apply UI rules] Create `Pages/Auth/Login.cshtml` and `Login.cshtml.cs`.
>
> Layout: use `_AuthLayout`. Heading: "Sign in" (`text-lg font-semibold text-slate-900 dark:text-slate-100 mb-6 text-center`).
>
> Fields: Email (type=email, autocomplete=email), Password (type=password, autocomplete=current-password) with a show/hide toggle (eye icon SVG, vanilla JS toggles `type` attribute), "Remember me" checkbox aligned left, "Forgot password?" link aligned right on the same row.
>
> Below the form: `_Divider` ("or"), "Continue with Google" button (outlined, Google logo SVG inline, only rendered if `GoogleOptions != null` in `IOptions<AqIdentityOptions>`).
>
> Below that: "Don't have an account? Sign up" link.
>
> `OnPostAsync`: call `SignInManager.PasswordSignInAsync`. Lockout → `/auth/lockout`. RequiresTwoFactor → `/auth/mfa/challenge`. Success → redirect to validated `returnUrl`. Failure → `ModelState.AddModelError("", "Incorrect email or password")` (never field-specific).
>
> The page must correctly receive and forward the OpenIddict authorization request: on GET, if an OIDC request is present (`HttpContext.GetOpenIddictServerRequest()`), store the `returnUrl` in a hidden field or TempData and restore it after sign-in.

### 3.3 — Register page

**Prompt for agent:**
> [Apply UI rules] Create `Pages/Auth/Register.cshtml/.cs`.
>
> Fields: Full Name (autocomplete=name), Email (type=email), Password with show/hide toggle, Confirm Password.
>
> Below form: "Already have an account? Sign in" link.
>
> `OnPostAsync`: validate all fields, confirm passwords match (add ModelState error if not — do not rely on `[Compare]` attribute alone). Create `ApplicationUser` via `UserManager.CreateAsync`. On Identity errors (e.g. email taken): show each error inline. On success: generate email verification token, call `IEmailService` via `IEmailTemplateService.BuildVerificationEmail`, redirect to `/auth/verify-email-sent?email={UrlEncoded(email)}`.

### 3.4 — Email verification pages

**Prompt for agent:**
> [Apply UI rules] Create:
>
> 1. `Pages/Auth/VerifyEmailSent.cshtml/.cs` — shows `email` from query string. Message: "We sent a verification link to **{email}**. Check your inbox and spam folder." Resend button (POST handler): rate-limited — store last-sent timestamp in a cookie (`verify_sent_at`); if < 60 seconds ago, show "Please wait before requesting another link." On resend: regenerate token and resend email.
>
> 2. `Pages/Auth/VerifyEmail.cshtml/.cs` — accepts `?userId=&token=` from query string. `OnGetAsync`: call `UserManager.ConfirmEmailAsync`. On success: green confirmation card with "Your email is confirmed." and a "Sign in" button. On failure (expired/invalid): error card with "This link has expired or is invalid." and a "Request a new link" button linking to `/auth/verify-email-sent`.

### 3.5 — Password reset pages

**Prompt for agent:**
> [Apply UI rules] Create four pages:
>
> 1. `Pages/Auth/ForgotPassword.cshtml/.cs` — one email field. `OnPostAsync`: always redirect to confirmation page (anti-enumeration). Internally: find user by email; if exists and email confirmed, generate reset token, call `IEmailTemplateService.BuildPasswordResetEmail`, send via `IEmailService`.
>
> 2. `Pages/Auth/ForgotPasswordConfirmation.cshtml` — static: "If that email address is registered, you'll receive a reset link shortly. Check your inbox." Back to sign-in link.
>
> 3. `Pages/Auth/ResetPassword.cshtml/.cs` — `userId` and `token` as hidden fields (populated from query string on GET). Fields: new password, confirm password. `OnPostAsync`: `UserManager.ResetPasswordAsync`. On token expired/invalid: show error card. On success: redirect to confirmation page.
>
> 4. `Pages/Auth/ResetPasswordConfirmation.cshtml` — static: "Password reset successfully." Sign in button.

### 3.6 — Lockout and logout pages

**Prompt for agent:**
> [Apply UI rules] Create:
>
> 1. `Pages/Auth/Lockout.cshtml` — static. Icon: a padlock SVG (inline, 32px, slate colour, centered). Message: "Your account is temporarily locked after too many failed sign-in attempts. Try again in a few minutes." Sign in link.
>
> 2. `Pages/Auth/Logout.cshtml/.cs` — GET: if authenticated, show confirmation card: "Sign out of {AppName}?" with Sign out (primary) and Cancel (secondary, link back to previous page) buttons. Never sign out on GET. POST: call `SignInManager.SignOutAsync()`, process the OpenIddict end-session request (`HttpContext.GetOpenIddictServerRequest()`), redirect to `post_logout_redirect_uri` if registered, otherwise to `/auth/login`.

**Deliverables after Phase 3:**
- Full registration → verify email → login flow works in a browser
- Forgot password → reset → login flow works
- Dark mode and light mode render correctly
- All pages are keyboard-accessible

---

## Phase 4 — Email Package

**Goal:** `AQ.Identity.Email` provides working SMTP + console implementations and inline HTML email templates.

### 4.1 — Implementations

**Prompt for agent:**
> In `AQ.Identity.Email/`:
>
> `SmtpEmailService : IEmailService` — uses MailKit `SmtpClient`. Reads config from the injected `EmailOptions`. Sends both HTML and plain-text body (MimeKit `BodyBuilder`). On failure: log error with subject and recipient (never log the body). Dispose `SmtpClient` after each send.
>
> `ConsoleEmailService : IEmailService` — logs `Subject`, `To`, and `HtmlBody` at `Debug` level. Used in Development.
>
> `DefaultEmailTemplateService : IEmailTemplateService` — (moved here from Phase 4.2, implement it here) builds verification and password reset email messages. Implement methods: `BuildVerificationEmail(string toEmail, string verificationUrl, string appName)` and `BuildPasswordResetEmail(string toEmail, string resetUrl, string appName)`. Both return `EmailMessage` with inline CSS (no `<style>` blocks).
>
> **Registration extension:**
> Create `AQ.Identity.Email/Extensions/ServiceCollectionExtensions.cs` with:
> ```csharp
> public static IServiceCollection AddAqIdentityEmail(
>     this IServiceCollection services,
>     EmailOptions options,
>     IHostEnvironment env)
> ```
> This extension registers:
> - `ConsoleEmailService` in Development, `SmtpEmailService` in other environments, as `IEmailService`
> - `DefaultEmailTemplateService` as `IEmailTemplateService`
> - The `options` are passed to the email services via `IOptions<EmailOptions>`
>
> Host apps call `services.AddAqIdentityEmail(options.Email, env)` to register both services at once. This keeps email setup completely separate from `AddAqIdentity`.

---

## Phase 5 — MFA (TOTP)

### 5.1 — Setup and backup codes

**Prompt for agent:**
> [Apply UI rules] Create `Pages/Mfa/Setup.cshtml/.cs` in `AQ.Identity.UI` (requires `[Authorize]`).
>
> GET: get or reset authenticator key via `UserManager.GetAuthenticatorKeyAsync` (call `ResetAuthenticatorKeyAsync` if null). Build `otpauth://totp/{AppName}:{email}?secret={key}&issuer={AppName}`. Generate QR code PNG from the URI using `QRCoder.QRCodeGenerator` → `QRCoder.PngByteQRCode` → base64 data URI. Render: QR code image (200×200, centered), the raw key in a monospace box with a "Copy" button (vanilla JS `navigator.clipboard.writeText`), a 6-digit input for the user to confirm enrolment.
>
> POST: `UserManager.VerifyTwoFactorTokenAsync` with `AuthenticatorTokenProvider`. On valid: `UserManager.SetTwoFactorEnabledAsync(true)`, generate 8 backup codes via `UserManager.GenerateNewTwoFactorRecoveryCodesAsync`, store codes in TempData, redirect to `/auth/mfa/backup-codes`. On invalid: show inline error "Invalid code. Try again."
>
> `Pages/Mfa/BackupCodes.cshtml/.cs` — reads codes from TempData. Displays codes in a 2-column monospace grid (`font-mono text-sm`). Yellow warning banner: "Save these codes somewhere safe — you will not see them again." A "Download as .txt" link (builds a Blob client-side via vanilla JS). "I've saved my codes" button (POST, just redirects to `/account/security`). If TempData is empty (direct navigation): show "Backup codes are only shown once. Go to Security settings to regenerate them."

### 5.2 — Login challenge

**Prompt for agent:**
> [Apply UI rules] Create `Pages/Mfa/Challenge.cshtml/.cs`.
>
> Two sections toggled by a pair of tab-style buttons (no JS library — `<button type="button">` with `aria-selected`, vanilla JS toggles `hidden` class on sections):
> - **Authenticator app**: 6-digit numeric input, `inputmode="numeric"`, `autocomplete="one-time-code"`
> - **Backup code**: alphanumeric input
>
> `OnPostAuthenticatorAsync`: `SignInManager.TwoFactorAuthenticatorSignInAsync(code, rememberMe: false, rememberBrowser: false)`. On success: redirect to `returnUrl`. On failure: ModelState error. After 5 failures: redirect to `/auth/lockout`.
>
> `OnPostBackupCodeAsync`: `SignInManager.TwoFactorRecoveryCodeSignInAsync(code)`. Same success/failure handling.

### 5.3 — MFA disable

**Prompt for agent:**
> [Apply UI rules] Create `Pages/Mfa/Disable.cshtml/.cs` (requires `[Authorize]`, requires 2FA enabled).
>
> GET: shows a warning card with amber background (`bg-amber-50 dark:bg-amber-900/20 border border-amber-200`): "Disabling two-factor authentication makes your account less secure." Two buttons: "Disable 2FA" (red primary: `bg-red-600 hover:bg-red-700`) and "Cancel" (secondary, link to `/account/security`).
>
> POST: `UserManager.SetTwoFactorEnabledAsync(false)`, `UserManager.ResetAuthenticatorKeyAsync`. Redirect to `/account/security`.

---

## Phase 6 — Account Settings

**Prompt for agent:**
> [Apply UI rules] Create a settings area at `Pages/Account/` in `AQ.Identity.UI` (all pages require `[Authorize]`).
>
> **Layout**: `Pages/Account/_AccountLayout.cshtml` — two-column on desktop (`grid grid-cols-[200px_1fr] gap-8`), stacked on mobile. Left column: vertical nav with links to Profile, Security. Current page link is highlighted (`bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 font-medium`). Right column: `@RenderSection("Content")`.
>
> **`Pages/Account/Profile.cshtml/.cs`**
> - Read-only email field (shown but not editable — changing email requires reverification, reserved for future)
> - Editable Full Name field
> - Save button
> - `OnPostAsync`: `UserManager.UpdateAsync(user)`. Show success banner on redirect.
>
> **`Pages/Account/Security.cshtml/.cs`**
> - Change password form: Current Password, New Password, Confirm New Password. `UserManager.ChangePasswordAsync`. Show inline errors.
> - MFA section: if disabled, shows "Two-factor authentication is off" with "Enable 2FA" link to `/auth/mfa/setup`. If enabled, shows "Two-factor authentication is on" with green badge and "Disable 2FA" link.
> - Active sessions section: table of active refresh tokens from OpenIddict (query `IOpenIddictTokenManager` for tokens with the current user's subject and type `refresh_token`). Columns: Created, Last used (if available). "Sign out all other devices" button (POST — revokes all refresh tokens for the user except the current session; identify current session by the `oi_tkn_id` claim in `User.Claims`).

---

## Phase 7 — Management API

**Goal:** FastEndpoints management API bundled in `AQ.Identity.OpenIddict`. Secured with `client_credentials` + `manage_api` scope.

**Prompt for agent:**
> In `AQ.Identity.OpenIddict/Management/`, add FastEndpoints endpoints under prefix `/manage/`. Protect all with `[Authorize(Policy = "ManageApi")]` — register this policy in `AddAqIdentity` requiring the `manage_api` scope.
>
> Implement:
> - `GET /manage/users` — paginated, search by email/name. Returns `DataSet<UserSummaryDto>`.
> - `GET /manage/users/{id}` — returns `UserDetailDto` (profile, MFA on/off, active token count, last login).
> - `PUT /manage/users/{id}/active` — body `{ "isActive": bool }`. Sets `user.IsActive`. Disabled users: add a check in the OpenIddict token validation pipeline that rejects tokens for inactive users (via `IOpenIddictServerHandler<ValidateTokenRequestContext>`).
> - `DELETE /manage/users/{id}/sessions` — revoke all refresh tokens for the user via `IOpenIddictTokenManager`.
> - `GET /manage/clients` — list all OpenIddict applications.
> - `POST /manage/clients` — create client from `IdentityClientConfig` JSON. Validate redirect URIs are absolute HTTPS (allow HTTP for localhost).
> - `PUT /manage/clients/{clientId}` — update existing client.
> - `GET /manage/audit-log` — paginated, filter by `userId`, `action`, `from`, `to`. Returns `DataSet<AuditEntryDto>`.
> - `POST /manage/keys/rotate` — force immediate key rotation via `SigningKeyManager`.

---

## Phase 8 — Social Login (Google)

**Prompt for agent:**
> In `AQ.Identity.OpenIddict/Extensions/ServiceCollectionExtensions.cs`, when `options.Google != null`, call:
> ```csharp
> services.AddAuthentication().AddGoogle(o => {
>     o.ClientId = options.Google.ClientId;
>     o.ClientSecret = options.Google.ClientSecret;
> });
> ```
>
> In `AQ.Identity.UI`, create `Pages/Auth/ExternalCallback.cshtml.cs` (no .cshtml needed — page handler only, no UI):
> - `OnGetAsync`: call `HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme)`.
> - Extract `email` and `name` from the external principal.
> - Look up user by email via `UserManager.FindByEmailAsync`.
>   - Found, external login not linked: `UserManager.AddLoginAsync`, then `SignInManager.SignInAsync`.
>   - Found, already linked: `SignInManager.ExternalLoginSignInAsync`.
>   - Not found: create `ApplicationUser` with `EmailConfirmed = true` (Google has verified it), `UserManager.CreateAsync` (no password), `UserManager.AddLoginAsync`, `SignInManager.SignInAsync`.
> - On success: redirect to validated `returnUrl`.
> - On any failure: redirect to `/auth/login` with an error query param.
>
> In `Login.cshtml`: render the "Continue with Google" button only when `IOptions<AqIdentityOptions>.Value.Google != null`. Clicking it POSTs to `/auth/external-login?provider=Google&returnUrl={returnUrl}` which triggers `SignInManager.ConfigureExternalAuthenticationProperties` and redirects to Google.

---

## Phase 9 — Token Signing Key Management

*(Already described in Phase 2.4 — this phase adds the background rotation worker)*

**Prompt for agent:**
> Add `KeyRotationWorker : BackgroundService` in `AQ.Identity.OpenIddict/KeyManagement/`. This worker runs every 24 hours. On each tick:
> - Call `SigningKeyManager` to check if the active key is within 30 days of expiry.
> - If so and no newer key exists: generate a new key, persist it, write `AuditEntry.Actions.KeyRotated`.
> - Mark keys whose `RetiredAt < DateTimeOffset.UtcNow` as effectively retired (they remain in DB but are excluded from JWKS by `SigningKeyManager`).
> Register `KeyRotationWorker` in `AddAqIdentity`.

---

## Phase 10 — Hardening

### 10.1 — Security middleware

**Prompt for agent:**
> Add `SecurityHeadersMiddleware` in `AQ.Identity.OpenIddict/Middleware/`. Sets response headers:
> - `X-Content-Type-Options: nosniff`
> - `X-Frame-Options: DENY`
> - `Referrer-Policy: strict-origin-when-cross-origin`
> - `Content-Security-Policy: default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; form-action 'self'; frame-ancestors 'none'`
>
> Add ASP.NET Core rate limiting in `AddAqIdentity`:
> - Auth endpoints (`/auth/login`, `/auth/register`, `/auth/forgot-password`): 5 POST requests/minute per IP
> - Token endpoint (`/connect/token`): 20 requests/minute per IP
> Use `AddRateLimiter` with `AddFixedWindowLimiter`.
>
> Register both in `UseAqIdentity`.

### 10.2 — Health checks and observability

**Prompt for agent:**
> In `AddAqIdentity`, register:
> - `GET /health` — checks `IIdentityDbContext` connectivity (run a `SELECT 1` via `context.Database.CanConnectAsync()`).
> - `GET /health/ready` — checks pending migrations via `context.Database.GetPendingMigrationsAsync()`.
> - Structured logging: document that the host app should configure Serilog. Add an `AddAqIdentityLogging` optional extension that sets up Serilog with console JSON sink and enrichers: `MachineName`, `Environment`, `ApplicationName`.
> - Add `X-Request-Id` middleware: reads `X-Request-Id` header if present, otherwise generates `Guid.NewGuid()`, sets on both `HttpContext.TraceIdentifier` and the response header.

---

## Phase 11 — ELS.Identity Host App

**Goal:** A thin host app in the ELS repo that composes all library packages, uses ELS's Postgres instance (separate `els_identity` database), and injects custom claims directly from `CoreDbContext`.

### 11.1 — New project

**Prompt for agent:**
> In `d:\Repositories\ELS\backend\src\Identity\`, create `ELS.Identity.csproj` — an ASP.NET Core web app targeting .NET 10. Add it to `ELS.sln`. References:
> - `AQ.Identity.Core` (NuGet or project reference during development)
> - `AQ.Identity.OpenIddict`
> - `AQ.Identity.UI`
> - `AQ.Identity.Email`
> - `ELS.Core.Infrastructure` (project reference — for `CoreDbContext`)
> - `Npgsql.EntityFrameworkCore.PostgreSQL`
>
> Add `appsettings.json`:
> ```json
> {
>   "ConnectionStrings": {
>     "IdentityDb": "Host=localhost;Database=els_identity;Username=postgres;Password=postgres"
>   },
>   "Identity": {
>     "Issuer": "http://localhost:5001",
>     "AppName": "ELS",
>     "Email": { "Host": "localhost", "Port": 1025, "FromAddress": "noreply@els.local", "FromName": "ELS" }
>   }
> }
> ```
>
> Add `config/clients.json`:
> ```json
> [
>   {
>     "clientId": "els-web",
>     "displayName": "ELS Web App",
>     "type": "public",
>     "redirectUris": ["http://localhost:3000/api/auth/callback/els"],
>     "postLogoutRedirectUris": ["http://localhost:3000"],
>     "scopes": ["openid", "profile", "email", "offline_access", "els_api"]
>   },
>   {
>     "clientId": "els-mobile",
>     "displayName": "ELS Mobile App",
>     "type": "public",
>     "redirectUris": ["msauth.com.els.mobile://auth"],
>     "postLogoutRedirectUris": [],
>     "scopes": ["openid", "profile", "email", "offline_access", "els_api"]
>   }
> ]
> ```

### 11.2 — ELS DbContext (identity DB)

**Prompt for agent:**
> Create `ELS.Identity.Infrastructure/ElsIdentityDbContext.cs`:
> ```csharp
> public class ElsIdentityDbContext
>     : OpenIddictDbContext<ApplicationUser, IdentityRole<Guid>, Guid>,
>       IIdentityDbContext
> {
>     public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();
>     public DbSet<SigningKey> SigningKeys => Set<SigningKey>();
>
>     protected override void OnModelCreating(ModelBuilder builder)
>     {
>         base.OnModelCreating(builder); // applies OpenIddict + Identity table configs
>         builder.ApplyConfigurationsFromAssembly(typeof(ElsIdentityDbContext).Assembly);
>     }
> }
> ```
>
> Configure Npgsql: `UseNpgsql(connectionString)`. Add EF configurations for `ApplicationUser` (table `users`), `AuditEntry` (table `audit_log`), `SigningKey` (table `signing_keys`). Create the initial migration: `dotnet ef migrations add InitialCreate --project ELS.Identity.Infrastructure`. Add a `create-migration.ps1` script in `backend/src/Identity/` matching the pattern in `backend/db/`.

### 11.3 — Claims enricher

**Prompt for agent:**
> Create `ELS.Identity.Api/ClaimsEnricher.cs`:
>
> ```csharp
> public class ElsClaimsEnricher(CoreDbContext db, ILogger<ElsClaimsEnricher> logger)
>     : IClaimsEnricher
> {
>     public async Task<IReadOnlyDictionary<string, string>> EnrichAsync(
>         Guid subject, string clientId, IEnumerable<string> scopes, CancellationToken ct)
>     {
>         // subject is ApplicationUser.Id — map to ELS User via ExternalId
>         // (ApplicationUser.Id == ELS User.ExternalId — set on first registration)
>         var user = await db.Users
>             .AsNoTracking()
>             .Include(u => u.WorkspaceMemberships)
>             .FirstOrDefaultAsync(u => u.ExternalId == subject, ct);
>
>         if (user is null)
>         {
>             logger.LogWarning("No ELS user found for identity subject {Subject}", subject);
>             return ImmutableDictionary<string, string>.Empty;
>         }
>
>         var workspaceIds = user.WorkspaceMemberships
>             .Select(m => m.WorkspaceId.ToString());
>
>         var workspacePerms = user.WorkspaceMemberships
>             .Select(m => $"{m.WorkspaceId}:{(int)m.Permission}");
>
>         return new Dictionary<string, string>
>         {
>             ["user_app_id"]           = user.Id.ToString(),
>             ["user_permissions"]      = ((long)user.Permissions).ToString(),
>             ["workspaces"]            = string.Join(" ", workspaceIds),
>             ["workspace_permissions"] = string.Join(" ", workspacePerms),
>             ["claims_version"]        = user.ClaimsVersion.ToString()
>         };
>     }
> }
> ```
>
> Note: first-time registration flow — when a user registers in `ELS.Identity`, `ELS.ClaimsEnricher` will not find an ELS user yet (they haven't enrolled in ELS). The enricher returns empty claims for new users. The ELS API's existing auto-registration logic (creating a user on first authenticated API call) handles creating the ELS user record and linking it by `ExternalId`. Ensure `User.ExternalId` is set to `ApplicationUser.Id` during that auto-registration.

### 11.4 — Program.cs

**Prompt for agent:**
> In `ELS.Identity.Api/Program.cs`:
>
> ```csharp
> var builder = WebApplication.CreateBuilder(args);
>
> var identityOptions = builder.Configuration.GetSection("Identity").Get<AqIdentityOptions>()!;
> var clients = JsonSerializer.Deserialize<List<IdentityClientConfig>>(
>     File.ReadAllText("config/clients.json"))!;
>
> builder.Services
>     .AddDbContext<ElsIdentityDbContext>(o =>
>         o.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")))
>     .AddAqIdentity<ElsIdentityDbContext>(identityOptions, clients)
>     .AddAqIdentityEmail(identityOptions.Email, builder.Environment)
>     .AddScoped<IClaimsEnricher, ElsClaimsEnricher>()
>     .AddDbContext<CoreDbContext>(o =>       // read-only reference for claim enrichment
>         o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
>         .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
>
> var app = builder.Build();
> app.UseAqIdentity();
> app.MapHealthChecks("/health").AllowAnonymous();
> app.Run();
> ```

### 11.5 — ELS API changes

**Prompt for agent:**
> In `ELS.Core.Api`:
>
> 1. In `ServiceCollectionExtensions.cs`, replace `AddMicrosoftIdentityWebApi(...)` with:
>    ```csharp
>    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
>        .AddJwtBearer(o =>
>        {
>            o.Authority = configuration["Identity:Issuer"]; // e.g. http://localhost:5001
>            o.Audience  = configuration["Identity:Audience"]; // e.g. "els_api"
>            o.MapInboundClaims = false;
>        });
>    ```
> 2. Remove `AddMicrosoftGraph`, `AddInMemoryTokenCaches`, `EnableTokenAcquisitionToCallDownstreamApi`, and the `EntraClaimsProvider` policy.
> 3. Delete `ClaimsProviderEndpoint.cs` and its request/response models — the enrichment is now in-process in `ELS.Identity`.
> 4. Rename `User.EntraId` → `User.ExternalId` (property rename + EF migration for column rename: `RenameColumn("EntraId", "users", "ExternalId")`). Update all references.
> 5. In `appsettings.json`: remove `AzureAd` and `CustomExtensions` sections. Add `Identity: { Issuer: "http://localhost:5001", Audience: "els_api" }`.
> 6. `ClaimsRevocationMiddleware`, all authorization handlers, all endpoints — no changes.

### 11.6 — ELS Web and Mobile

*(Unchanged from previous plan — same MSAL → next-auth and oauth2Config.ts swap)*

**Web**: Remove MSAL, add `next-auth@5` with OIDC provider pointing at `http://localhost:5001`.

**Mobile**: Change `EXPO_PUBLIC_MSAL_AUTHORITY` → `EXPO_PUBLIC_IDENTITY_ISSUER`, remove Entra-specific API scope, remove `fetchDiscoveryWithFallback` fallback logic.

### 11.7 — Docker Compose

**Prompt for agent:**
> Update `d:\Repositories\ELS\docker-compose.yml` (or create it if absent). Add:
>
> ```yaml
> services:
>   els-identity:
>     build:
>       context: ./backend
>       dockerfile: src/Identity/Dockerfile
>     ports:
>       - "5001:8080"
>     environment:
>       - ASPNETCORE_ENVIRONMENT=Development
>       - ConnectionStrings__IdentityDb=Host=postgres;Database=els_identity;Username=postgres;Password=${POSTGRES_PASSWORD}
>       - ConnectionStrings__DefaultConnection=Host=postgres;Database=els_core;Username=postgres;Password=${POSTGRES_PASSWORD}
>       - Identity__Issuer=http://localhost:5001
>     depends_on:
>       - postgres
>       - mailpit
>
>   els-api:
>     # existing service — add env var:
>     environment:
>       - Identity__Issuer=http://localhost:5001
>       - Identity__Audience=els_api
>
>   postgres:
>     image: postgres:16
>     environment:
>       POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgres}
>     volumes:
>       - pgdata:/var/lib/postgresql/data
>     # Creates both databases on first run:
>     # els_core and els_identity are created automatically by EF migrations
>
>   mailpit:
>     image: axllent/mailpit
>     ports:
>       - "1025:1025"   # SMTP
>       - "8025:8025"   # Web UI
>
> volumes:
>   pgdata:
> ```
>
> Note: `els-identity` and `els-api` share the same `postgres` container but use different `Database` values in their connection strings — completely separate logical databases, no schema overlap. Both databases are created by their respective EF migrations running at startup.

---

## Phase 12 — Testing

### Library unit tests

**Prompt pattern:**
> Write xUnit tests for `[class]` in `AQ.Identity.Core.Tests` / `AQ.Identity.OpenIddict.Tests`. Use NSubstitute for mocks, FluentAssertions for assertions. Follow conventions in `AQ-Libraries/dotnet/tests/`. Pure unit tests only — no database.

**What to cover:**
- `ClaimsEnrichmentHandler`: enricher returns claims → merged into principal; enricher throws → token issued without extra claims, warning logged
- `ClientSeeder`: idempotent — running twice does not duplicate clients
- `SigningKeyManager`: no keys → generates one; key near expiry → generates new alongside; expired key excluded from signing; retired key excluded from JWKS
- `DefaultEmailTemplateService`: verification URL appears in both HTML and text body; correct subject
- `AqIdentityOptions` defaults: correct lockout duration, token lifetimes

### Integration tests

**Prompt for agent:**
> In `ELS.Identity.Tests/`, use `WebApplicationFactory<Program>` with a test Postgres database. `TestWebApplicationFactory` overrides the identity DB connection string to `els_identity_test_{Guid.NewGuid():N}` and runs migrations on startup. Use `WireMock.Net` if any HTTP stubs are needed. Test:
> - Authorization Code + PKCE flow end-to-end (simulate browser redirects via `HttpClient`)
> - Registration → email verification → login
> - `IClaimsEnricher` result (`ElsClaimsEnricher`) is reflected in the decoded JWT claims
> - Inactive user (`IsActive = false`) cannot obtain tokens
> - Revoked refresh token is rejected on refresh
>
> Clean up each test's database in `IAsyncLifetime.DisposeAsync`.

---

## Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| Token endpoint p99 latency | < 200ms |
| Claims enrichment | In-process; no network call |
| Refresh token lifetime | 14 days sliding (configurable) |
| Access token lifetime | 1 hour (configurable) |
| Password minimum | 8 chars, 1 digit (configurable) |
| Account lockout threshold | 5 failed attempts (configurable) |
| Lockout duration | 15 minutes (configurable) |
| Key rotation period | 90 days (configurable) |
| Key retirement overlap | 30 days (configurable) |
| Signing algorithm | RS256 (RSA 2048-bit) |

---

## Development Sequence Summary

| Phase | Agent Task | Depends On | Est. |
|-------|-----------|-----------|------|
| 0 | Library project scaffold | — | 1h |
| 1.1 | Entities | 0 | 1h |
| 1.2 | Abstractions (`IClaimsEnricher`, `IEmailService`, etc.) | 0 | 1h |
| 1.3 | Configuration models | 0 | 1h |
| 2.1 | `AddAqIdentity` extension | 1 | 3h |
| 2.2 | Claims enrichment handler | 2.1 | 2h |
| 2.3 | Client seeder | 2.1 | 2h |
| 2.4 | Key manager | 2.1 | 3h |
| 3.1 | Shared UI layout + partials | 0 | 2h |
| 3.2 | Login page | 3.1, 2.1 | 2h |
| 3.3 | Register page | 3.1 | 2h |
| 3.4 | Email verification pages | 3.1, 4.1 | 2h |
| 3.5 | Password reset pages | 3.1, 4.1 | 2h |
| 3.6 | Lockout + logout | 3.1 | 1h |
| 4.1 | Email service + templates (SMTP, console, `DefaultEmailTemplateService`) | 1.2 | 4h |
| 5.1 | TOTP setup + backup codes | 3.1 | 3h |
| 5.2 | MFA login challenge | 5.1 | 2h |
| 5.3 | MFA disable | 5.1 | 1h |
| 6 | Account settings pages | 3.1, 5.1 | 3h |
| 7 | Management API | 2.1 | 4h |
| 8 | Google social login | 3.2 | 3h |
| 9 | Key rotation background worker | 2.4 | 1h |
| 10.1 | Security middleware + rate limiting | 2.1 | 2h |
| 10.2 | Health checks + observability | 2.1 | 2h |
| 11.1 | `ELS.Identity` project scaffold | 2.1 | 2h |
| 11.2 | `ElsIdentityDbContext` + migrations | 11.1 | 2h |
| 11.3 | `ElsClaimsEnricher` | 11.2 | 2h |
| 11.4 | `Program.cs` wiring | 11.1–11.3 | 1h |
| 11.5 | ELS API changes (JWT swap, rename EntraId) | 11.4 | 2h |
| 11.6 | ELS web + mobile changes | 11.4 | 4h |
| 11.7 | Docker Compose update | 11.4 | 1h |
| 12 | Unit + integration tests | all | 6h |
| **Total** | | | **~63h** |

---

## How to Use This Plan with an LLM

Each phase section contains a **"Prompt for agent"** block. To use it:

1. Start a new conversation (fresh context for each phase).
2. Paste the relevant `CLAUDE.md` files as context (root + layer-specific).
3. Paste the agent prompt verbatim.
4. After the agent delivers, run `dotnet build` and fix any compile errors before moving to the next phase.
5. For UI phases, start the server and verify the page in a browser before marking done.

Parallel execution: phases 1.1, 1.2, and 1.3 can run in parallel. Phases 3.x and 4.x can be parallelised after Phase 2.1 is done. Phase 11 can only start after Phase 2 is complete and stable.
