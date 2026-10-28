# 006. The review schedule is named "periodic review"

Status: Accepted
Date: 2026-10-28

## Context
ADR-005 reduced a review to a schedule: the last review date and the next due date. Apps also have approval-style reviews (a reviewer approves a record, a document is under review, a risk review is approved). Users of consuming apps confused the schedule with those reviews, because both were called "review".

## Options considered
1. Keep "review" and rely on UI wording: the library and app code keep the ambiguous name, and every new reader has to be told which review is meant.
2. Rename only user-visible labels in apps: code and docs still say "review" for two different things.
3. Rename the utility to "periodic review" everywhere (types, namespaces, entity member, docs) and keep the stored column names.

## Decision
Option 3. Types become `PeriodicReviewStateBase`, `PeriodicReviewState`, `IPeriodicReviewable`, `IPeriodicReviewDueCalculator`, `PeriodicReviewableExtensions`; the mapping is `HasPeriodicReviewState`; the entity member is `PeriodicReview`; the pipeline flag is `PeriodicReview`. Columns `LastReviewedAt` and `NextReviewDue`, the `MarkReviewed` and `SetNextReviewDue` methods, and the rule ids RVW-1..3 are unchanged.

## Why
The name now says the review recurs on a schedule, which separates it from approval reviews. Keeping the column names means no schema change for apps.

## Consequences
Consuming apps rename their usings, entity member and mapping call in one pass; no migration is needed. ADR-005 still stands; only the naming changed.
