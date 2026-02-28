# AQ-Libraries Restructure & Shared React Component Library — Implementation Plan

## Overview

Two coordinated changes to AQ-Libraries:

1. **Restructure repository** — Move all .NET code into a `dotnet/` subfolder and add a `react/` subfolder for shared frontend components, making the repo a clear multi-tech monorepo.
2. **Shared React component library** — Move duplicated React components (DataGrid, AutoCompleteCombo, ThemeSwitcher/ThemeProvider) from DQM and ELS into `react/`, eliminating code duplication.

Both changes must be applied together since the directory restructure affects all ProjectReference paths in DQM and ELS.

---

## Part A: Current State

### Shared UI Components

| Component | DQM (`src/dqm-frontend/src/`) | ELS (`frontend/web/lib/`) | Status |
|---|---|---|---|
| DataGrid | `components/DataGrid.tsx` (833 lines) | `components/DataGrid.tsx` (837 lines) | Near-identical |
| ColumnFilter | `components/ColumnFilter.tsx` (555 lines) | `components/ColumnFilter.tsx` (544 lines) | Near-identical |
| AutoCompleteCombo | `components/AutoCompleteCombo.tsx` (152 lines) | `components/AutoCompleteCombo.tsx` (181 lines) | Near-identical |
| DataGrid types | `types/dataGrid.ts` (232 lines) | `components/DataGrid.types.ts` (211 lines) | Minor type differences |
| FilterExpressionBuilder | `types/dataGrid.ts` | `components/DataGrid.types.ts` | Identical |
| SortExpressionBuilder | `types/dataGrid.ts` | `components/DataGrid.types.ts` | Identical |
| ThemeSwitcher | — | `components/ThemeSwitcher.tsx` (62 lines) | ELS only, move to shared |
| ThemeProvider | — | `providers/ThemeProvider.tsx` (160 lines) | ELS only, move to shared |
| Theme registry | — | `lib/themes/index.ts` (60 lines) | ELS only, move to shared |

**Not shared** (project-specific):
- `useDataGridApi` hook — DQM only (coupled to DQM's axios apiClient)
- `useEntityCache` hook — DQM only (domain-specific entity queries)
- `useEntityNavigation` hook — DQM only (hardcoded DQM routes)
- Theme preset files (`zinc/theme.ts`, `blue/theme.ts`, CSS files) — project-specific visual identity

### Tech Stack Alignment

| | DQM | ELS |
|---|---|---|
| React | 19.1.1 | 19.2.3 |
| Mantine | 8.2.4 | 8.2.4 ✅ |
| Bundler | Vite 7 | Next.js 16 (Turbopack) |
| Module | ESNext (bundler) | ESNext (bundler) |
| Path aliases | None | `@/*` → `./*` |

Both projects use **Mantine v8** and **React 19** — compatible for sharing.

---

## Part B: Repository Restructure

### Current AQ-Libraries Layout

```
AQ-Libraries/
├── AQ.sln
├── Directory.Packages.props
├── LICENSE
├── src/
│   ├── Abstractions/           AQ.Abstractions.csproj
│   ├── CQRS/                   AQ.CQRS.csproj
│   ├── DataSeeding/            AQ.DataSeeding.csproj
│   ├── Entities/               AQ.Entities.csproj
│   ├── Events/
│   │   ├── Dispatchers/        AQ.Events.Dispatchers.csproj
│   │   ├── Domain/             AQ.Events.Domain.csproj
│   │   ├── Integration/        AQ.Events.Integration.csproj
│   │   └── Outbox/             AQ.Events.Outbox.csproj
│   ├── Extensions/             AQ.Extensions.csproj
│   ├── StateMachine/
│   │   ├── Entities/           AQ.StateMachine.Entities.csproj
│   │   └── Services/           AQ.StateMachine.Services.csproj
│   ├── Utilities/
│   │   ├── Filter/             AQ.Utilities.Filter.csproj
│   │   ├── Results/            AQ.Utilities.Results.csproj
│   │   ├── Search/             AQ.Utilities.Search.csproj
│   │   └── Sort/               AQ.Utilities.Sort.csproj
│   └── ValueObjects/           AQ.ValueObjects.csproj
└── tests/
    └── StateMachine/
        └── Services/           AQ.StateMachine.Services.Tests.csproj
```

### New AQ-Libraries Layout

```
AQ-Libraries/
├── LICENSE
├── .gitignore                    # Updated with node_modules/, dist/
│
├── dotnet/                       # ALL .NET code moves here
│   ├── AQ.sln                    # Moved from root
│   ├── Directory.Packages.props  # Moved from root
│   ├── src/
│   │   ├── Abstractions/
│   │   ├── CQRS/
│   │   ├── DataSeeding/
│   │   ├── Entities/
│   │   ├── Events/
│   │   │   ├── Dispatchers/
│   │   │   ├── Domain/
│   │   │   ├── Integration/
│   │   │   └── Outbox/
│   │   ├── Extensions/
│   │   ├── StateMachine/
│   │   │   ├── Entities/
│   │   │   └── Services/
│   │   ├── Utilities/
│   │   │   ├── Filter/
│   │   │   ├── Results/
│   │   │   ├── Search/
│   │   │   └── Sort/
│   │   └── ValueObjects/
│   └── tests/
│       └── StateMachine/
│           └── Services/
│
├── react/                        # NEW — shared React components
│   ├── package.json              # @AQ/react-components
│   ├── tsconfig.json
│   ├── src/
│   │   ├── index.ts              # Root barrel export
│   │   │
│   │   ├── mantine/              # Grouped by tech line
│   │   │   ├── index.ts          # Mantine barrel export
│   │   │   │
│   │   │   ├── data-grid/        # DataGrid component group
│   │   │   │   ├── index.ts
│   │   │   │   ├── DataGrid.tsx
│   │   │   │   ├── DataGrid.types.ts
│   │   │   │   └── ColumnFilter.tsx
│   │   │   │
│   │   │   ├── autocomplete/     # AutoCompleteCombo component group
│   │   │   │   ├── index.ts
│   │   │   │   └── AutoCompleteCombo.tsx
│   │   │   │
│   │   │   └── theme/            # Theme switching system
│   │   │       ├── index.ts
│   │   │       ├── ThemeSwitcher.tsx
│   │   │       ├── ThemeProvider.tsx
│   │   │       └── themes/
│   │   │           └── index.ts  # Theme registry (availableThemes, loadTheme, etc.)
│   │   │
│   │   └── utils/                # Framework-agnostic utilities
│   │       ├── index.ts
│   │       ├── FilterExpressionBuilder.ts
│   │       └── SortExpressionBuilder.ts
│   │
│   └── README.md
│
└── docs/
    └── SHARED_REACT_COMPONENTS_PLAN.md  # This file
```

### Design Principles

1. **Clear tech separation** — `dotnet/` for all .NET, `react/` for all frontend. No mixing.
2. **Group by tech line** — `mantine/data-grid/`, `mantine/autocomplete/`, `mantine/theme/`. Future non-Mantine components go in their own tech folder.
3. **Framework-agnostic** — No `'use client'` directives in the shared library. Consumers add wrappers if needed.
4. **DQM types as source of truth** — DQM uses stricter typing (`keyof T`, `unknown`). The shared library adopts the stricter signatures.
5. **Utilities separated** — `FilterExpressionBuilder` and `SortExpressionBuilder` are pure logic, placed in `utils/`.
6. **No bundling** — Distributed as raw TypeScript source. Both consumers use bundlers (Vite, Turbopack) that handle TS natively.
7. **Theme presets stay in consumers** — The shared library provides the theme infrastructure (provider, switcher, registry). Actual theme files (`zinc/theme.ts`, `blue/theme.ts`, CSS) stay in each project since they define project-specific visual identity.

---

## Part C: .NET Path Changes (Dotnet Restructure)

Moving `AQ.sln`, `Directory.Packages.props`, `src/`, and `tests/` into `dotnet/` requires updating all references in AQ-Libraries itself, plus all external references in ELS and DQM.

### C.1: AQ-Libraries Internal Changes

**Move files/folders:**
```
AQ.sln                    → dotnet/AQ.sln
Directory.Packages.props  → dotnet/Directory.Packages.props
src/                      → dotnet/src/
tests/                    → dotnet/tests/
```

**AQ.sln project paths** (no changes needed — paths are relative to .sln location, and the .sln moves with `src/` and `tests/`):

| Project | Path in .sln (unchanged) |
|---|---|
| AQ.Abstractions | `src\Abstractions\AQ.Abstractions.csproj` |
| AQ.CQRS | `src\CQRS\AQ.CQRS.csproj` |
| AQ.DataSeeding | `src\DataSeeding\AQ.DataSeeding.csproj` |
| AQ.Entities | `src\Entities\AQ.Entities.csproj` |
| AQ.Events.Dispatchers | `src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` |
| AQ.Events.Domain | `src\Events\Domain\AQ.Events.Domain.csproj` |
| AQ.Events.Integration | `src\Events\Integration\AQ.Events.Integration.csproj` |
| AQ.Events.Outbox | `src\Events\Outbox\AQ.Events.Outbox.csproj` |
| AQ.Extensions | `src\Extensions\AQ.Extensions.csproj` |
| AQ.StateMachine.Entities | `src\StateMachine\Entities\AQ.StateMachine.Entities.csproj` |
| AQ.StateMachine.Services | `src\StateMachine\Services\AQ.StateMachine.Services.csproj` |
| AQ.Utilities.Filter | `src\Utilities\Filter\AQ.Utilities.Filter.csproj` |
| AQ.Utilities.Results | `src\Utilities\Results\AQ.Utilities.Results.csproj` |
| AQ.Utilities.Search | `src\Utilities\Search\AQ.Utilities.Search.csproj` |
| AQ.Utilities.Sort | `src\Utilities\Sort\AQ.Utilities.Sort.csproj` |
| AQ.ValueObjects | `src\ValueObjects\AQ.ValueObjects.csproj` |
| AQ.StateMachine.Services.Tests | `tests\StateMachine\Services\AQ.StateMachine.Services.Tests.csproj` |

Since `AQ.sln` moves into `dotnet/` alongside `src/` and `tests/`, all internal relative paths stay the same. `Directory.Packages.props` also moves into `dotnet/` so it remains discoverable by MSBuild.

### C.2: ELS .sln Path Changes

**File**: `ELS/backend/ELS.sln`  
**Pattern**: `..\..\AQ-Libraries\src\` → `..\..\AQ-Libraries\dotnet\src\`

| Line | Current Path | New Path |
|------|-------------|----------|
| L16 | `..\..\AQ-Libraries\src\Abstractions\AQ.Abstractions.csproj` | `..\..\AQ-Libraries\dotnet\src\Abstractions\AQ.Abstractions.csproj` |
| L18 | `..\..\AQ-Libraries\src\Extensions\AQ.Extensions.csproj` | `..\..\AQ-Libraries\dotnet\src\Extensions\AQ.Extensions.csproj` |
| L20 | `..\..\AQ-Libraries\src\Entities\AQ.Entities.csproj` | `..\..\AQ-Libraries\dotnet\src\Entities\AQ.Entities.csproj` |
| L22 | `..\..\AQ-Libraries\src\CQRS\AQ.CQRS.csproj` | `..\..\AQ-Libraries\dotnet\src\CQRS\AQ.CQRS.csproj` |
| L26 | `..\..\AQ-Libraries\src\Events\Domain\AQ.Events.Domain.csproj` | `..\..\AQ-Libraries\dotnet\src\Events\Domain\AQ.Events.Domain.csproj` |
| L28 | `..\..\AQ-Libraries\src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` | `..\..\AQ-Libraries\dotnet\src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` |
| L30 | `..\..\AQ-Libraries\src\Events\Outbox\AQ.Events.Outbox.csproj` | `..\..\AQ-Libraries\dotnet\src\Events\Outbox\AQ.Events.Outbox.csproj` |
| L32 | `..\..\AQ-Libraries\src\Utilities\Filter\AQ.Utilities.Filter.csproj` | `..\..\AQ-Libraries\dotnet\src\Utilities\Filter\AQ.Utilities.Filter.csproj` |
| L36 | `..\..\AQ-Libraries\src\Utilities\Results\AQ.Utilities.Results.csproj` | `..\..\AQ-Libraries\dotnet\src\Utilities\Results\AQ.Utilities.Results.csproj` |
| L38 | `..\..\AQ-Libraries\src\Utilities\Search\AQ.Utilities.Search.csproj` | `..\..\AQ-Libraries\dotnet\src\Utilities\Search\AQ.Utilities.Search.csproj` |
| L40 | `..\..\AQ-Libraries\src\Utilities\Sort\AQ.Utilities.Sort.csproj` | `..\..\AQ-Libraries\dotnet\src\Utilities\Sort\AQ.Utilities.Sort.csproj` |
| L42 | `..\..\AQ-Libraries\src\ValueObjects\AQ.ValueObjects.csproj` | `..\..\AQ-Libraries\dotnet\src\ValueObjects\AQ.ValueObjects.csproj` |

**Total: 12 path changes** (find/replace `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\`)

### C.3: ELS .csproj Path Changes

All ELS .csproj files are in `backend/src/Core/*/` (5 levels up to reach `AQ-Libraries/`).  
**Pattern**: `..\..\..\..\..\AQ-Libraries\src\` → `..\..\..\..\..\AQ-Libraries\dotnet\src\`

**`ELS.Core.Domain.csproj`** (`backend/src/Core/Domain/`) — 6 references:

| Line | Current | New |
|------|---------|-----|
| L7 | `..\..\..\..\..\AQ-Libraries\src\Abstractions\AQ.Abstractions.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Abstractions\AQ.Abstractions.csproj` |
| L8 | `..\..\..\..\..\AQ-Libraries\src\Entities\AQ.Entities.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Entities\AQ.Entities.csproj` |
| L9 | `..\..\..\..\..\AQ-Libraries\src\Events\Domain\AQ.Events.Domain.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Events\Domain\AQ.Events.Domain.csproj` |
| L10 | `..\..\..\..\..\AQ-Libraries\src\Utilities\Search\AQ.Utilities.Search.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Search\AQ.Utilities.Search.csproj` |
| L11 | `..\..\..\..\..\AQ-Libraries\src\Utilities\Results\AQ.Utilities.Results.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Results\AQ.Utilities.Results.csproj` |
| L12 | `..\..\..\..\..\AQ-Libraries\src\ValueObjects\AQ.ValueObjects.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\ValueObjects\AQ.ValueObjects.csproj` |

**`ELS.Core.Infrastructure.csproj`** (`backend/src/Core/Infrastructure/`) — 3 references:

| Line | Current | New |
|------|---------|-----|
| L24 | `..\..\..\..\..\AQ-Libraries\src\DataSeeding\AQ.DataSeeding.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\DataSeeding\AQ.DataSeeding.csproj` |
| L25 | `..\..\..\..\..\AQ-Libraries\src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` |
| L26 | `..\..\..\..\..\AQ-Libraries\src\Events\Outbox\AQ.Events.Outbox.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Events\Outbox\AQ.Events.Outbox.csproj` |

**`ELS.Core.Api.csproj`** (`backend/src/Core/Api/`) — 5 references:

| Line | Current | New |
|------|---------|-----|
| L31 | `..\..\..\..\..\AQ-Libraries\src\Utilities\Search\AQ.Utilities.Search.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Search\AQ.Utilities.Search.csproj` |
| L32 | `..\..\..\..\..\AQ-Libraries\src\Utilities\Filter\AQ.Utilities.Filter.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Filter\AQ.Utilities.Filter.csproj` |
| L33 | `..\..\..\..\..\AQ-Libraries\src\Utilities\Sort\AQ.Utilities.Sort.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Sort\AQ.Utilities.Sort.csproj` |
| L34 | `..\..\..\..\..\AQ-Libraries\src\Utilities\Results\AQ.Utilities.Results.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Results\AQ.Utilities.Results.csproj` |
| L35 | `..\..\..\..\..\AQ-Libraries\src\Events\Domain\AQ.Events.Domain.csproj` | `..\..\..\..\..\AQ-Libraries\dotnet\src\Events\Domain\AQ.Events.Domain.csproj` |

**ELS .csproj total: 14 path changes** across 3 files (find/replace `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\`)

### C.4: DQM .sln Path Changes

**File**: `DQM/DQM.sln`  
**Pattern**: `..\AQ-Libraries\src\` → `..\AQ-Libraries\dotnet\src\`

| Line | Current Path | New Path |
|------|-------------|----------|
| L14 | `..\AQ-Libraries\src\Entities\AQ.Entities.csproj` | `..\AQ-Libraries\dotnet\src\Entities\AQ.Entities.csproj` |
| L18 | `..\AQ-Libraries\src\StateMachine\Entities\AQ.StateMachine.Entities.csproj` | `..\AQ-Libraries\dotnet\src\StateMachine\Entities\AQ.StateMachine.Entities.csproj` |
| L20 | `..\AQ-Libraries\src\Abstractions\AQ.Abstractions.csproj` | `..\AQ-Libraries\dotnet\src\Abstractions\AQ.Abstractions.csproj` |
| L22 | `..\AQ-Libraries\src\ValueObjects\AQ.ValueObjects.csproj` | `..\AQ-Libraries\dotnet\src\ValueObjects\AQ.ValueObjects.csproj` |
| L26 | `..\AQ-Libraries\src\Utilities\Search\AQ.Utilities.Search.csproj` | `..\AQ-Libraries\dotnet\src\Utilities\Search\AQ.Utilities.Search.csproj` |
| L28 | `..\AQ-Libraries\src\Utilities\Results\AQ.Utilities.Results.csproj` | `..\AQ-Libraries\dotnet\src\Utilities\Results\AQ.Utilities.Results.csproj` |
| L30 | `..\AQ-Libraries\src\Utilities\Filter\AQ.Utilities.Filter.csproj` | `..\AQ-Libraries\dotnet\src\Utilities\Filter\AQ.Utilities.Filter.csproj` |
| L32 | `..\AQ-Libraries\src\Utilities\Sort\AQ.Utilities.Sort.csproj` | `..\AQ-Libraries\dotnet\src\Utilities\Sort\AQ.Utilities.Sort.csproj` |
| L34 | `..\AQ-Libraries\src\StateMachine\Services\AQ.StateMachine.Services.csproj` | `..\AQ-Libraries\dotnet\src\StateMachine\Services\AQ.StateMachine.Services.csproj` |
| L40 | `..\AQ-Libraries\src\Events\Domain\AQ.Events.Domain.csproj` | `..\AQ-Libraries\dotnet\src\Events\Domain\AQ.Events.Domain.csproj` |
| L42 | `..\AQ-Libraries\src\Events\Integration\AQ.Events.Integration.csproj` | `..\AQ-Libraries\dotnet\src\Events\Integration\AQ.Events.Integration.csproj` |
| L44 | `..\AQ-Libraries\src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` | `..\AQ-Libraries\dotnet\src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` |
| L46 | `..\AQ-Libraries\src\Extensions\AQ.Extensions.csproj` | `..\AQ-Libraries\dotnet\src\Extensions\AQ.Extensions.csproj` |
| L48 | `..\AQ-Libraries\src\DataSeeding\AQ.DataSeeding.csproj` | `..\AQ-Libraries\dotnet\src\DataSeeding\AQ.DataSeeding.csproj` |
| L52 | `..\AQ-Libraries\src\Events\Outbox\AQ.Events.Outbox.csproj` | `..\AQ-Libraries\dotnet\src\Events\Outbox\AQ.Events.Outbox.csproj` |

**Total: 15 path changes** (find/replace `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\`)

### C.5: DQM .csproj Path Changes

**`DQM.Core.Domain.csproj`** (`src/Core/Domain/`) — 5 references:  
**Pattern**: `..\..\..\..\AQ-Libraries\src\` → `..\..\..\..\AQ-Libraries\dotnet\src\`

| Line | Current | New |
|------|---------|-----|
| L11 | `..\..\..\..\AQ-Libraries\src\Abstractions\AQ.Abstractions.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Abstractions\AQ.Abstractions.csproj` |
| L12 | `..\..\..\..\AQ-Libraries\src\Entities\AQ.Entities.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Entities\AQ.Entities.csproj` |
| L13 | `..\..\..\..\AQ-Libraries\src\StateMachine\Entities\AQ.StateMachine.Entities.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\StateMachine\Entities\AQ.StateMachine.Entities.csproj` |
| L14 | `..\..\..\..\AQ-Libraries\src\Utilities\Search\AQ.Utilities.Search.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Search\AQ.Utilities.Search.csproj` |
| L15 | `..\..\..\..\AQ-Libraries\src\ValueObjects\AQ.ValueObjects.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\ValueObjects\AQ.ValueObjects.csproj` |

**`DQM.Core.Infrastructure.csproj`** (`src/Core/Infrastructure/`) — 5 references:  
**Pattern**: `..\..\..\..\AQ-Libraries\src\` → `..\..\..\..\AQ-Libraries\dotnet\src\`

| Line | Current | New |
|------|---------|-----|
| L31 | `..\..\..\..\AQ-Libraries\src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Events\Dispatchers\AQ.Events.Dispatchers.csproj` |
| L32 | `..\..\..\..\AQ-Libraries\src\Events\Outbox\AQ.Events.Outbox.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Events\Outbox\AQ.Events.Outbox.csproj` |
| L33 | `..\..\..\..\AQ-Libraries\src\Utilities\Results\AQ.Utilities.Results.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Results\AQ.Utilities.Results.csproj` |
| L34 | `..\..\..\..\AQ-Libraries\src\StateMachine\Services\AQ.StateMachine.Services.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\StateMachine\Services\AQ.StateMachine.Services.csproj` |
| L35 | `..\..\..\..\AQ-Libraries\src\DataSeeding\AQ.DataSeeding.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\DataSeeding\AQ.DataSeeding.csproj` |

**`DQM.Core.Api.csproj`** (`src/Core/Api/`) — 5 references:  
**Pattern**: `..\..\..\..\AQ-Libraries\src\` → `..\..\..\..\AQ-Libraries\dotnet\src\`

| Line | Current | New |
|------|---------|-----|
| L37 | `..\..\..\..\AQ-Libraries\src\Extensions\AQ.Extensions.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Extensions\AQ.Extensions.csproj` |
| L38 | `..\..\..\..\AQ-Libraries\src\Utilities\Results\AQ.Utilities.Results.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Results\AQ.Utilities.Results.csproj` |
| L39 | `..\..\..\..\AQ-Libraries\src\Utilities\Filter\AQ.Utilities.Filter.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Filter\AQ.Utilities.Filter.csproj` |
| L40 | `..\..\..\..\AQ-Libraries\src\Utilities\Sort\AQ.Utilities.Sort.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\Utilities\Sort\AQ.Utilities.Sort.csproj` |
| L41 | `..\..\..\..\AQ-Libraries\src\StateMachine\Services\AQ.StateMachine.Services.csproj` | `..\..\..\..\AQ-Libraries\dotnet\src\StateMachine\Services\AQ.StateMachine.Services.csproj` |

**`DQM.Migration.csproj`** (`src/Migration/`) — 1 reference:  
**Pattern**: `..\..\..\AQ-Libraries\src\` → `..\..\..\AQ-Libraries\dotnet\src\`

| Line | Current | New |
|------|---------|-----|
| L17 | `..\..\..\AQ-Libraries\src\DataSeeding\AQ.DataSeeding.csproj` | `..\..\..\AQ-Libraries\dotnet\src\DataSeeding\AQ.DataSeeding.csproj` |

**DQM .csproj total: 16 path changes** across 4 files (find/replace `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\`)

### C.6: Path Changes Summary

| Scope | Files Affected | References Changed | Find/Replace Pattern |
|---|---|---|---|
| ELS .sln | 1 | 12 | `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\` |
| ELS .csproj | 3 | 14 | `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\` |
| DQM .sln | 1 | 15 | `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\` |
| DQM .csproj | 4 | 16 | `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\` |
| AQ-Libraries internal | 0 | 0 | No changes (relative paths preserved) |
| **Total** | **9 files** | **57 references** | Single find/replace per file |

---

## Part D: Shared React Components

### D.1: ThemeSwitcher / ThemeProvider Architecture

The theme system has three layers. The shared library provides the infrastructure; consuming projects supply the actual theme presets.

**Shared library provides** (`react/src/mantine/theme/`):
- `ThemeProvider.tsx` — React context provider wrapping `MantineProvider` + `ModalsProvider`. Manages `themeName`, `colorScheme`, `toggleColorScheme`. Persists to localStorage. Dynamically loads themes via the registry.
- `ThemeSwitcher.tsx` — UI component with color scheme toggle (sun/moon icon) and theme preset dropdown menu. Uses `useTheme()` from ThemeProvider context.
- `themes/index.ts` — Theme registry exporting `availableThemes`, `ThemeName` type, `ThemeModule` interface, `loadTheme()`, `preloadTheme()`, `getThemeDisplayName()`.

**Consumers provide** (stays in each project):
- Theme preset files (e.g., `zinc/theme.ts`, `zinc/cssVariableResolver.ts`, `zinc/styles.css`)
- `availableThemes` configuration (which theme presets are available)
- CSS imports for theme styles

**ThemeModule interface** (what each theme preset exports):
```typescript
interface ThemeModule {
  theme: MantineThemeOverride;
  cssVariableResolver: CSSVariablesResolver;
}
```

**How consumers configure it:**
```tsx
// Consumer's app layout
import { ThemeProvider } from '@AQ/react-components/mantine/theme';

// Consumer passes their theme loader and available themes
<ThemeProvider
  availableThemes={['zinc', 'blue']}
  loadTheme={(name) => import(`./themes/${name}/theme`)}
  defaultTheme="zinc"
>
  <App />
</ThemeProvider>
```

This way the ThemeProvider/ThemeSwitcher are reusable, but each project defines its own visual themes.

### D.2: Type Reconciliation

Differences between DQM and ELS resolved to a single canonical type:

#### DataGridColumn

| Property | DQM | ELS | Shared (chosen) |
|---|---|---|---|
| `dataIndex` | `keyof T` | `string` | `keyof T & string` — stricter, catches typos |
| `icon` | required `ReactNode` | optional `ReactNode?` | **optional** `ReactNode?` — more flexible |

#### ActionButton

| Property | DQM | ELS | Shared (chosen) |
|---|---|---|---|
| `icon` | `ReactNode` (required) | `ReactNode?` (optional) | `ReactNode?` — optional is safer |
| `variant` values | `'filled' \| 'outline' \| 'light' \| 'subtle'` | + `'default'` | Include `'default'` — valid Mantine variant |

#### FilterCondition

| Property | DQM | ELS | Shared (chosen) |
|---|---|---|---|
| `value` | `unknown` | `string` | `unknown` — supports numbers/dates without casting |
| `secondValue` | `unknown?` | `string?` | `unknown?` — consistent with value |
| `operator` | `FilterOperator` type | inline union | `FilterOperator` type reference |

#### PaginationConfig

| Property | DQM | ELS | Shared (chosen) |
|---|---|---|---|
| Name | `PaginationInfo` | `PaginationConfig` | `PaginationConfig` — clearer intent |
| Shape | identical | identical | No change needed |

#### DataGridProps

| Property | DQM | ELS | Shared (chosen) |
|---|---|---|---|
| `data` | `T[]` (required) | `T[]?` (optional) | `T[]` — required, use `[]` default at call site |
| `columns` | `DataGridColumn<T>[]` (required) | `DataGridColumn<T>[]?` (optional) | `DataGridColumn<T>[]` — required |
| `gridId` | `string?` | absent | Include — useful for persistence |
| `rowKey` | `keyof T \| ((record: T) => string)` | `string \| ((record: T) => string)` | `keyof T & string \| ((record: T) => string)` |
| `filterConfig` | `FilterConfig?` | absent | Include — DQM-only feature, optional prop |
| `onFilterChange` operator param | `LogicalOperator?` | `'and' \| 'or'` | `LogicalOperator` |

#### SpecialModeConfig

| Property | DQM | ELS | Shared (chosen) |
|---|---|---|---|
| `filter` | `{ enabled?: boolean }` | absent | Include — optional, non-breaking |
| button configs | inline `{ visible?; disabled? }` | extracted `SpecialModeButtonConfig` | Use extracted interface — cleaner |

#### Additional DQM-only types to include

| Type | Reason |
|---|---|
| `GridMode` | Cleaner named type alias for `'view' \| 'action' \| 'special'` |
| `FilterConfig` | Optional advanced filter panel support |
| `FormConfig` | Optional form panel support (used by some DQM grids) |

### D.3: Bug Fixes to Include During Migration

These bugs exist in both codebases and should be fixed in the shared version:

#### 1. `onSearch` fires on mount (Medium)
**File**: DataGrid.tsx ~line 90  
**Issue**: `useEffect` watching debounced search value calls `onSearch('')` on initial render, causing a duplicate API call.  
**Fix**: Add a `isInitialMount` ref guard to skip the first invocation.

#### 2. `selectedRows` prop not synced after mount (Medium)
**File**: DataGrid.tsx ~line 80  
**Issue**: External `selectedRows` prop changes are ignored after initial state set. The component uses internal state only.  
**Fix**: Add `useEffect` to sync `state.selectedRows` with the `selectedRows` prop when it changes externally.

#### 3. `parseFilterExpression` double-decodes `%2C` for between (Low)
**File**: ColumnFilter.tsx  
**Issue**: Comma values are decoded twice — once explicitly, once by the split logic.  
**Fix**: Remove redundant decode step.

#### 4. Pagination shows "1 to 0" when total is 0 (Low)
**File**: DataGrid.tsx  
**Issue**: When `pagination.total === 0`, the "Showing X to Y of Z" text calculates `1 to 0 of 0`.  
**Fix**: Show "No records" instead when total is 0.

---

## Part E: Implementation Checklist

### Phase 1: Restructure AQ-Libraries (.NET)

- [ ] Create `dotnet/` directory in AQ-Libraries root
- [ ] Move `AQ.sln` → `dotnet/AQ.sln`
- [ ] Move `Directory.Packages.props` → `dotnet/Directory.Packages.props`
- [ ] Move `src/` → `dotnet/src/`
- [ ] Move `tests/` → `dotnet/tests/`
- [ ] Verify `dotnet build` works from `dotnet/` folder
- [ ] Update `.gitignore` at root (add `node_modules/`, `dist/`)

### Phase 2: Update .NET References in ELS

- [ ] Update `ELS/backend/ELS.sln` — find/replace `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\` (12 refs)
- [ ] Update `ELS.Core.Domain.csproj` — find/replace `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\` (6 refs)
- [ ] Update `ELS.Core.Infrastructure.csproj` — find/replace (3 refs)
- [ ] Update `ELS.Core.Api.csproj` — find/replace (5 refs)
- [ ] Verify `dotnet build` in ELS backend

### Phase 3: Update .NET References in DQM

- [ ] Update `DQM/DQM.sln` — find/replace `AQ-Libraries\src\` → `AQ-Libraries\dotnet\src\` (15 refs)
- [ ] Update `DQM.Core.Domain.csproj` — find/replace (5 refs)
- [ ] Update `DQM.Core.Infrastructure.csproj` — find/replace (5 refs)
- [ ] Update `DQM.Core.Api.csproj` — find/replace (5 refs)
- [ ] Update `DQM.Migration.csproj` — find/replace (1 ref)
- [ ] Verify `dotnet build` in DQM

### Phase 4: Scaffold React Package

- [ ] Create `react/` directory in AQ-Libraries root
- [ ] Create `react/package.json` with name `@AQ/react-components`
  - **peerDependencies**: `react >=19`, `@mantine/core >=8`, `@mantine/hooks >=8`, `@mantine/dates >=8`, `@mantine/modals >=8`, `@tabler/icons-react >=3`
  - **No bundled dependencies** — consumers provide these
  - `"main": "src/index.ts"` — raw source, no build step
- [ ] Create `react/tsconfig.json` — strict mode, `jsx: react-jsx`, `moduleResolution: bundler`, `noEmit: true`

### Phase 5: Create Shared Components

- [ ] Create `react/src/mantine/data-grid/DataGrid.types.ts`
  - Consolidate types from DQM's `types/dataGrid.ts` (source of truth) with ELS additions
  - Use `keyof T & string` for `dataIndex` and `rowKey`
  - Use `unknown` for `FilterCondition.value`
  - Include `GridMode`, `FilterConfig`, `FormConfig`, `SpecialModeButtonConfig`
  - Export `PaginationConfig` (not `PaginationInfo`)
- [ ] Create `react/src/mantine/data-grid/DataGrid.tsx`
  - Use DQM version as base (no `'use client'` directive)
  - Apply bug fixes #1 (onSearch mount guard), #2 (selectedRows sync), #4 (pagination zero text)
  - Import types from local `./DataGrid.types`
- [ ] Create `react/src/mantine/data-grid/ColumnFilter.tsx`
  - Use DQM version as base
  - Apply bug fix #3 (double-decode)
  - Import `DataGridColumn` from `./DataGrid.types`
- [ ] Create `react/src/mantine/data-grid/index.ts` barrel export
- [ ] Create `react/src/mantine/autocomplete/AutoCompleteCombo.tsx`
  - Merge DQM + ELS versions (nearly identical)
  - No `'use client'` directive
- [ ] Create `react/src/mantine/autocomplete/index.ts` barrel export
- [ ] Create `react/src/mantine/theme/ThemeProvider.tsx`
  - Adapt from ELS `providers/ThemeProvider.tsx`
  - Accept `availableThemes`, `loadTheme`, `defaultTheme` as props (not hardcoded)
  - Wraps `MantineProvider` + `ModalsProvider`
  - Persists `themeName` and `colorScheme` to localStorage
- [ ] Create `react/src/mantine/theme/ThemeSwitcher.tsx`
  - Adapt from ELS `components/ThemeSwitcher.tsx`
  - Uses `useTheme()` context from ThemeProvider
- [ ] Create `react/src/mantine/theme/themes/index.ts`
  - Export `ThemeModule` interface, `ThemeName` type, helper functions
  - Theme registry logic (loadTheme, preloadTheme, getThemeDisplayName)
- [ ] Create `react/src/mantine/theme/index.ts` barrel export
- [ ] Create `react/src/utils/FilterExpressionBuilder.ts` (extracted from DataGrid.types)
- [ ] Create `react/src/utils/SortExpressionBuilder.ts` (extracted from DataGrid.types)
- [ ] Create `react/src/utils/index.ts` barrel export
- [ ] Create `react/src/mantine/index.ts` barrel export
- [ ] Create `react/src/index.ts` root barrel export

### Phase 6: Integrate into DQM

- [ ] Add path alias in `tsconfig.app.json`: `"@AQ/react-components": ["../../AQ-Libraries/react/src"]`
  - Or alternatively in `vite.config.ts` `resolve.alias`
- [ ] Update `src/components/DataGrid.tsx` → replace with re-export from `@AQ/react-components`
- [ ] Update `src/components/ColumnFilter.tsx` → remove (imported transitively via DataGrid)
- [ ] Update `src/components/AutoCompleteCombo.tsx` → replace with re-export from `@AQ/react-components`
- [ ] Update `src/types/dataGrid.ts` → remove, import types from `@AQ/react-components`
- [ ] Update all import paths across DQM pages/components
- [ ] Verify build: `cd src/dqm-frontend && npm run build`
- [ ] Manual smoke test

### Phase 7: Integrate into ELS

- [ ] Add path alias in `tsconfig.json` `paths`: `"@AQ/react-components": ["../../AQ-Libraries/react/src"]`
  - Also add to `next.config.ts` `webpack.resolve.alias` if needed
- [ ] Create thin wrapper `lib/components/DataGrid.tsx`:
  ```tsx
  'use client';
  export { DataGrid } from '@AQ/react-components/mantine/data-grid';
  ```
- [ ] Create thin wrapper `lib/components/AutoCompleteCombo.tsx`:
  ```tsx
  'use client';
  export { AutoCompleteCombo } from '@AQ/react-components/mantine/autocomplete';
  ```
- [ ] Create thin wrapper `lib/components/ThemeSwitcher.tsx`:
  ```tsx
  'use client';
  export { ThemeSwitcher } from '@AQ/react-components/mantine/theme';
  ```
- [ ] Update `lib/components/index.ts` to re-export from shared package + thin wrappers
- [ ] Replace `lib/providers/ThemeProvider.tsx` with import from `@AQ/react-components/mantine/theme`
- [ ] Keep theme preset files (`lib/themes/zinc/`, `lib/themes/blue/`) in ELS (project-specific)
- [ ] Remove old `lib/components/DataGrid.types.ts`, `lib/components/ColumnFilter.tsx`
- [ ] Update all import paths across ELS pages
- [ ] Verify build: `cd frontend/web && npx next build`
- [ ] Manual smoke test

### Phase 8: Verify & Cleanup

- [ ] Run `dotnet build` in AQ-Libraries (`dotnet/`)
- [ ] Run `dotnet build` in ELS (`backend/`)
- [ ] Run `dotnet build` in DQM
- [ ] Run `npm run build` in DQM frontend
- [ ] Run `npx next build` in ELS frontend
- [ ] Verify DataGrid renders with data, sorting, filtering, pagination in both apps
- [ ] Verify AutoCompleteCombo renders with search/select in both apps
- [ ] Verify ThemeSwitcher toggles color scheme and switches theme presets in ELS
- [ ] Commit AQ-Libraries changes separately from consuming project changes
- [ ] Delete old duplicated files from DQM and ELS after confirming everything works

---

## Part F: Consumer Import Examples

### Importing components

```tsx
// From root barrel
import { DataGrid, AutoCompleteCombo, ThemeSwitcher } from '@AQ/react-components';

// From tech-line barrel (more explicit)
import { DataGrid } from '@AQ/react-components/mantine/data-grid';
import { AutoCompleteCombo } from '@AQ/react-components/mantine/autocomplete';
import { ThemeProvider, ThemeSwitcher, useTheme } from '@AQ/react-components/mantine/theme';

// Types
import type { DataGridColumn, DataGridProps, FilterCondition } from '@AQ/react-components';
import type { ThemeModule, ThemeName } from '@AQ/react-components/mantine/theme';

// Utilities
import { FilterExpressionBuilder, SortExpressionBuilder } from '@AQ/react-components/utils';
```

### ELS wrapper pattern (Next.js `'use client'`)

```tsx
// lib/components/DataGrid.tsx
'use client';
export { DataGrid } from '@AQ/react-components/mantine/data-grid';
export type { DataGridColumn, DataGridProps, FilterCondition, PaginationConfig } from '@AQ/react-components/mantine/data-grid';
```

### ThemeProvider usage (consumer)

```tsx
// ELS: app/layout.tsx
import { ThemeProvider } from '@AQ/react-components/mantine/theme';

export default function RootLayout({ children }) {
  return (
    <ThemeProvider
      availableThemes={['zinc', 'blue']}
      loadTheme={(name) => import(`../lib/themes/${name}/theme`)}
      defaultTheme="zinc"
    >
      {children}
    </ThemeProvider>
  );
}
```

### Path alias configuration

**DQM** — `vite.config.ts`:
```ts
resolve: {
  alias: {
    '@AQ/react-components': path.resolve(__dirname, '../../AQ-Libraries/react/src'),
  },
}
```

**ELS** — `tsconfig.json`:
```json
{
  "compilerOptions": {
    "paths": {
      "@AQ/react-components": ["../../AQ-Libraries/react/src"],
      "@AQ/react-components/*": ["../../AQ-Libraries/react/src/*"]
    }
  }
}
```

---

## Out of Scope

| Item | Reason |
|---|---|
| `useDataGridApi` hook | DQM-specific (coupled to DQM axios apiClient) |
| `useEntityCache` hook | DQM-specific (domain entity queries) |
| `useEntityNavigation` hook | DQM-specific (hardcoded DQM routes) |
| Theme preset files (`theme.ts`, `styles.css`, `cssVariableResolver.ts`) | Project-specific visual identity — stay in each consumer |
| npm publishing | Not needed — sibling repos use relative path aliases |
| Build/bundle step | Not needed — consumed as raw TypeScript by bundlers |
| Storybook / component docs | Can be added later; not required for initial migration |
