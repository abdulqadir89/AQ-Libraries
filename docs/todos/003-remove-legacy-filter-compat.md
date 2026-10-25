# 003. Remove the legacy column-filter compatibility

Status: Open
Date: 2026-10-25
Location: `react/src/mantine/data-grid/DataGrid.tsx`, `react/src/mantine/data-grid/DataGrid.types.ts`

## Context
The data grid now keeps its filter as one expression (a group tree written as a `filterExpression` string, RCT-6). Grids written before that read the flat `onFilterChange(conditions)` callback and the per-column `columnFilters` map in the state snapshot and initial state. Both are still delivered so those grids keep working, which means two representations of the same filter.

## Scope
Drop the `onFilterChange` condition list and the `columnFilters` map from `DataGridInitialState` and `DataGridStateSnapshot`, and the column-key-from-expression parsing that serves them. Keep `filterExpression` only. Touches `DataGrid`, `ColumnFilter`, `CardDataGrid`, the types, and `docs/LIBRARIES.md` (RCT-6).

## Impact
Every consuming grid that still builds its expression from `onFilterChange` or seeds `columnFilters` must move to `onFilterExpressionChange` and `initialState.filterExpression` first. All DQM list grids have moved; ELS has to be checked.

## Options
1. Remove in one release once every consuming app has moved: simplest, needs the apps to be done.
2. Keep the compatibility behind a deprecation warning for a release: gentler, leaves the dual state longer.

## Done when
No consuming app passes `onFilterChange` or `initialState.columnFilters`, the compatibility code is removed, and every `TODO-003` comment is gone.
