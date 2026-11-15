# 004. Let a hotkey combination name "+" and hold a space

Status: Open
Date: 2026-11-16
Location: `react/src/mantine/data-grid/gridHotkeys.ts`, `react/src/mantine/data-grid/DataGrid.tsx` (page-key attribute)

## Context
The hotkey parser splits a combination on `+` and the page-key ownership check splits a list of combinations on a space. A combination whose key is `+` (`alt++`) parses to an empty key and never matches, and a combination with a space in it (`alt + N`) is cut apart. Both fail silently, with no console warning. Found in review of the hotkey PR (#39).

## Scope
Parse `+` as the key when it ends the combination, trim spaces around parts, and either warn on a combination that parses to an empty key or carry the combinations in the page-key attribute in a form that cannot clash with their content (a JSON list). Touches `parseHotkey`, `ownsPageKeys` and the attribute written by `DataGrid`.

## Impact
No consuming app uses these combinations today; an app that tried would see the key do nothing. No behaviour change for the defaults.

## Options
1. Fix the parser and the attribute format: handles every combination, one place
2. Reject such combinations with a warning: smaller, but `+` stays unbindable

## Done when
`alt++` and `alt + N` work as combinations, a combination that cannot work is reported, and the `TODO-004` comment is gone.
