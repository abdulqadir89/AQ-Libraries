# 001. Version groups replace data-copy versioning

Status: Accepted
Date: 2026-10-23

## Context
`[Versionable]` / `[Versioned]` kept labelled, numbered copies of an entity's data in a shared table (`EntityVersion`, `EntityVersioningService`). Nothing used it. Documents in DQM need versions that are real records of their own (own status, approval, attachments, content), grouped so users can list, move and detach them, with a free-form version label instead of a number.

## Options considered
1. Keep data-copy versioning and use it for documents: a copy stores field values only, so attachments, approval state and independent drafts do not fit; version numbers are integers.
2. Use snapshots: written on every save, keyed by the save counter, no label, no group, meant for audit history.
3. Version groups: each version is its own row sharing a group Guid, with a unique string label per group.

## Decision
Replace data-copy versioning with version groups. An entity is decorated with `[VersionGroup]` (partial class); the generator adds `VersionGroupId`, `Version` and the version-group interface. The library provides join, leave and relabel operations, query helpers, a unique index on group and label, and a save step that gives a new entity its own group.

## Why
A version that is a full record keeps its own attachments, approval and lifecycle, supports parallel drafts, and can be moved between groups or made independent. The old mechanism was unused, so removing it costs nothing.

## Consequences
`[Versionable]`, `[Versioned]`, `EntityVersion`, `EntityVersioningService`, `ApplyEntityVersioning` and `AddAqEntityVersioning` are gone; an app that used them must move to `[VersionGroup]` or snapshots. Entities opting in must be `partial` and migrated with a `VersionGroupId` column filled in for existing rows. The library does not decide which version is current or how publishing works; that stays in the app. The group id is not tied to any member's id, so leaving a group never disturbs the others.
