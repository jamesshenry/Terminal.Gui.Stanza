---
status: accepted
---

# ADR 7: Nested Subview Composition and View-Model Propagation

## Context

One of Stanza's core goals is to let a parent view be composed from reusable child views declared as normal properties or fields. In practice, many of those child views need access to the same view model as the parent so that bindings in nested controls can be generated without extra handwritten wiring.

The project therefore needs a compile-time rule for deciding when a child view should automatically receive the parent's `ViewModel`.

## Decision

Treat compatible nested subviews as same-view-model components and propagate the parent `ViewModel` during generated initialization.

`TuiViewParser` identifies compatible subviews with `HasViewModelPropertyOfType(...)` using a two-tier heuristic:

1. for source types, rerun the same view-model discovery logic used for top-level views
2. for referenced binaries, look for a `ViewModel` property whose type exactly matches the parent's inferred view-model type

Matching member names are stored in `ViewDeclaration.SubviewsWithViewModel`.

`InitializeComponentEmitter` then emits child setup in this order:

1. instantiate the child view if needed
2. assign `child.ViewModel = this.ViewModel` for compatible subviews
3. apply generated property assignments and bindings
4. add the child to the parent hierarchy

This makes nested views behave like composable fragments over a shared view-model contract rather than isolated MVVM islands.

## Consequences

### Positive

- Parent views can be decomposed into reusable child views without manual view-model forwarding code.
- Nested binding generation works for child views that participate in the same view-model type.
- The behavior works for both source-defined and referenced child view types.

### Negative

- Propagation currently requires exact type compatibility; there is no variance, adapter, or child-specific projection model.
- The detection heuristic is structural and convention-based rather than an explicit composition contract.
- Automatic propagation assumes the child should share the parent view model, which limits more complex parent-child MVVM relationships.
