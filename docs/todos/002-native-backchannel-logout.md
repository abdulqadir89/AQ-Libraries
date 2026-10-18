# 002. Use native OpenIddict back-channel logout

Status: Open
Date: 2026-10-18
Location: `dotnet/src/Identity/OpenIddict/Sessions/BackchannelLogout.cs`

## Context
OpenID Connect Back-Channel Logout is implemented by hand in the library because OpenIddict 7 has no native support. Native support is planned for OpenIddict 8.0 (openiddict-core#2175).

## Scope
When OpenIddict 8 is stable, replace the custom implementation with the native one and migrate stored client settings if names differ. Depends on the OpenIddict 8 release.

## Impact
Touches logout for every relying party. Verify against real clients before removing the custom code.

## Options
1. Adopt the native feature after upgrading: less code to own.
2. Keep the custom code: works today, owned by us.

## Done when
The custom back-channel code is removed (or an ADR keeps it), and logout works for existing clients.
