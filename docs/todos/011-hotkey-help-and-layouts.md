# 011. Show the shortcuts in a dialog and handle layouts where "/" needs Shift

Status: Done
Date: 2026-11-16
Location: `react/src/mantine/data-grid/gridHotkeys.ts` (default keys), `react/src/mantine/data-grid/DataGrid.tsx`

## Context
The only way to learn the keys is the button tooltips. A shortcuts dialog (opened with `?`) was left out of the first version. The default search key `/` is typed with Shift on some layouts (German), where it does not match, so those users have no default search key until the app overrides it.

## Scope
Add a dialog listing the grid's resolved keys (messages go through the locale, see todo 007) and decide how the default search key should work across layouts: a second default, a layout-aware match on the key code, or documenting the override.

## Impact
A new key (`?`) and new messages; no change for an app that does not open the dialog.

## Options
1. Dialog plus a second default for search (`mod+K`): covers every layout, one more default key
2. Dialog only; document the override for `/`: smaller, leaves some layouts without a default

## Done when
Pressing `?` lists the keys that work in the grid, search has a default on every common layout, and the `TODO-011` comment is gone.
