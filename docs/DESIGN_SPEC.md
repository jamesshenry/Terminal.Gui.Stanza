# DESIGN_SPEC

This document defines the mapping between the TuiGenerated Attributes and Terminal.Gui v2 code.

## 1. View Definition

- **Attribute**: `[TuiView]` (applied to class)
- **Parameters**:
  - `Title`: Static string for the window title.
  - `BindTitle`: Name of a ViewModel property to bind the title to dynamically.
- **Rule**: Must be a `partial class` inheriting from `BindableView<T>`.

## 2. Layout Mapping (`[Layout]`)

The generator maps the `Anchor` enum and relative properties to `Terminal.Gui.Pos` and `Dim`.

| Parameter | Terminal.Gui v2 Output |
| :--- | :--- |
| `X = Anchor.Center` | `X = Pos.Center()` |
| `X = Anchor.Start` | `X = 0` |
| `X = Anchor.End` | `X = Pos.AnchorEnd(offset)` |
| `Below = nameof(T)` | `Y = Pos.Bottom(T) + Spacing` |
| `RightOf = nameof(T)` | `X = Pos.Right(T) + Spacing` |
| `Width = Anchor.Fill` | `Width = Dim.Fill()` |

## 3. Data Binding Mapping (Hybrid Model)

The Generator recognizes assignments to "Synthetic" Extension Properties inside View initializers. It maps these to the `BindingExtensions.cs` library.

### 3.1 Mapping Table

| Extension Property | Supported Views | Logic |
| :--- | :--- | :--- |
| `BindText` | Label, TextField, TextView | One-way (Label) or Two-Way (Fields) string binding. |
| `BindValue` | CheckBox, Slider, ProgressBar | Maps to the primary "Value" or "Checked" state. |
| `BindCommand` | Button, MenuItem | Maps the view's 'Accept' or 'Trigger' event to an IRelayCommand. |
| `BindVisible` | All Views | Toggles `Visible` property based on a boolean VM property. |
| `BindEnabled` | All Views | Toggles `Enabled` property based on a boolean VM property. |

### 3.2 Binding Syntax Rules

1. Must use `nameof(vm.Property)` or `nameof(ViewModel.Property)` to ensure refactoring support.
2. The Generator verifies the property exists on the generic `TViewModel` at compile time.
3. The Generator automatically handles `SetNeedsDraw()` calls within the generated binding update actions.

## 4. Hierarchy Management

- **Automatic Addition**: Any property decorated with `[Layout]` is automatically passed to `this.Add()`.
- **Tab Management**:
  - `[Tab(nameof(TabViewName), Title="...")]`
  - The generator wraps the view in a `Terminal.Gui.Tab` object and adds it to the specified `TabView` property.

### 5. Property Handling Logic

The Generator follows a hierarchy for property assignment:

1. **Direct Passthrough**: Properties like `Text`, `Secret`, or `ReadOnly` are emitted as direct assignments.
2. **Object Flattening**: Attributes for `Padding`, `Margin`, and `Border` are expanded into their respective object initializers (`new Thickness(...)`).
3. **Reactive Binding**: Any property prefixed with `Bind` (e.g., `BindEnabled`) triggers the emission of an MVVM `PropertyChanged` subscription.
4. **Styles**: A `[Style]` attribute applies a pre-defined "Bundle" of properties, allowing for project-wide UI consistency.

## 6. Style System

The generator supports a centralized Design System to minimize property duplication.

### 6.1 Defining Styles

- Users define styles in a class marked `[StyleProvider]`.
- Styles are properties returning `ViewStyle` (a generator-internal IR type).
- Supported properties: `Padding`, `Margin`, `Border`, `ColorScheme`, `TextAlignment`, `CanFocus`, etc.

### 6.2 Applying Styles

- **Attribute**: `[Style(string styleName)]`
- **Behavior**: The generator performs a "Compile-time Splat." It takes every property defined in the style and emits it as an assignment in the target view's initialization.
- **Precedence**: Local attributes (e.g., a `[Layout]` attribute on the property) always override settings provided by a `[Style]`.
