# AQ-Libraries — .NET

See root `CLAUDE.md` for the documentation rules and CI. What each library guarantees is in [docs/LIBRARIES.md](../docs/LIBRARIES.md) — read the section of the library you are changing first.

**Changing a library here means updating `docs/LIBRARIES.md` in the same piece of work**, then running `pwsh ../scripts/docs-sync.ps1 -Check`.

## Solution & Projects

```
dotnet/
  AQ.sln
  Directory.Build.props      # net10.0, implicit usings, nullable
  Directory.Packages.props   # Central NuGet version management — all versions here
  src/
    Actors/                  # AQ.Actors          — Actor, IActorContext, IActorScopeFactory, ActorBackgroundService
    Entities/                # AQ.Entities        — Entity, Entity<TPrincipal>, HierarchicalEntity, [Audited]
    Events/
      Domain/                # AQ.Events.Domain       — domain event interfaces and base record
      Integration/           # AQ.Events.Integration  — integration events and publisher
      Dispatchers/           # AQ.Events.Dispatchers  — in-process domain event dispatch
      Outbox/                # AQ.Events.Outbox       — OutboxEvent and its processing contract
    EntityFramework/         # AQ.EntityFramework — save pipeline, audit log, versioning, hierarchy service, state mappings
      SqlServer/Search/      # AQ.EntityFramework.SqlServer  — SQL Server search dialect
      PostgreSql/Search/     # AQ.EntityFramework.PostgreSql — PostgreSQL search dialect
    Generators/              # AQ.Generators     — Roslyn source generator (analyzer; netstandard2.0)
    ValueObjects/            # AQ.ValueObjects   — Email, Money, Address, ranges, HTML/markdown content, data collection
    CQRS/                    # AQ.CQRS           — command/query handlers and dispatchers
    StateMachine/
      Entities/              # AQ.StateMachine.Entities — definitions, instances, transitions, requirements
      Services/              # AQ.StateMachine.Services — requirement evaluation
    Utilities/               # AQ.Utilities — Results, Filter, Sort, Search, Merging, Verification, PeriodicReview,
                             #   Snapshots, Versioning (version groups), Tagging, Commenting, EntityData, References, Attachments (one project)
      Email/                 # AQ.Utilities.Email — email sending + localized templates
    DataSeeding/             # AQ.DataSeeding — EF Core seeding framework
    Identity/
      Core/                  # AQ.Identity.Core       — options, entities, abstractions
      OpenIddict/            # AQ.Identity.OpenIddict — server wiring, sessions, key management, management API
      UI/                    # AQ.Identity.UI         — Razor class library (login, account, admin pages)
  tests/                     # mirrors src/: one test project per library (Actors, EntityFramework, Generators,
                             #   Identity/*, StateMachine/Services, Utilities, ValueObjects)
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
- **Formatting**: run `dotnet format AQ.sln` before every push; CI will reject unformatted code.
- **Tests**: xUnit + NSubstitute (mocking) + FluentAssertions. Mirror the source structure under `tests/`; name projects `AQ.[Library].Tests`.
- **No service abstractions for data**: handlers fetch directly from `DbContext`, not through service wrappers.
- **Type safety**: use `typeof(T).Name` for type discovery in the requirements system — never manual type strings.
- **Rules and tags**: a guarantee a caller can rely on gets a rule in `docs/LIBRARIES.md` and a `// RULE XXX-n` comment on the enforcing line. Change the behaviour, change the rule.
- **Provider-specific code stays out of the core**: `AQ.Utilities` has no EF provider dependency; SQL Server and PostgreSQL specifics live in the `AQ.EntityFramework.*` provider packages.
- **Snapshot storage**: apps call `ApplyEntitySnapshots()` (one `EntitySnapshots` table, FK per snapshot type) instead of mapping each generated snapshot class; retention goes on `[Snapshottable(KeepLast, MaxAgeDays)]` and the app runs `SnapshotPruner` (`AddAqSnapshotPruning`) on its own schedule.
- **Generated data writers**: snapshot classes and the members of `[VersionGroup]` classes come from `AQ.Generators`; do not hand-write what `[Snapshottable]` / `[VersionGroup]` / `[Taggable(typeof(Tag))]` generate (version-grouped and taggable classes are `partial`; the app owns the tag entity, which implements `ITag`, and calls `ApplyEntityTags<Tag>()` and enables the `Tags` pipeline step).
- **Comments**: `[Commentable(typeof(AppComment))]` on an entity class (it need not be partial) generates `{Name}Comment`, which derives from the app's `AppComment : Comment<TPrincipal>` (the app adds its own columns there) and carries the link to the entity. The author and time are the audit creator and creation time. The app calls `ApplyEntityComments<AppComment>()`, adds the generated comment through its `DbSet` (never into a collection), and owns authorization. `[Commentable(MaxDepth = n)]` limits reply depth (unset: no limit; ADR-007). A review's comment type derives from `ReviewComment<TPrincipal>` (resolve and reopen on top-level comments); votes derive from `CommentVote<TPrincipal>` and are mapped by `ApplyCommentVotes<TVote, TComment>()`.
- **Periodic review state**: `PeriodicReviewStateBase` is two dates (a recurring review schedule, not an approval review); entities implement `IPeriodicReviewable<PeriodicReviewState>` (or `IPeriodicReviewable<AppState>` for an app-derived state), expose it as `PeriodicReview`, and map it with `HasPeriodicReviewState`. The app supplies `IPeriodicReviewDueCalculator<TEntity>` for entities whose next date is not given.

## Email localization conventions (`Utilities/Email`)

- Resource keys are descriptive ids (`Verification.Subject`, `Verification.Heading`), not English text, because bodies are long — unlike the app-level `SharedResource` in ELS. Regional resource files (`.zh-HK.resx`) hold only the overrides and fall back to their parent (`zh-Hant`).
- The template service methods take a trailing optional `CultureInfo? culture = null`; the hosting app calls `AddLocalization()` itself.
