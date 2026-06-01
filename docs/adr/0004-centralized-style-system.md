---
status: deferred
---

# ADR 4: Centralized Style System

## Context

Early design notes proposed a compile-time style system where views could opt into reusable UI stanzas and the generator would splat those properties into emitted code. That idea is still attractive for reducing duplication, but it is not implemented in the current repository.

Today the generator only understands:

- class-level view metadata via `[TuiView]`
- per-control property assignments
- synthetic layout members
- synthetic binding members

There is no `TuiStyle` attribute, no style provider model, and no style-merging logic in the parser or emitter.

## Decision

Defer the centralized style system until the core generator contract is stable.

For the current implementation, styling remains explicit in the authored view initializer through normal Terminal.Gui properties such as `TextAlignment`, `CanFocus`, colors, or border-related settings.

Any future style system must satisfy these constraints:

1. Local property assignments always override style-provided values.
2. Style expansion must happen at compile time rather than through runtime reflection.
3. The feature must integrate cleanly with the existing IR/parser/emitter pipeline instead of bypassing it.

## Consequences

### Positive

- The current generator remains small and easy to reason about.
- There is no hidden precedence logic beyond normal initializer assignments.
- Documentation can accurately distinguish implemented features from aspirational ones.

### Negative

- Repetitive visual configuration must be duplicated across views.
- The design-system story remains incomplete until a style abstraction is added.
- Earlier planning documents that assumed compile-time style splatting must be treated as forward-looking rather than current behavior.
