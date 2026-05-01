# AQ-Libraries — React (@AQ/react-components)

See root `CLAUDE.md` for overall repo context and dependency management rationale.

## Package Identity

- **Package name**: `@AQ/react-components`
- **Entry point**: `src/index.ts` (source-only; no build step required by consumers)
- **Consumed via**: tsconfig path alias in ELS web → resolves to `react/src/index.ts`

## Source Layout

```
react/src/
  index.ts                        # Root export — re-exports everything
  mantine/
    index.ts
    address/                      # AddressInput component
    autocomplete/                 # AutoCompleteCombo
    data-grid/                    # DataGrid, CardDataGrid, ColumnFilter, DataGridSwitch, DataGridViewSwitcher
    datetime/                     # DateTimeOffsetDisplay, DateTimeOffsetRangeInput, DateRangeDisplay, DateTimeOffsetRangeDisplay
    theme/                        # ThemeProvider, ThemeSwitcher, blue/zinc themes + CSS variable resolvers
  utils/
    index.ts
    FilterExpressionBuilder.ts    # Builds FilterExpression strings for API queries
    SortExpressionBuilder.ts      # Builds SortExpression strings for API queries
    DateTimeOffsetUtils.ts        # DateTimeOffset parsing/formatting helpers
```

## Dependency Rules (Critical)

- **Only** `typescript` and `@types/*` in `devDependencies`.
- All runtime deps (React, Mantine, TanStack Query, dayjs, Tabler icons) are **peer dependencies** — consumers must install them.
- This prevents duplicate module instances (React context, MantineProvider) in the consuming app.
- Never add a runtime dependency to `dependencies` — always `peerDependencies`.

## Key Rules

- **No build step**: the library is consumed as raw TypeScript source. Don't add a compilation/bundle step without discussion.
- **Peer dep versions**: React ≥19, Mantine ≥8, TanStack Query ≥5, dayjs ≥1, Tabler icons ≥3.
- **TypeScript strict**: no `any`. Types must align with what ELS web and mobile expect.
- **Exports**: always re-export new components through the nearest `index.ts` barrel and up to `src/index.ts`.
- **Test in ELS context**: changes here affect ELS web SSR. Test against ELS web before considering a change complete.
- **No duplicate React/Mantine**: if you add a dependency that includes React or Mantine as a transitive dep, make it a peer dep instead.

## Adding a New Component

1. Create folder under `src/mantine/<name>/` (or `src/utils/` for utilities).
2. Export from a local `index.ts` inside that folder.
3. Re-export from `src/mantine/index.ts`.
4. Re-export from `src/index.ts`.
5. Verify ELS web still builds: `npm run build --workspace=@els/web`.
