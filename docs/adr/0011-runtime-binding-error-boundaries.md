---
status: accepted
---

# ADR 11: Runtime Binding Error Boundaries and Logging

## Context

In an MVVM application, runtime property getters, custom converters, and data formatting expressions may occasionally throw unexpected exceptions (e.g., `NullReferenceException` during nested path evaluation, formatting exceptions, or network timeouts).

If an exception escapes a binding setter/getter, it will propagate up into Terminal.Gui's main event loop, causing the entire terminal application to crash abruptly, often corrupting the user's terminal window state.

## Decision

Implement explicit error boundaries inside all core binding wrappers:

1. Wrap all `uiSetter` and `vmSetter` delegate executions in `try-catch` blocks inside `BindingExtensions.cs`.
2. If an exception is caught during a binding sync operation:
   - Prevent the exception from bubbling up to the Terminal.Gui main thread.
   - Route the exception to the configured `StanzaConfig.Logger` for diagnostic tracking.
   - Degrade gracefully (e.g., leave the UI control's previous value intact, or set it to an empty fallback state).
3. Provide a visual error hook in debug builds (such as flashing the status bar or outputting to stderr) to ensure developers catch binding bugs during development without crashing the active terminal session.

## Consequences

### Positive

- Improves application resilience; a single broken data binding or converter bug cannot crash the entire user session.
- Diagnostic logs are centralized, making it easy to identify which specific property sync failed.

### Negative

- Swallowing exceptions can occasionally hide logical developer errors if logging is not actively monitored during development.
