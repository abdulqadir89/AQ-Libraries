# Todos

One file per deferred piece of work: `NNN-short-title.md` (three digits, next free number). Every code `TODO` has a file here.
Rules for when to write one: root `CLAUDE.md`, section "Documentation".

```markdown
# NNN. Title

Status: Open | In progress | Done | Dropped
Date: YYYY-MM-DD
Location: file or area the todo concerns

## Context
Why this exists, what triggered it.

## Scope
What has to change, where, and roughly how much.

## Impact
What can break, what behaviour changes, which consuming apps notice.

## Options
1. Option: pros / cons

## Done when
How we know it is finished.
```

When a todo is finished, set Status to `Done` and keep the file. If finishing it changes or reverts an approach, also write an ADR.
Every code `TODO` carries an id: `// TODO-NNN: short reason`, pointing at its file here. A bare `TODO` fails `docs-sync.ps1`.

## Index

Legend: 🔴 Open · 🟡 In progress · 🟠 Under consideration · 🟢 Done · ⚪ Dropped

| # | Todo | Status |
|---|------|--------|
| 001 | [Make the reserved service-account claim set configurable per app](001-reserved-claims-per-app.md) | 🟢 Done |
| 002 | [Use native OpenIddict back-channel logout](002-native-backchannel-logout.md) | 🔴 Open |
| 003 | [Remove the legacy column-filter compatibility](003-remove-legacy-filter-compat.md) | 🟢 Done |
| 004 | [Let a hotkey combination name "+" and hold a space](004-hotkey-combo-grammar.md) | 🟢 Done |
| 005 | [Hold the page-wide hotkeys back while a grid loads or the user types](005-page-key-guards.md) | 🟢 Done |
| 006 | [Make the focus after a page change robust](006-pending-page-focus.md) | 🟢 Done |
| 007 | [Translate the key names shown in hotkey tooltips](007-hotkey-labels-locale.md) | 🟢 Done |
| 008 | [Memoise the hotkey work and tighten its conflict check](008-hotkey-resolution-cost.md) | 🟢 Done |
| 009 | [Settle what Enter opens and how a link action is run](009-enter-open-and-link-actions.md) | 🟢 Done |
| 010 | [Add keyboard navigation and hotkeys to the card grid](010-card-grid-keyboard.md) | 🟢 Done |
| 011 | [Show the shortcuts in a dialog and handle layouts where "/" needs Shift](011-hotkey-help-and-layouts.md) | 🟢 Done |
