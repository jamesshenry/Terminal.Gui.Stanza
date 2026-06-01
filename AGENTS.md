# Terminal.Gui.Stanza

A declarative, attribute-based UI engine for **Terminal.Gui v2**, targeting .NET 11 / C# 15. **Terminal.Gui.Stanza** (or simply "Stanza") transforms the imperative Terminal.Gui API into a modern, schema-driven framework using Source Generators and C# 14/15 Extension Members.

## Build & Test

- Build: `dotnet build Terminal.Gui.Stanza.slnx`
- Test: `dotnet test Terminal.Gui.Stanza.slnx`
- Single test: `dotnet test Terminal.Gui.Stanza.slnx --filter "FullyQualifiedName~TestName"`
- Snapshot Testing: Uses `Verify.SourceGenerators` to verify emitted code against `tests/Terminal.Gui.Stanza.Generators.Tests/snapshots/`

## Project Structure

- `src/Terminal.Gui.Stanza.Abstractions/` — Marker attributes (`[TuiView]`, `[TuiStyle]`), Layout Enums (`Anchor`), and the IR (Intermediate Representation) schemas. **Zero external dependencies.**
- `src/Terminal.Gui.Stanza.Generators/` — The Roslyn `IIncrementalGenerator`. Contains the dependency graph logic, IR Parser, and C# Emitter.
- `src/Terminal.Gui.Stanza/` — Core framework library. Contains `BindingContext`, `BindableView<T>`, and the **C# 14 Extension Blocks** providing the fluent layout and binding API for `Terminal.Gui.View`.
- `tests/Terminal.Gui.Stanza.Generators.Tests/` — Unit tests for the generator logic using snapshot verification.

## The Stanza Paradigm

Stanza eliminates "Glue Code" by treating UI definitions as a **Schema** rather than a **Script**. It resolves the core friction points of building complex terminal UIs:

1. **The Reference Problem**: C# field/property initializers cannot reference other instance members (e.g., `Y = Pos.Bottom(lbl)` fails). Stanza’s generator intercepts these declarations and moves them into an ordered `InitializeComponent` method where relative references are legally allowed.
2. **The Binding Gap**: Terminal.Gui lacks a native dependency-property system. Stanza provides synthetic extension properties (e.g., `BindText = nameof(vm.Data)`) that the generator wires into a leak-proof MVVM lifecycle.
3. **The Design System**: Through `[TuiStyle]`, users define UI "stanzas" (bundles of borders, colors, padding) once. The generator "splats" these settings at compile-time, ensuring zero runtime performance overhead.

## Key Namespaces

- `Terminal.Gui.Stanza.Binding` — `BindingContext` and `BindingExtensions` (The MVVM glue).
- `Terminal.Gui.Stanza.Layout` — C# 14 Extension properties for `Terminal.Gui.View` (The Layout DSL).
- `Terminal.Gui.Stanza.Generators.IR` — The Intermediate Representation models that decouple TUI logic from Roslyn symbols.

## ADR Index (Architecture Decision Records)

Located in `docs/adr/`:

- `0001-source-generator-architecture.md` — Project separation and IR pattern.
- `0002-mvvm-binding-logic.md` — `BindingContext` and lifecycle management.
- `0003-hybrid-declaration-paradigm.md` — Combining Attributes with C# 14 Extensions.
- `0004-centralized-style-system.md` — Compile-time "Splatting" of UI styles.
- `0005-dependency-graph-initialization.md` — Automated view instantiation ordering based on layout relationships.

# Coding Guidelines

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

- State your assumptions explicitly.
- If multiple interpretations of a `[TuiStyle]` override exist, document the precedence (local property assignment always wins over style property).

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- Stanza is a build-time tool. If a feature can be solved by emitting standard C#, avoid creating complex runtime reflection helpers.
- abstractions for single-use code are discouraged.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

- When updating the `Terminal.Gui.Stanza.Generators` Parser, ensure pre-existing snapshots in the test project still pass or are updated intentionally.
- Match existing style, even if you'd do it differently.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

- "Add TabView support" → "Define `[Tab]` attribute, update IR to support child-containers, update Emitter to wrap views in `Tab` objects, verify snapshot."

---

## Absolute Paths

Trust the working directory. Use paths relative to the root of the site as a priority; do not prefix with drive and folder unless absolutely necessary. Do not `cd` into folders superfluously. Trust your working directory.
