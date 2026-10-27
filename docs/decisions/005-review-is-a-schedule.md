# 005. A review is a schedule, separate from verification

Status: Accepted
Date: 2026-10-28

## Context
The review state kept who last reviewed, which process produced the review, when it was notified, and when it is next due. Reviewing also verified the entity, a verification reset could be undone by a review in the same save, and a review counted as stale when verifiable data changed after it. Who reviews and how a review is carried out differ from app to app, so the reviewer and the process link were app concepts inside a shared library, and review and verification could not be used apart.

## Options considered
1. Keep the state and make the reviewer type and the process link configurable: the library stays tied to one kind of workflow, and every app carries columns it may not use.
2. Record each review as its own row (who, when, outcome, comment): a second history store beside whatever trail the app already keeps.
3. Reduce the state to the last review date and the next due date, and let an app extend it.

## Decision
Option 3. The base state holds `LastReviewedAt` and `NextReviewDue`; an app derives its own state type for anything more. The next date comes from the caller, else from a per-entity due calculator the app registers, else a still-future date already set. Review and verification no longer touch each other. Discussion on an entity is a separate utility (comments), not part of a review.

## Why
How a review was carried out, and by whom, belongs to the app, which records it in its own way. The library only needs to answer when something is due next, and a schedule is the same for every app. Splitting verification out removes three couplings: the reviewer added to verification, the review that survived a reset, and the staleness check.

## Consequences
An app drops its reviewer, process and notified columns, and its review state interface becomes `IReviewable<TState>`. An app that notified from the notified flag keeps its own bookkeeping or shows overdue items instead (`IsOverdue`). A review no longer verifies; an app that wants that calls the verification state itself.
