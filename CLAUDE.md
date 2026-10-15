# AQ-Libraries

Monorepo of reusable DDD/utility libraries for .NET, plus a React component library built on Mantine. Apps (DQM, ELS) consume the libraries by project reference or path alias; nothing here may know about an app.

Sub-folder CLAUDE.md files contain layer-specific rules:

| Layer | Location | CLAUDE.md |
|-------|----------|-----------|
| .NET libraries | `dotnet/` | [dotnet/CLAUDE.md](dotnet/CLAUDE.md) |
| React components | `react/` | [react/CLAUDE.md](react/CLAUDE.md) |

## Repository Overview

- **dotnet/** — C# library suite (actors, entities, events, EF Core save pipeline, CQRS, state machine, utilities, identity)
  - `src/` — Library implementations
  - `tests/` — Unit tests mirroring source structure
  - `AQ.sln` — Solution file
- **react/** — TypeScript React component library (`@AQ/react-components`)
  - Built on Mantine v8+ with TypeScript
  - Source-only package (no build step); consumed via tsconfig alias in the consuming web app
  - Requires React 19+, Mantine 8+, TanStack Query v5+

What each library does and guarantees is in [docs/LIBRARIES.md](docs/LIBRARIES.md).

**Any change to a library under `dotnet/src` or `react/src` updates `docs/LIBRARIES.md` in the same piece of work** (rule text, a new rule, or a removed rule with its `// RULE` tag), then run `pwsh ./scripts/docs-sync.ps1 -Check`. Details under Documentation below.

## Quick Commands

### .NET (run from `dotnet/` directory)

```bash
dotnet restore AQ.sln
dotnet format AQ.sln                  # required by CI, will fix most violations
dotnet build AQ.sln
dotnet test AQ.sln
dotnet test AQ.sln --filter "FullyQualifiedName~MyNamespace.MyClass.MyMethod"
dotnet pack AQ.sln --configuration Release
```

### React (run from `react/` directory)

The React library has no build or test scripts. It is a source-only package with Mantine components as peer dependencies.

### Docs check (run from the repository root)

```powershell
pwsh ./scripts/docs-sync.ps1 -Check   # fails when docs/LIBRARIES.md and the code disagree
pwsh ./scripts/docs-sync.ps1 -Write   # regenerates the package graph block, then checks
```

## Documentation

The code is the source of truth. Docs describe what the code does today; they never carry plans.

| Question | Where |
|----------|-------|
| What does each library do and promise? Rules, invariants, who depends on whom? | [docs/LIBRARIES.md](docs/LIBRARIES.md) — one file, read it first |
| Why was it built this way / what was tried and dropped? | [docs/decisions/](docs/decisions/) — ADRs |
| How do I work in this layer (patterns, conventions, gotchas)? | the layer's `CLAUDE.md` (`dotnet/`, `react/`) |
| How do I use a library (syntax, examples)? | the `README.md` next to the library (Filter, Sort, Search, Results, StateMachine, DataSeeding, Identity) |

### Keeping LIBRARIES.md true
- **Behaviour change in a library ⇒ update LIBRARIES.md in the same piece of work**: the rule text, or a new rule. Then run `pwsh ./scripts/docs-sync.ps1 -Check`.
- The block between `<!-- BEGIN:surface -->` and `<!-- END:surface -->` is generated from the project files and `react/package.json` — never hand-edit it; run `-Write`.
- Every library section heading carries a `<!-- projects: ... -->` marker naming the projects it describes. A new project must be claimed by one.
- **Rule ids**: every guarantee in LIBRARIES.md has an id (`**ACT-3**`: three capital letters, a dash, a number, unique per file). Put `// RULE ACT-3` on the line that enforces it, in any language in the repo. Deleting a rule in code means deleting it from the doc; the script flags both directions.
- Write rules as what a caller can rely on, not as a tour of the implementation. One rule, one behaviour.
- **Planning lives in code only**: a `TODO` comment next to the code it concerns. No roadmap, status or "planned" text in docs.
- A change that breaks a consumer (DQM, ELS) changes the rule, and the consuming repo's `CLAUDE.md` or docs say what it must do — note it in the PR.

### Decisions (ADRs)
When you change, replace or revert an existing implementation — or the user rejects an approach you proposed — ask the user why, or infer the reason from their instruction **and confirm it with them**. Then:
1. Write `docs/decisions/NNN-short-title.md` (next number): Context, Options considered, Decision, Why, Consequences, Status (`Accepted` / `Superseded by NNN` / `Rejected`).
2. Leave a one-line comment at the code site: `// ADR-NNN: why X, not Y`.
3. `docs-sync.ps1` checks every ADR reference resolves to a file.

### Policy
- DO: update LIBRARIES.md with every behaviour change; add an ADR for every changed, reverted or rejected approach.
- DON'T: create task-specific docs, implementation plans, status or completion reports (scratch work goes in the session scratchpad, not the repo).
- DON'T: restate a fact that LIBRARIES.md, a layer `CLAUDE.md` or the code already owns — link or delete instead.

## CI/CD & Code Quality

The repository enforces:
- Code formatting via `dotnet format` (required, unformatted code is rejected)
- Full test suite must pass (Release configuration)
- `docs-sync.ps1 -Check` must pass
- Package metadata validation
- Merge conflict detection
- No direct merges from main/master to main/master branches

Run before pushing:
```bash
cd dotnet/
dotnet format AQ.sln
dotnet build AQ.sln --configuration Release
dotnet test AQ.sln --configuration Release
```

## Project Assumptions

- **Implicit Usings** and **Nullable Reference Types** are enabled in all projects; handle nullability explicitly.
- **Central package management**: versions live in `dotnet/Directory.Packages.props`, target framework is `net10.0` (set in `dotnet/Directory.Build.props`).
- **No service abstractions for data**: handlers fetch what they need from the DbContext directly.
- **Type safety over runtime strings**: use generic constraints and `typeof(T).Name`, not manual type-name constants.
- **No app knowledge**: no app branding, no app table names, no app claim names in a shared library. If a library needs something app-specific, it takes it as an option or a type parameter.
