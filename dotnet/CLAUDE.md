# AQ-Libraries — .NET

See root `CLAUDE.md` for architecture overview, DDD patterns, and CI/CD rules.

## Solution & Projects

```
dotnet/
  AQ.sln
  Directory.Packages.props   # Central NuGet version management — all versions here
  src/
    Entities/                # AQ.Entities       — Base Entity<TId> aggregate root
    ValueObjects/            # AQ.ValueObjects   — Immutable value object primitives
    CQRS/                    # AQ.CQRS           — Command/Query patterns
    Extensions/              # AQ.Extensions     — Utility extension methods
    Events/
      Domain/                # AQ.Events.Domain       — Domain event interfaces
      Integration/           # AQ.Events.Integration  — Integration event handling
      Dispatchers/           # AQ.Events.Dispatchers  — Dispatch infrastructure
      Outbox/                # AQ.Events.Outbox       — Reliable outbox publishing
    StateMachine/
      Entities/              # AQ.StateMachine.Entities — Core state machine types
      Services/              # AQ.StateMachine.Services — Transition execution
    Utilities/
      Filter/                # AQ.Utilities.Filter  — Query filtering
      Search/                # AQ.Utilities.Search  — Search query helpers
      Sort/                  # AQ.Utilities.Sort    — Sorting/ordering
      Results/               # AQ.Utilities.Results — Result pattern
    DataSeeding/             # AQ.DataSeeding — EF Core seeding framework
  tests/
    StateMachine/Services/   # AQ.StateMachine.Services.Tests
```

## Commands

```bash
# Run from dotnet/
dotnet restore AQ.sln
dotnet format AQ.sln                              # Required before push (CI enforces this)
dotnet build AQ.sln
dotnet build AQ.sln --configuration Release

dotnet test AQ.sln
dotnet test AQ.sln --configuration Release
dotnet test AQ.sln --filter "ClassName=MyClass"

dotnet pack AQ.sln --configuration Release
```

## Key Rules

- **Central package management**: all NuGet versions live in `Directory.Packages.props` — never set `Version=` in individual `.csproj` files.
- **Target framework**: `net10.0`. Implicit usings + nullable reference types enabled globally.
- **No service abstractions for data**: handlers fetch directly from `DbContext`, not through service wrappers.
- **Type safety**: use `typeof(T).Name` for type discovery in the requirements system — never manual type strings.
- **Formatting**: run `dotnet format AQ.sln` before every push; CI will reject unformatted code.
- **Tests**: xUnit + NSubstitute (mocking) + FluentAssertions. Mirror source structure under `tests/`.

## StateMachine Requirements System

- Transitions declare requirements; handlers evaluate them before execution.
- **Specific handlers**: `IStateMachineTransitionRequirementHandler<TRequirement>` — typed, one per requirement type.
- **Generic handlers**: `IStateMachineTransitionHandler` — process all requirements with status tracking; multiple allowed.
- Two-phase evaluation: specific handlers run first, then all generic handlers.
- Requirements reference `typeof(T).Name` — no manual string constants.
- See `src/StateMachine/Entities/README.md` for detailed usage patterns.

## DataSeeding

- Multiple seeder types: test data, configuration, migration.
- Automatic dependency resolution + priority-based ordering within dependency levels.
- See `src/DataSeeding/README.md` for usage.
