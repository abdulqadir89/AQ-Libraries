# 009. Settle what Enter opens and how a link action is run

Status: Open
Date: 2026-11-16
Location: `react/src/mantine/data-grid/DataGrid.tsx` (the open key, `clickRowAction`)

## Context
Enter runs Details, else Overview, else Edit by clicking the first of those buttons that is shown and enabled, and does nothing when none is. A hotkey runs a link action (`href`) by clicking its anchor, so opening it in a new tab is not possible and a disabled anchor is only recognised by the `disabled` and `data-disabled` attributes.

## Scope
Decide whether Enter should be configurable (an ordered list of action keys), what it does with no action, and whether Ctrl+Enter should open a link action in a new tab. Check how Mantine marks a disabled anchor.

## Impact
Changes what Enter does only where an app hides or disables Details.

## Options
1. Make the order configurable and add a modifier for a new tab: flexible, more surface
2. Keep the fixed order and document it: no change, stays rigid

## Done when
The behaviour of Enter with missing, hidden and disabled actions and with link actions is stated in RCT-21 and checked in the browser, and the `TODO-009` comment is gone.
