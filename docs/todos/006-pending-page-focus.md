# 006. Make the focus after a page change robust

Status: Done
Date: 2026-11-16
Location: `react/src/mantine/data-grid/DataGrid.tsx` (pending row focus)

## Context
After Down on the last row (or Up on the first) the grid requests another page and waits to focus a row of it. The wait is dropped by any key press on a row, bare Shift or Ctrl included, and it is only released when the new page's row keys differ from the old ones, so a page with the same keys (index-based row keys) never gets its focus and the wait lingers until the next key.

## Scope
Ignore modifier-only key presses when dropping the wait. Release it on a clear signal from the consumer (`loading` going true then false, or the pagination changing) instead of comparing row keys. Re-check the refused-change and clamped-change cases.

## Impact
Only keyboard users paging with the keys notice; no API change.

## Options
1. Drive the wait from `loading` and `pagination.current`: no key comparison, needs the consumer to report `loading`
2. Keep the row-key comparison and add a time limit: works without `loading`, brings back a timer

## Done when
Paging with the keys focuses the right row with and without `loading`, tapping a modifier does not cancel it, and the `TODO-006` comment is gone.
