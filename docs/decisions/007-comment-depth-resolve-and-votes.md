# 007. Comment depth is configurable; review comments carry resolve and votes

Status: Accepted
Date: 2026-10-28

## Context
Comments allowed one level of replies, fixed in the base class. A document review needs the same threads plus a resolved state per thread and up or down votes, and other apps may want deeper threads.

## Options considered
1. Keep one fixed level and build resolve and votes in each app: every app repeats the rules and the table.
2. Make the depth a setting of the commentable class (unset means no limit) and add an opt-in review comment base and a vote base.
3. Always allow unlimited depth: a review thread would lose its one-level shape.

## Decision
Option 2. `[Commentable(MaxDepth = n)]` limits replies, and unset allows any depth. `ReviewComment<TPrincipal>` adds resolve and reopen on top-level comments and keeps the last change. `CommentVote<TPrincipal>` is one up or down vote per voter and comment, mapped by `ApplyCommentVotes`.

## Why
The depth belongs to the kind of record, not to the library, and resolve and votes are the same wherever a review exists, so the library owns the rules and the app only adds its own columns (anchors, suggestions).

## Consequences
Each comment stores its depth. `ParentId` is now the comment answered, not always the top-level one. The default is no longer one level: an app that relied on the old limit sets `MaxDepth = 1`, and an app with existing replies needs a migration adding the `Depth` column (0 for top-level comments, 1 for existing replies).
