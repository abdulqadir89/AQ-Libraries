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
    locale/                       # AQLocaleProvider, useAQLocale, AQMessages, defaultMessages (English)
  utils/
    index.ts
    FilterExpressionBuilder.ts    # Builds FilterExpression strings for API queries
    SortExpressionBuilder.ts      # Builds SortExpression strings for API queries
    DateTimeOffsetUtils.ts        # DateTimeOffset parsing/formatting helpers (locale-aware; see Localization below)
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

## Localization (`AQLocaleProvider`)

Components with user-visible strings (`DataGrid`/`CardDataGrid`, `ColumnFilter`, `RichTextEditor`,
`DateTimeOffsetRangeInput`, `AddressInput`, `AttachmentList`/`AttachmentPanel`/`AttachmentUpload`,
`StagedAttachmentPicker`, `ThemeSwitcher`, `MasterDetail`, `SplitButton`, `AutoCompleteCombo`,
`DateTimeOffsetDisplay`) pull their strings from `useAQLocale()` (`src/mantine/locale/`) rather than
hard-coding English. `AQLocaleProvider({ locale?, messages?, children })` deep-merges `messages` over
`defaultMessages` (the built-in English strings) and makes both available via `useAQLocale()`, which
works fine with **no** provider mounted (returns `defaultMessages`, `locale: undefined`) — so a
consumer that doesn't opt in sees unchanged English behavior.

- Message shape is a typed `AQMessages` object grouped by component (`dataGrid`, `columnFilter`,
  `richTextEditor`, `dateTimeRange`, `address`, `attachments`, `themeSwitcher`, `masterDetail`,
  `splitButton`, `autocomplete`, `cardDataGrid`). Strings with counts/values are **functions**, not
  ICU templates — e.g. `rowsSelected: (n: number) => string` — because this library doesn't depend
  on next-intl/ICU; the consuming app's own i18n layer supplies the function bodies.
- A component prop that already sets a label always wins over the context message for that string.
- `DateTimeOffsetUtils.ts` exported formatters take an optional trailing `locale?: string` (passed
  through to `Intl.DateTimeFormat`); `MoneyDisplay` and `AddressInput`'s country list
  (`mantine/address/data.ts`, `getCountryName`) do the same via `useAQLocale().locale`. The
  `'en-CA'` literals at `DateTimeOffsetUtils.ts:214/237/242` are internal ISO-date parsing helpers,
  not display strings — leave them as-is.
- ELS web supplies actual translated strings by wiring `AQLocaleProvider` into its own
  `providers/LocaleProvider.tsx` with `buildAqMessages(t)` — see `frontend/web`'s
  `docs/architecture/frontend.md` "Internationalization" section for that side of the wiring.
