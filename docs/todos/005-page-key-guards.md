# 005. Hold the page-wide hotkeys back while a grid loads or the user types

Status: Open
Date: 2026-11-16
Location: `react/src/mantine/data-grid/DataGrid.tsx` (page key handler)

## Context
The page-wide keys (create, refresh, search) skip the typing guard when the combination holds Ctrl, Cmd or Alt, and they ignore the grid's `loading` state. Alt+N therefore opens create while a grid is loading (its button cannot be clicked then) and while the user types in a text field outside a dialog, where it can drop a half-written form. Alt+N and Alt+R are also taken by some browsers and screen readers.

## Scope
Decide the rule, then apply it in the page key handler: skip while `loading`, and skip in a text field unless the app asked for it. Possibly add a per-grid option to keep the modifier keys working in fields. Update RCT-22.

## Impact
Alt+N and Alt+R stop working in fields and while loading; an app that relies on that has to opt in.

## Options
1. Skip while loading and in any text field: simple, predictable, gives up the keys inside forms
2. Skip while loading only; a field keeps Alt combinations: keeps today's reach, keeps the form-loss risk

## Done when
RCT-22 states the rule and the handler follows it, with a browser check of each case, and the `TODO-005` comment is gone.
