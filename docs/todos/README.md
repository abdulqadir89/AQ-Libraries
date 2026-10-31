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
| 001 | [Make the reserved service-account claim set configurable per app](001-reserved-claims-per-app.md) | 🔴 Open |
| 002 | [Use native OpenIddict back-channel logout](002-native-backchannel-logout.md) | 🔴 Open |
| 003 | [Remove the legacy column-filter compatibility](003-remove-legacy-filter-compat.md) | 🔴 Open |
