# Terminal.Gui.Stanza

A declarative, attribute-based UI engine for **Terminal.Gui v2**, targeting .NET 11 / C# 15. **Terminal.Gui.Stanza** (or simply "Stanza") transforms the imperative Terminal.Gui API into a modern, schema-driven framework using Source Generators and C# 14/15 Extension Members.

## Build & Test

- Build: `dotnet build Terminal.Gui.Stanza.slnx`
- Test: `dotnet test Terminal.Gui.Stanza.slnx`
- Single test: `dotnet test Terminal.Gui.Stanza.slnx --filter "FullyQualifiedName~TestName"`
- Snapshot Testing: Uses `Verify.SourceGenerators` in `src/Terminal.Gui.Stanza.Generators.Tests/`

## Project Structure

- `src/Terminal.Gui.Stanza/` — Consumer-facing runtime and authoring surface. Contains `[TuiView]`, IR records, `BindingContext`, binding helpers, and layout extension members.
- `src/Terminal.Gui.Stanza.Generators/` — The Roslyn `IIncrementalGenerator`. Contains the parser, dependency resolver, and C# emitter.
- `src/Terminal.Gui.Stanza.Generators.Tests/` — Snapshot-driven generator tests that also recompile generated output.
- `src/Terminal.Gui.Stanza.Tests/` — End-to-end runtime tests for generated binding behavior.

## The Stanza Paradigm

Stanza eliminates "Glue Code" by treating UI definitions as a **Schema** rather than a **Script**. It resolves the core friction points of building complex terminal UIs:

1. **The Reference Problem**: C# field/property initializers cannot reference other instance members (e.g., `Y = Pos.Bottom(lbl)` fails). Stanza’s generator intercepts these declarations and moves them into an ordered `InitializeComponent` method where relative references are legally allowed.
2. **The Binding Gap**: Terminal.Gui lacks a native dependency-property system. Stanza provides synthetic extension properties (e.g., `BindText = nameof(vm.Data)`) that the generator wires into a leak-proof MVVM lifecycle.
3. **The Deferred Design System**: Centralized style splatting is still planned, but not implemented in the current repository. Styling remains explicit in authored view initializers.

## Key Namespaces

- `Terminal.Gui.Stanza.Binding` — `BindingContext` and `BindingExtensions` (The MVVM glue).
- `Terminal.Gui.Stanza.Layout` — C# 14 Extension properties for `Terminal.Gui.View` (The Layout DSL).
- `Terminal.Gui.Stanza.Generators.IR` — The Intermediate Representation models that decouple TUI logic from Roslyn symbols.

## ADR Index (Architecture Decision Records)

Located in `docs/adr/`:

- `0001-source-generator-architecture.md` — Project separation and IR pattern.
- `0002-mvvm-binding-logic.md` — `BindingContext` and lifecycle management.
- `0003-hybrid-declaration-paradigm.md` — Combining attributes with C# extension members.
- `0004-centralized-style-system.md` — Deferred centralized style system.
- `0005-dependency-graph-initialization.md` — Automated view instantiation ordering based on layout relationships.
- `0006-view-model-discovery-and-lifecycle.md` — View-model inference and generated lifecycle semantics.
- `0007-nested-subview-composition.md` — Nested view composition and shared `ViewModel` propagation.
- `0008-generator-validation-and-diagnostics.md` — Test strategy and current diagnostics policy.

## Coding Guidelines

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

- State your assumptions explicitly.
- If a future style system is added, document precedence explicitly (local property assignment should win over style-provided values).

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
