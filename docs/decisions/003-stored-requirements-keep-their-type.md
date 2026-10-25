# 003. Stored transition requirements keep their type

Status: Accepted
Date: 2026-10-25

## Context
Transition requirements were saved as a JSON object keyed by type name, but each value was written through the interface type, so every property was lost (`{"NoActiveDependencyRequirement":{}}`), and read back as an internal `JsonStoredRequirement`. The evaluator picks handlers by the requirement's exact type, so a stored requirement could never match its handler and the transition it sat on could never run. Saving a transition again wrote the entries under the key `JsonStoredRequirement`, and malformed JSON read back as an empty list, which means no requirements.

## Options considered
1. Keep the internal wrapper and have the evaluator translate it to the real type when it looks for a handler: every consumer of `Requirements` (diagram, copy, history) would still see wrappers, and the data loss on save stays.
2. Make each app supply its own value converter: duplicates the format and leaves the library methods wrong.
3. Register requirement types by name and serialize and deserialize through them.

## Decision
Option 3. `StateMachineRequirementTypes` maps type names to types (an app registers its assemblies at startup); `StateMachineRequirementSerializer` writes the runtime type with all properties and reads the registered type back. The stored shape is unchanged, so existing rows load. An unregistered name becomes an `UnresolvedStateMachineTransitionRequirement` that is written back as it was.

## Why
The registry is the one place that knows which names are real, and keeping unknown names lossless means a removed requirement type cannot corrupt rows or silently open a transition. Failing on malformed JSON replaces an empty result that dropped requirements without a trace.

## Consequences
An app must register its requirement assemblies before transitions are loaded; without it every requirement reads as unresolved and blocks its transition. `JsonStoredRequirement` is gone (apps that compared it by reflection compare requirements with `Equals` instead; the requirement records have value equality).
