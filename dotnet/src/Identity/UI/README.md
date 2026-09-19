# AQ.Identity.UI

Shared Razor Class Library (RCL) providing the OpenIddict login/consent/register/account/
manage pages used by every app that references this project. It ships:

- `Pages/Auth/*` — login, register, forgot/reset password, consent, MFA, lockout
- `Pages/Account/*` — self-service profile/security/sessions/apps
- `Pages/Manage/*` — admin console for clients/scopes/users
- `Pages/Shared/_AuthLayout.cshtml`, `Pages/Account/_AccountLayout.cshtml`,
  `Pages/Manage/_ManageLayout.cshtml` — the three layouts every page above renders inside
- A theme system (`tailwind.input.css`, six built-in `[data-theme]` palettes, light/dark
  via `[data-mode="dark"]`) shared by every consumer

Because this is shared across multiple apps (ELS, and any future app that references it),
it intentionally has **no app-specific branding** — only a generic text wordmark
(`@options.Value.AppName`) as the default logo. Do not add any one app's brand assets,
colors, or fonts here.

## Adding a logo for your app

Two ways to do it, in order of preference:

### 1. `Branding.LogoUrl` config (simplest — works today, zero code changes)

Every layout already has:

```csharp
@if (!string.IsNullOrEmpty(options.Value.Branding.LogoUrl))
{
    <img src="@options.Value.Branding.LogoUrl" alt="@options.Value.AppName" class="..." />
}
else
{
    <span class="...">@options.Value.AppName</span>
}
```

Set `Identity:Branding:LogoUrl` in your app's `appsettings.json` to a URL (can be a static
file served from your own app's `wwwroot`, e.g. `/logo.svg`). This is enough if a static
raster/SVG image is good enough for your brand.

Limitation: this only supports a single flat `<img>`. It won't adapt per light/dark mode
or carry multiple wordmark languages/taglines the way an inline SVG component can.

### 2. Override the Razor views in your own app (for a full inline-SVG brand mark)

ASP.NET Core resolves an app's own `Pages/<path>.cshtml` **ahead of** an RCL's view at the
same relative path — no extra wiring needed beyond `AddRazorPages()` / `MapRazorPages()`,
which every consumer already calls. This lets your app fully replace `_AuthLayout.cshtml`,
`_AccountLayout.cshtml`, and/or `_ManageLayout.cshtml` with a version that renders your own
brand mark, while every other page (login form, MFA, admin console, etc.) keeps coming from
this shared library untouched.

**Steps** (see `ELS/backend/src/Identity/Pages/` for a full example):

1. In your app's own Web SDK project (not this RCL), add:
   - `Pages/_ViewImports.cshtml` with at least `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`
     (needed for `<partial>` to work — your app's `Pages/` has no imports by default, unlike
     this RCL which already has `@using` on every file).
   - `Pages/Shared/_AuthLayout.cshtml`, `Pages/Account/_AccountLayout.cshtml`,
     `Pages/Manage/_ManageLayout.cshtml` — copy the current version from this RCL as your
     starting point, then swap only the logo block for your own brand partial. Keep
     everything else identical so you don't silently diverge from shared layout fixes.
   - Your own logo partial (e.g. `Pages/Shared/_BrandLockup.cshtml` + a matching
     `BrandLockupModel.cs`) and any brand CSS/fonts in your app's own `wwwroot/`.
2. Link your brand stylesheet after `identity.css` in each overridden layout:
   `<link rel="stylesheet" href="/css/brand.css" />`.
3. If you self-host a font, put the files under your app's own `wwwroot/fonts/` and
   `@font-face` them in your brand CSS — do not add external font CDN links; the shared
   `SecurityHeadersMiddleware` CSP is locked to `style-src 'self'` / `default-src 'self'`
   for every consumer, and loosening it here would weaken it for all of them.
4. Keep your override in sync with this RCL's original manually — there's no compiler
   check that flags drift when the shared layout changes. Diff your app's copy against
   this RCL's version occasionally, especially after upgrading the `AQ.Identity.UI`
   package/reference.

**Do not** add your app's SVG markup, hex colors, `@font-face` rules, or font files into
this RCL — that leaks one app's brand into every other consumer's build. Everything
brand-specific belongs in the *consuming* app's own project.
