# 007. Translate the key names shown in hotkey tooltips

Status: Done
Date: 2026-11-16
Location: `react/src/mantine/data-grid/gridHotkeys.ts` (`formatHotkey`)

## Context
Tooltips such as "Edit (Alt+E)" build their key names (Ctrl, Cmd, Alt, Option, Shift, Win, Space and the arrow names) in English inside `formatHotkey`. The package rule is that user-visible text comes from the locale messages (RCT-2).

## Scope
Add the modifier and key names to the data grid messages (`DataGridMessages`, `defaultMessages`), have `formatHotkey` take them, and document them in `react/CLAUDE.md` where it lists the localized components.

## Impact
Apps that translate the grid messages get translated tooltips; apps that do not see no change. A new required field in `DataGridMessages` means apps that supply a full message set must add it.

## Options
1. Messages in the data grid group: consistent with the rest, widens `DataGridMessages`
2. Leave the symbols (Ctrl, Alt) untranslated and translate only the word keys (Space, Delete): smaller, still partly English

## Done when
A translated message set changes every key name in the tooltips, and the `TODO-007` comment is gone.
