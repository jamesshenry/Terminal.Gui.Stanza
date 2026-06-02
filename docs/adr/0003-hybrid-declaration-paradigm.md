---
status: accepted
---

# ADR 3: Hybrid Declaration Paradigm (Extensions + Source Generation)

## Context

The project wants the ergonomics of declarative view definitions without introducing a custom markup language. At the same time, ordinary C# initializers cannot directly express all of the runtime behavior Stanza needs, such as relative layout ordering and generated binding subscriptions.

## Decision

Use a hybrid declaration model with two authoring mechanisms:

1. Class-level attributes for view metadata and view-model association.
   - `[StanzaView]`
   - `[StanzaView<TViewModel>]`

2. C# extension members on `Terminal.Gui.View` for per-control declarative intent inside object initializers.
   - layout placeholders such as `PositionX`, `PositionY`, `Width`, `Height`, `Below`, and `RightOf`
   - binding placeholders such as `BindText`, `BindChecked`, `BindValue`, `BindCommand`, `BindVisible`, and `BindEnabled`

The extension members are intentionally placeholders. They exist to give authors a discoverable, typed surface in C# while the generator interprets those assignments and rewrites them into concrete Terminal.Gui code.

The parser handles two categories of initializer content:

- direct property assignments that are emitted as-is
- synthetic assignments that are translated into either layout expressions or binding calls

For dependency detection, the parser also inspects the right-hand side syntax tree of assignments and records references to sibling subviews.

## Consequences

### Positive

- The authoring model stays inside ordinary C# and benefits from editor completion.
- Refactoring remains safe because relative references and bindings are typically expressed with `nameof(...)`.
- The generator can add behavior without inventing a separate DSL or runtime reflection scheme.

### Negative

- The extension members are not meaningful at runtime on their own; they only become real behavior when the generator runs.
- The approach depends on preview-language features and Roslyn syntax analysis.
- Some author intent is encoded indirectly through placeholder members, so generated output is the ultimate source of truth for debugging.
