---
status: accepted
---

# ADR 2: MVVM Binding Logic and Lifecycle Management

## Context

Terminal.Gui does not provide a first-class dependency-property or binding system. Without generator support, even simple MVVM scenarios require repeated `PropertyChanged` subscriptions, event handlers for user edits, and explicit cleanup to avoid leaking handlers.

The original design assumed a handwritten `BindableView<T>` base class. The current implementation instead generates the minimum view-model plumbing directly into each annotated partial view so that user code can inherit from any appropriate Terminal.Gui base type.

## Decision

Adopt a generated mixin model for MVVM support:

1. `BindingContext` owns the lifetime of generated subscriptions and disposes them as a group.
2. `BindingExtensions` provides the reusable runtime primitives:
   - generic one-way and two-way binding helpers
   - view-specific adapters such as `ApplyBindText`, `ApplyBindChecked`, `ApplyBindCommand`, `ApplyBindVisible`, and `ApplyBindEnabled`
3. `[TuiView<TViewModel>]` or constructor/base-type inference tells the generator which view-model type to target.
4. The generator emits these members into the partial view when a view-model type is present:
   - `ViewModel` property with reference-equality guards to prevent redundant layout passes
   - `BindingContext` property backed by a mutable field, allowing context recreation on ViewModel re-binding
   - convenience `Bind` helper for manual bindings
   - `Dispose(bool)` override that disposes the binding context before delegating to the base class
5. Generated bindings are emitted inside `InitializeComponent()` and registered through `BindingContext.AddBinding(...)`.

The generated `ViewModel` setter enforces a deterministic state machine:

- It disposes of active bindings before replacing the `ViewModel` instance.
- It instantiates a fresh, empty `BindingContext` during transition.
- It invokes `InitializeComponent()` if the new reference is non-null, supporting safe dynamic re-binding and null-valued detachment.

## Consequences

### Positive

- Views are free to inherit from `View`, `Window`, or other Terminal.Gui types rather than a Stanza-specific base class.
- Binding logic is centralized in runtime helpers instead of being duplicated in emitted code for each control.
- ViewModels can be reassigned or cleared dynamically at runtime without leaking event subscriptions.
- The approach works naturally with `INotifyPropertyChanged` view models, including `CommunityToolkit.Mvvm` types.

### Negative

- The generated binding lifecycle is implicit, which makes debugging slightly more indirect than explicit handwritten event wiring.
- Authoring still requires partial classes and the generator/runtime pair to remain compatible.
