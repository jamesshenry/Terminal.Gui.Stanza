---
status: accepted
---

# ADR 1: Source Generator Architecture and Project Separation

## Context

Stanza needs to let application code describe Terminal.Gui views in ordinary C# object initializers while still generating the imperative code that Terminal.Gui expects at runtime. The implementation also needs to stay testable: Roslyn analysis, dependency ordering, and emitted source should be verifiable without spinning up a terminal UI.

Earlier drafts assumed a separate abstractions package plus a runtime base class. The current repository has converged on a simpler shape:

- A consumer-facing runtime library that exposes the public attributes, IR records, binding helpers, layout extension members, and logging/configuration surface.
- A dedicated incremental generator project that parses source into a small intermediate representation and emits `InitializeComponent` plus the view-model mixin members.
- Test projects that validate emitted code through snapshots and basic end-to-end binding behavior.

## Decision

Use a two-layer production architecture:

1. `Terminal.Gui.Stanza`
   Exposes the public authoring surface used by application code:
   - `[TuiView]` and `[TuiView<TViewModel>]`
   - layout and binding extension members such as `Below`, `RightOf`, and `BindText`
   - runtime binding helpers such as `BindingContext` and `BindingExtensions`
   - IR records in the `Terminal.Gui.Stanza.IR` namespace shared with the generator implementation

2. `Terminal.Gui.Stanza.Generators`
   Owns compile-time transformation:
   - `TuiViewParser` converts annotated classes into IR records
   - `DependencyResolver` computes instantiation order from layout dependencies
   - `InitializeComponentEmitter` writes the generated partial class members and initialization code

Keep the generator architecture explicitly IR-driven so Roslyn analysis stays isolated from code emission logic.

## Consequences

### Positive

- The compile-time pipeline is easy to test with snapshot verification.
- Parser, ordering, and emission concerns remain separated.
- Application code depends on a single runtime package rather than a runtime plus a dedicated abstractions assembly.

### Negative

- The `Terminal.Gui.Stanza` project carries both runtime concerns and the public authoring contract, so the namespace name `Abstractions` is logical rather than physical.
- Generator and runtime must stay in lockstep because synthetic members such as `BindText` and `Below` only make sense when both pieces are present.
