# 010. Add keyboard navigation and hotkeys to the card grid

Status: Open
Date: 2026-11-16
Location: `react/src/mantine/data-grid/CardDataGrid.tsx`

## Context
The table grid answers to row and page keys, but `CardDataGrid` has no keyboard support. An app that lets the user switch between table and card view gets the keys in one view only, and `ActionButton.hotkey` does nothing in cards.

## Scope
Roving focus over the cards in two dimensions (the layout columns decide Up and Down), the same selection, create, refresh and search keys, and the shared key configuration (`useGridHotkeys`). Update RCT-21 and RCT-22 and the note in `react/CLAUDE.md`.

## Impact
Adds focus stops and key handling to the card view; apps see the keys appear there.

## Options
1. Share the key handling with the table grid by extracting it into a hook: one behaviour, a refactor of the table grid first
2. Write a second implementation for cards: quicker, two copies to keep in step

## Done when
Cards can be walked and acted on with the same keys as rows, the rules cover both views, and the `TODO-010` comment is gone.
