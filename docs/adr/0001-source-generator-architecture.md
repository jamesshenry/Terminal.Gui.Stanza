---
status: proposed
---

# ADR 1: Source Generator Architecture and Project Separation

## Context

We want to generate Terminal.Gui v2 UI code from a declarative schema, this will be implemented using modern C# and Roslyn features (source generators, discrimated unions). Terminal.Gui v2 implements a new lifecycle management model, enabling modern standards for DI and unit testing among other things.

## Decision

There will be 2 main projects to enable this:

1. **Typical.UI.Abstractions**: A thin library containing attributes and enums, perhaps also value objects. These will be used by consumers and trigger source generators.
2. **Typical.Generators**: The `IIncrementalGenerator` project, this will use an **Intermediate Representation (IR)**.
   - **Parser**: Converts Roslyn symbols into internal POCO records or record structs.
   - **Emitter**: Converts records into Terminal.Gui C# code.
3. **Typical.UI**: The core library containing `BindingContext`, `BindingExtensions`, and `BindableView<T>`.

## Consequences

- **Pros**: Prevents circular dependencies, allows for snapshot testing of generator, decouples UI logic from roslyn API.
- Cons: Requires multiple projects
