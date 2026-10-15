# Decisions (ADRs)

One file per decision that was not obvious, or that changed course: `NNN-short-title.md` (three digits, next free number).
Rules for when to write one: root `CLAUDE.md`, section "Documentation".

```markdown
# NNN. Title

Status: Accepted | Superseded by NNN | Rejected
Date: YYYY-MM-DD

## Context
What problem, what constraints.

## Options considered
1. Option: pros / cons
2. Option: pros / cons

## Decision
What we chose.

## Why
The reasons that decided it. For a Rejected or reverted approach: what was tried and why it did not work.

## Consequences
What gets easier, what gets harder, what to watch. For a library: what a consuming app has to change.
```

Reference an ADR from code with a one-line comment at the site: `// ADR-NNN: why X, not Y`.

## Index
_None yet. Add a line per ADR: `- [NNN Title](NNN-short-title.md) - status`._
