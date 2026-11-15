# 008. Memoise the hotkey work and tighten its conflict check

Status: Open
Date: 2026-11-16
Location: `react/src/mantine/data-grid/DataGrid.tsx` (hotkey resolution, conflict check, page key listener)

## Context
Every render of a grid rebuilds the hotkey map, the action lookup tables and the conflict list, re-assigns the page key handler in an effect, and each mounted grid adds its own document keydown listener. The conflict check compares every key name, including keys of actions the grid does not render, and misses two custom actions that share a key or a custom key that shadows a built-in one. The built-in action key lookup is a cast the compiler cannot check, and the warning also logs in production.

## Scope
Memoise on the resolved keys and the action list, assign the handler during render (or use an effect event), and share one document listener across grids. Compare only live actions and every pair, and check the built-in key names against `GridHotkeyName` without a cast.

## Impact
No behaviour change for correct configurations; wrong ones get an accurate warning.

## Options
1. Memoise and fix the check in place: small, keeps one listener per grid
2. A shared listener plus a registry of grids: also replaces the attribute lookup, bigger change

## Done when
A grid render does no hotkey work when its keys and actions are unchanged, the conflict warning names only real conflicts, and the `TODO-008` comment is gone.
