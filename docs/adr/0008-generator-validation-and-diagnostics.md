---
status: accepted
---

# ADR 8: Generator Validation and Diagnostics Policy

## Context

Stanza is a compile-time transformation system. If parsing, ordering, or emission drifts from authored intent, the failure can show up as broken generated code, invalid runtime behavior, or silent degradation. The architecture therefore needs an explicit verification strategy and a clear statement of what the generator does and does not diagnose today.

## Decision

Use a layered validation strategy with minimal custom diagnostics.

### Verification Layers

1. Snapshot verification in `Terminal.Gui.Stanza.Generators.Tests`
   - validates the exact emitted source shape for representative inputs
2. Compilation validation in the snapshot test harness
   - generated trees are added back into the compilation and checked for Roslyn errors
3. End-to-end runtime tests in `Terminal.Gui.Stanza.Tests`
   - verify key observable behaviors such as initialization and two-way text binding

### Diagnostics Policy

The current generator emits behavior but very few domain-specific diagnostics.

- Missing or invalid constructs primarily fail through normal Roslyn compilation.
- Circular layout dependencies are handled as best-effort ordering in `DependencyResolver`; unresolved nodes are appended rather than rejected.
- The `[TuiView]` attribute surface is intentionally minimal: only `Title` is defined, and it is fully consumed by the generator. There are no silent no-op parameters.

The project therefore treats tests and generated-output review as the primary safety mechanism, with richer diagnostics deferred until the core transformation model stabilizes.

## Consequences

### Positive

- The validation story is simple and already implemented in the current repository.
- Snapshot tests make generator regressions visible at the emitted-source level.
- The project can evolve the authoring model quickly without maintaining a large custom diagnostic catalog.

### Negative

- Some architectural failures degrade silently instead of surfacing as precise diagnostics.
- Circular dependency cycles produce best-effort output rather than a build error.
- Debugging certain issues still requires reading generated code or test snapshots rather than relying on analyzer messages.
