# 001. Make the reserved service-account claim set configurable per app

Status: Open
Date: 2026-10-18
Location: `dotnet/src/Identity/Core/PrincipalClaims.cs`

## Context
The reserved claim types (those a service account may never set) are a fixed set in the shared library. It includes `els_internal_user_id`, an app-specific claim name that does not belong in a shared library.

## Scope
Move the app-specific name out of the library and let each app add reserved claim types through configuration or registration. Touches `PrincipalClaims`, the validation caller, its tests (rule IDC-1) and the apps that need the extra name.

## Impact
Apps that rely on `els_internal_user_id` being blocked must register it themselves, otherwise it becomes settable.

## Options
1. Options or registration API that adds names to the base set: keeps the safe defaults.
2. Pass the full set at each call: easy to forget a name, weaker.

## Done when
The library holds no app-specific claim name, apps register theirs, and the `TODO-004` comment is removed.
