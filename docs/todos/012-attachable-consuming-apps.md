# 012. Move consuming apps to generated attachments before this reaches main

Status: Open
Date: 2026-11-24
Location: `dotnet/src/Utilities/Attachments`, `dotnet/src/Generators/Attachments`

## Context
`[Attachable]` generates one attachment class per entity (a shared table, an optional link to the record, limits) and replaces three older types, which this branch deletes without a shim: `AttachmentLimitAttribute` with `AttachmentLimitResolver`, the `IHasAttachments` marker and `PendingAttachment`. The branch is not merged to main yet because the apps that use the old types have not moved.

## Scope
For each consuming app (DQM first, ELS: `d:\Repositories\ELS` has `IHasAttachments` on eight entities and its own `Attachment` entity with `AttachmentEntityTypeRegistry`; also CVETrack and any other app that references AQ):
1. Replace `AttachmentLimitAttribute` with the options of `[Attachable]` and `AttachmentLimitResolver` with `AttachmentRegistry.Find(name).Limits`.
2. Remove `IHasAttachments` from the entities and put `[Attachable(typeof(AppAttachment))]` on them.
3. Add the app's `AppAttachment : Attachment<TPrincipal>`, call `ApplyEntityAttachments<AppAttachment>()`, add a migration.
4. Replace `PendingAttachment` staging: a file uploaded before its record exists is a pending attachment (`CreateStaged`), the save of the record calls `Link`, and a sweep removes pending ones older than the retention the app sets. Drop the app's `PendingAttachments` table after its rows are imported.
5. Import the existing files as rows (an app-owned job that walks the file store; the file store is not part of this library) and run it before the migration that makes the new links required.

## Impact
An app that updates AQ without these steps fails to build. Files and their paths are untouched.

## Options
1. Move every app in one release: simple, needs all apps ready.
2. Merge with the old types kept as obsolete: no app breaks, but the two ways stay side by side. Rejected for now.

## Done when
Every consuming app builds on the branch with the generated attachments and has imported its files, then the branch merges to main. Set Status to Done.
