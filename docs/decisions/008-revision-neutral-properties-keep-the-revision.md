# 008. Revision-neutral properties: bookkeeping saves keep the revision

Status: Accepted
Date: 2026-11-10

## Context
Every save raises an entity's revision, and an app can make the revision a concurrency token and an `If-Match` precondition (stale-save protection). A background job that only recomputes a flag on a record people also edit (a validity flag, a last-checked time) then makes every open edit form stale: the person gets a 412 for a change they never saw and cannot have clashed with. Atomic SQL updates avoid this for counters, but skip the save pipeline (no audit event, no events), and do not fit a recomputed value.

## Options considered
1. **Atomic `ExecuteUpdate` per bookkeeping column** (what ELS does for counters): no pipeline, no audit event, conditions on the revision must be hand-written in each statement.
2. **A second revision for content only**: a new column and migration per entity type, and every writer must pick the right revision.
3. **A property attribute the pipeline reads** (chosen): the save pipeline leaves the revision alone when every modified property is marked, and everything else (the revision comparison on save, the audit log, events) works as before.
4. **An attribute with a condition** (only for system actors, or only for some values): more power, but no case needs it yet; the revision comparison already prevents the overwrites a condition would guard against.

## Decision
`[RevisionNeutral]` marks a property. A save that modifies only marked properties of an entity does not stamp it: no revision, update time, updater or snapshot. Any other modified property, a touch of the revision, or a change in an owned part makes the save an ordinary one. The save still compares against the revision it loaded, so a neutral write cannot overwrite a person's newer edit; it fails and the writer retries.

## Why
The comparison and the bump are separate: leaving the bump out does not leave the check out. The writer stays a normal tracked save, so the audit log and domain events keep working, and the rule is declared once on the property.

## Consequences
- Two neutral saves of the same row do not conflict with each other (the revision does not move). The attribute is for a value with one logical writer or one recomputed in full. Counters and read-modify-write values still need an atomic update.
- A neutral save does not change "last edited": the audit log is where a bookkeeping write is recorded.
- Apps that never use the attribute see no change.
- A condition on the attribute is the next step if a case appears (option 4).
