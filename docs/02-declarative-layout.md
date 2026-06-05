# The Declarative Layout DSL

## 1. Extension-Driven Syntax

Stanza utilizes C# **Extensions (Roles)** to inject synthetic layout and binding properties directly into Terminal.Gui's base `View`.

- **Object Initializers**: Developers define layouts and bindings in the same initialization block as native properties (e.g., `Text`, `Color`).
- **No Inheritance**: Eliminates the need for custom wrapper classes (like `StanzaTextField`).

## 2. Polymorphic Constraints (Discriminated Unions)

Layout positioning and sizing use **C# Discriminated Unions** to provide type-safe flexibility without cluttering the API.

- **The `Anchor` Union (`Below`, `RightOf`, etc.)**:
  - `Target(string ViewName)`: Anchors to the edge of another view.
  - `TargetOffset(string ViewName, int Offset)`: Anchors to another view with a specific margin.
- **The `Sizing` Union (`WidthSize`, `HeightSize`)**:
  - `Fill(int Margin)`: Fills remaining space (`Dim.Fill`).
  - `Percent(int Value)`: Fills a ratio of the parent (`Dim.Percent`).
  - `Absolute(int Value)`: Sets an exact dimension (`Dim.Sized`).

## 3. Synthetic Binding Markers

String-based properties that act strictly as metadata for the Source Generator.

- **Data & State**: `BindText`, `BindChecked`, `BindVisible`, `BindEnabled`.
- **Commands**: `BindCommand`.
- **Refactoring Safety**: Designed to be used exclusively with `nameof(ViewModel.Property)` to ensure compile-time safety and rename support.

## 4. Strict Generator Dependency

The declarative DSL **cannot be used standalone** at runtime; it relies entirely on the Source Generator.

- **Metadata Only**: The properties defined in the extension hold no state and execute no logic at runtime.
- **String-to-Instance Resolution**: To remain reflection-free and NativeAOT compatible, the runtime cannot map a string (like `nameof(Header)`) to an object instance. The generator is required to rewrite these string markers into direct variable references (e.g., `Pos.Bottom(Header)`) at compile time.
