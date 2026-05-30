---
status: proposed
---

# ADR 3: Hybrid Declaration Paradigm (Extensions + SG)

## Context

We want a declarative UI syntax that feels like modern C# but allows for the complex "plumbing" required by Terminal.Gui v2 and MVVM.

## Decision

We will use C# 14 **Extension Blocks** to add "Synthetic Properties" to the `Terminal.Gui.View` type. These properties serve two purposes:

1. **At Development Time**: They provide IntelliSense and type-safety within object initializers (e.g., `new Label { PositionBelow = ... }`).
2. **At Compile Time**: The Source Generator intercepts assignments to these properties to generate the actual `Pos`/`Dim` math and MVVM binding subscriptions.

We will reserve **Attributes** (like `[TuiView]`) strictly for class-level metadata that cannot be represented within a property initializer.

## Consequences

### Pros

- 100% type-safe
- no magic strings in layout
- refactoring-friendly
- excellent IDE discovery.

### Cons

- Requires C# 14/15 preview features
- requires the Source Generator to parse method bodies (Object Creation Expressions).
