---
status: proposed
---

# ADR 2: MVVM Binding Logic and Lifecycle Management

## Context

Terminal.Gui does not have a data-binding system akin to WPF or Avalonia instead using a model more similar to Winforms. This requires extensive wiring of events which are boiler-plate heavy and vulnerable to memory leaks if not disposed of properly.

## Decisions

We will implement a centralized pattern of Binding:

1. BindingContext: A container that tracks `IDisposable` bindings.
2. BindingExtensions: Fluent wrappers for `ObservableObject` that return `IDisposable` handlers for VM-to-UI sync.
3. **Generator-Driven Wiring**:
   - The generator will look for `[Bind]` attributes.
   - It will automatically emit calls to `BindingExtensions` inside the `InitializeComponent` method.
   - All generated bindings will be registered to the view's `BindingContext`.
4. **Dispose Pattern**: The base `BindableView<T>` will dispose of the `BindingContext` in its own `Dispose` method, ensuring zero memory leaks.

## Consequences

### Pros

- Type-safe bindings using `nameof`
- automatic cleanup
- compatibility with `CommunityToolkit.Mvvm`.

### Cons

- Requires strict adherence to the `partial class` and `base.Dispose()` patterns.
