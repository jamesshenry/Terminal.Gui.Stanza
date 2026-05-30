---
status: proposed
---

# ADR 5: Dependency Graph Initialization

## Context

In standard C#, a property initializer cannot reference another instance property (e.g., `Y = Pos.Bottom(OtherLabel)` fails).

## Decision

The Source Generator will analyze the relationships defined in the UI class (e.g., `PositionBelow = Target`). It will construct a **Dependency Graph** to determine the correct instantiation order.
The generator will then emit the code inside a private `InitializeComponent()` method where these references are legally allowed.

## Consequences

- **Pros**: Allows a flat, declarative UI file while the generator handles the complex C# ordering requirements.
