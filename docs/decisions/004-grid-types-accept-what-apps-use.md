# 004. Grid and select types accept what the applications pass

Status: Accepted
Date: 2026-10-27

## Context
DQM's type check reported about 110 errors that came from the grid and select types, not from the application code: a grid row had to be a type with an index signature, so a plain interface was refused; a column's `render` had to take `unknown`, so a callback typed for its own cell value was refused; an enum option could not carry a boolean or null; and the autocomplete select refused `style`, which it already passes on to the Mantine select.

## Options considered
1. Leave the types and cast at every application column definition: about 80 files repeat the cast and it hides real errors.
2. Loosen the types to what the components already handle at run time.

## Decision
Option 2. The grid row type is `T extends object`, `render` is declared as a method (bivariant parameters), enum option values may also be boolean or null, and the autocomplete select declares `style`.

## Why
The components already turn enum values into strings and forward unknown props, so the types only had to say so. No run-time change.

## Consequences
A grid's row type is no longer checked for an index signature; a column's `dataIndex` is still checked against the row type.
