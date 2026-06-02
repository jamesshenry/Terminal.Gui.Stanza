---
status: accepted
---

# ADR 6: View-Model Discovery and Generated Lifecycle

## Context

Stanza needs a consistent way to determine whether a decorated view participates in MVVM generation and, if it does, which view-model type to target. The project also wants to avoid a rigid runtime base class so authored views can inherit from any appropriate Terminal.Gui type.

That means two architectural decisions have to stay aligned:

- how the generator discovers the view-model type
- how the generated partial class manages initialization and disposal once that type is known

## Decision

The generator treats view-model discovery as an ordered inference process.

`TuiViewParser.GetViewModelSymbol(...)` resolves the target type in this precedence order:

1. explicit generic attribute: `[TuiView<TViewModel>]`
2. constructor inference: a constructor parameter whose type implements `System.ComponentModel.INotifyPropertyChanged`
3. generic base-type inference: the first generic argument of the authored base type

If no compatible view-model type can be inferred, the current generator returns no `ViewDeclaration` and emits no partial view implementation.

When a view-model type is present, `InitializeComponentEmitter` generates a mixin-style runtime surface directly into the partial class:

- `ViewModel` property with reference-equality checking
- `BindingContext` property backed by a private field
- optional parameterless constructor when the user has not defined one
- optional single-parameter view-model constructor when the user has not defined one
- protected `Bind` helper for manual bindings
- `Dispose(bool)` override that disposes the binding context before calling the base implementation

Having both parameterless and parameterized constructors ensures full compatibility with both visual designer tools and Dependency Injection (DI) runtime containers.

The `ViewModel` setter is the initialization trigger: assigning a non-null, non-equal value calls `InitializeComponent()`.

When the `[TuiView]` or `[TuiView<TViewModel>]` attribute carries a `Title` value, the emitter assigns it in the generated parameterless constructor, eliminating the handwritten constructor override that would otherwise be required for static window titles.

## Consequences

### Positive

- Views are not forced into a Stanza-specific inheritance chain.
- Authors can choose between explicit generic annotation and inference-based authoring styles.
- Generated constructors avoid clobbering user-defined constructors when equivalent overloads already exist.
- View re-assignments are fully idempotent, avoiding redundant initialization passes.

### Negative

- Non-generic `[TuiView]` only works when inference succeeds; the attribute alone is not enough.
