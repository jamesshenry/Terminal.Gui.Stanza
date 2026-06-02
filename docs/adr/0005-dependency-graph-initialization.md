---
status: accepted
---

# ADR 5: Dependency Graph Initialization

## Context

Subview initializers often depend on sibling views for layout, for example `Below = nameof(TitleLabel)` or `X = Pos.Right(LeftPanel)`. In ordinary instance initialization, those references are awkward or illegal because sibling members may not have been constructed yet.

Stanza needs to preserve a flat declarative view definition while still emitting legal C# that instantiates controls before any layout expression references them.

## Decision

Build a layout dependency graph during parsing and topologically sort subviews before emission.

The current implementation does this in three steps:

1. `TuiViewParser` records layout constraints from synthetic members like `Below` and `RightOf`.
2. The parser also performs AST-based dependency detection for ordinary assignments whose expressions reference sibling view identifiers.
3. `DependencyResolver` uses Kahn's algorithm to produce a safe instantiation order, and `InitializeComponentEmitter` emits control construction before property assignment and hierarchy insertion.

Generated initialization therefore follows this shape:

1. instantiate controls in dependency order, using a safe check-before-add pattern to prevent duplicate subviews
2. apply properties and translated layout expressions
3. wire bindings
4. add controls to the parent view hierarchy

Subviews that themselves expose a compatible `ViewModel` property are assigned the parent `ViewModel` during the instantiation phase.

## Consequences

### Positive

- Authors can keep view declarations flat and readable.
- Relative layout references compile into legal, ordered C#.
- Dependency ordering is centralized and testable independent of Terminal.Gui runtime behavior.
- Circular layout dependencies are actively detected by the generator and reported as `STN001` compiler errors, preventing infinite rendering loops.

### Negative

- The dependency model only captures relationships the parser can see in initializer syntax.
- Initialization order becomes generator-defined rather than purely source-order-defined.
