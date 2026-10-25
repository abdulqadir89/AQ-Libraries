# 002. The grid filter is one expression, with a builder beside the column popovers

Status: Accepted
Date: 2026-10-25

## Context
The data grid kept one `property,operator,value` string per column. That cannot express a list column (a document with several tags: any, all or none of them), two conditions on one column, OR across columns or negation, and the key of each entry was read from the first comma of its own string. The server filter could not compare a list property at all, and read `a && b || c` as one flat group.

## Options considered
1. Make the column popover emit and read compound strings, inferring the meaning from their shape: small change, but every shape the popover cannot show becomes a dead end, and the per-column key and the URL parsing stay tied to the three-part string.
2. A filter specific to one consuming app (a tag filter on the document endpoint): works at once, but every list column on every endpoint would need its own code.
3. One filter expression as the grid state, a builder for any expression, and popovers as views over it: more work in shared code, one format everywhere.

## Decision
Option 3. The server filter understands list paths (some element matches; the negative operators mean no element matches), `!( … )` negation, quoted values and `&&` before `||`. The grid holds the filter as a tree written as one expression, shows it as chips, and opens a builder (nested groups, all/any/not, sort) from the Filter and Sort buttons. A column popover edits only its own top-level conditions and hands anything larger to the builder.

## Why
The expression is already what the server takes, the URL carries and a saved view would store. Making the UI speak it removes the translation layer instead of growing it, and list columns need no code of their own.

## Consequences
Consuming grids move from `onFilterChange(conditions)` and `initialState.columnFilters` to `onFilterExpressionChange` and `initialState.filterExpression`; both old forms keep working until todo 003. A consumer that needs the builder in its own modal or drawer passes `filterBuilderHost`. A filter that mixed `&&` and `||` without brackets now means what precedence says, not "the last operator wins".
