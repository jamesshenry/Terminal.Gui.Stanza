# The Authoring Model

## 1. Native Terminal.Gui Layouts

Stanza does not reinvent the layout wheel. Developers use standard Terminal.Gui `Pos` and `Dim` objects inside standard C# constructors or object initializers to build the UI structure.

This ensures that developers have full access to Terminal.Gui's native feature set without fighting a Source Generator or learning a custom DSL.

## 2. Declarative Binding Attributes

To connect the UI to the ViewModel, Stanza uses C# Attributes. This cleanly separates the visual layout of the view from the data-flow logic.

- **Data & State**: `[BindText]`, `[BindChecked]`, `[BindVisible]`, `[BindEnabled]`.
- **Commands**: `[BindCommand]`.
- **Refactoring Safety**: Attributes are designed to be used with the `nameof()` operator (e.g., `[BindText(nameof(ViewModel.UserName))]`) to ensure compile-time safety and rename support.

## 3. Advanced Binding Configuration

Attributes support optional parameters to fine-tune binding behavior directly at the declaration site:

- **Mode**: Explicitly set `BindingMode.OneWay` or `BindingMode.TwoWay`. (e.g., `[BindText(nameof(ViewModel.Hash), Mode = BindingMode.OneWay)]`).
- **Diagnostics**: The generator actively analyzes the target ViewModel. If a developer requests a `TwoWay` binding on a read-only property, the generator will issue a compile-time diagnostic warning or error (`STN004`/`STN005`).
