---
status: accepted
---

# ADR 8: Generator Validation and Diagnostics Policy

## Context

Stanza is a compile-time transformation system. If parsing, ordering, or emission drifts from authored intent, the failure can show up as broken generated code, invalid runtime behavior, or silent degradation. The architecture therefore needs an explicit verification strategy and a clear statement of what the generator does and does not diagnose today.

## Decision

Use a layered validation strategy with an active compile-time diagnostic verification framework.

### Verification Layers

1. Snapshot verification in `Stanza.TerminalGui.Generators.Tests`
   - validates the exact emitted source shape for representative inputs
2. Compilation validation in the snapshot test harness
   - generated trees are added back into the compilation and checked for Roslyn errors
3. End-to-end runtime tests in `Stanza.TerminalGui.Tests`
   - verify key observable behaviors such as initialization and two-way text binding

### Diagnostics Policy

The generator actively acts as a compile-time static analyzer, reporting explicit compiler diagnostics to prevent silent degradation or invalid layout orders:

- **`STN001` (Error):** Circular layout dependencies are caught inside the parser and reported immediately, halting the build.
- **`STN002` (Error):** Annotated classes lacking the `partial` modifier are flagged, preventing invalid partial class generation.
- **`STN003` (Error):** Base classes lacking accessible parameterless constructors are intercepted, ensuring the generated parameterless constructor is safe.

This diagnostic framework enforces strict safety constraints early in the compilation lifecycle.

## Consequences

### Positive

- The validation story is robust and integrated directly into the Roslyn compiler pipeline.
- Snapshot tests make generator regressions visible at the emitted-source level.
- Invalid architectural shapes (like non-partial classes or layout cycles) fail the build with explicit and actionable compiler messages.

### Negative

- The compiler pipeline carries strict verification constraints that must be maintained across newer Roslyn and language versions.
