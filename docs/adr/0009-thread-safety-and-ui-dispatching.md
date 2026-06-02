---
status: accepted
---

# ADR 9: Thread Safety and UI Thread Synchronization

## Context

Terminal.Gui (like almost all desktop UI toolkits) is fundamentally single-threaded. Any attempt to modify UI properties (such as `Text`, `Value`, or hierarchy) from a background thread—such as during an asynchronous database query, network request, or background timer update—will result in race conditions, memory corruption, or screen tearing.

In manual code, developers are forced to write boilerplate thread marshalling:

```csharp
App?.Invoke(() => _sourceLabel.Text = ViewModel.Target.Source);
```

Stanza's data-binding extensions must handle thread synchronization transparently so that background updates to ViewModel properties seamlessly update the terminal UI without risking thread safety violations.

## Decision

Centralize UI-thread synchronization directly inside the `BindingExtensions` primitive adapters:

1. Use Terminal.Gui's native thread-dispatching mechanism (`Application.Invoke` or equivalent wrapper) directly inside the update delegates of `BindOneWay` and `BindTwoWay`.
2. When the ViewModel raises a `PropertyChanged` event from a background thread, the binding extension intercepts it, wraps the UI setter call, and dispatches it onto the main UI thread queue.
3. For performance optimization, if the calling thread is already the main UI thread, execute the setter immediately to avoid unnecessary dispatching overhead.

## Consequences

### Positive

- Developers do not need to write `Application.Invoke` or `App?.Invoke` boilerplate in their ViewModels or custom views.
- Asynchronous operations, network clients, and timer ticks in the ViewModel are 100% safe to update properties directly.
- Thread-safety constraints are enforced globally and uniformly by the framework.

### Negative

- Introduces a dependency on Terminal.Gui's global thread scheduler within the core binding primitives.
- High-frequency background updates can overwhelm the UI message loop if not throttled or debounced.
