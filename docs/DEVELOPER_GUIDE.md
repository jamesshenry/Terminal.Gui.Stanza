# Terminal.Gui.Stanza Developer Guide

## Overview

**Terminal.Gui.Stanza** is a declarative, attribute-based UI engine for Terminal.Gui v2. It eliminates "glue code" by transforming imperative Terminal.Gui patterns into a schema-driven framework powered by .NET Source Generators and C# 14/15.

The core problem Stanza solves:

1. **The Reference Problem**: C# field/property initializers cannot reference other instance members (e.g., `Y = Pos.Bottom(lbl)` fails during initialization).
2. **The Binding Gap**: Terminal.Gui lacks a native dependency-property system for MVVM data binding.
3. **The Design System**: No way to define and reuse UI "stanzas" (theme bundles) at compile time.

**Solution**: A three-stage pipeline that runs at compile time:

- **Parser** (Roslyn) → extracts `[TuiView]` attributes and generates IR models
- **Dependency Resolver** → topologically sorts views by layout constraints
- **Emitter** → generates a `partial InitializeComponent()` method with all initialization logic

The generated method is placed in a `.g.cs` file alongside the user-written declaration, completing the partial class.

---

## Architecture Overview

### The Three-Stage Pipeline

```
User Code (.cs)               Parser                     Emitter
    ↓                            ↓                          ↓
[TuiView] class          IR Models (Records)      Generated .g.cs
Properties + Bindings    ↓                        InitializeComponent()
Layout References    DependencyResolver              ↓
                         (Topological Sort)      User's partial class
                         ↓
                    Instantiation Order
```

### Project Structure

| Project | Purpose |
|---------|---------|
| [Abstractions](../src/Terminal.Gui.Stanza.Abstractions) | Marker attributes, IR models, layout enums (zero dependencies) |
| [Generators](../src/Terminal.Gui.Stanza.Generators) | Roslyn `IIncrementalGenerator` with Parser, Resolver, Emitter |
| [Core (Stanza)](../src/Terminal.Gui.Stanza) | Runtime: `BindableView<T>`, `BindingContext`, extension methods |
| [Tests](../src/Terminal.Gui.Stanza.Generators.Tests) | Snapshot tests verifying generated code against verified outputs |

---

## The Complete Transformation Pipeline

### 1. Input: User-Written View Declaration

The user creates a partial class decorated with `[TuiView]`:

```csharp
// User writes:
[TuiView]
public partial class MyView : BindableView<MyViewModel>
{
    public Label MyLabel { get; set; } = new() { Text = "Hello" };
}
```

**Reference**: [Generator test input](GeneratorSnapshotTests.cs#L17-L27)

### 2. Parser: Extract IR Models

The [TuiViewGenerator](../src/Terminal.Gui.Stanza.Generators/TuiViewGenerator.cs#L11-L60) orchestrates the pipeline:

| Stage | Link | Purpose |
|-------|------|---------|
| **Syntax Detection** | [TuiViewGenerator.Initialize()](../src/Terminal.Gui.Stanza.Generators/TuiViewGenerator.cs#L12-L18) | Use Roslyn's `SyntaxProvider` to find classes with `[TuiView]` attribute |
| **Symbol Resolution** | [TuiViewGenerator.Initialize()](../src/Terminal.Gui.Stanza.Generators/TuiViewGenerator.cs#L26-L27) | Resolve `TuiViewAttribute` and `TuiViewAttribute<T>` types from compilation |
| **Parser Instantiation** | [TuiViewGenerator.Initialize()](../src/Terminal.Gui.Stanza.Generators/TuiViewGenerator.cs#L29-L31) | Create `TuiViewParser`, `DependencyResolver`, and `InitializeComponentEmitter` |
| **IR Generation** | [TuiViewParser.Parse()](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L22-L25) | Extract attributes, properties, bindings → produce `ViewDeclaration` IR |

#### Parser Deep-Dive

The [TuiViewParser](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs) walks the decorated class and builds IR models:

**What the parser extracts:**

| Element | Parser Logic | IR Model |
|---------|--------------|----------|
| **Class metadata** | [Lines 26-30](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L26-L30) | `ViewDeclaration.ClassName`, `.Namespace`, `.BaseType` |
| **ViewModel type** | [Lines 36-55](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L36-L55) | `ViewDeclaration.ViewModelType` (resolved from constructor param, generic arg, or base type) |
| **Property assignments** | [Lines 72-76](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L72-L76) | `PropertyAssignment` records for each field/property initializer |
| **Binding extensions** | [Lines 128-135](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L128-L135) | `BindingInfo` records (e.g., `BindText = nameof(vm.Name)`) |
| **Layout constraints** | [Lines 136-151](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L136-L151) | `LayoutConstraint` records (e.g., `Y = Pos.Bottom(otherView)`) |

**Example property parsing:**

```csharp
// Input (in user's class initializer):
public Label MyLabel { get; set; } = new() { Text = "Hello" };

// Parser extracts:
PropertyAssignment("MyLabel", "Text", "\"Hello\"", isLayoutConstraint: false)
```

See [ParseMemberInitializer()](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L155-L205) for the full logic.

### 3. Dependency Resolver: Topological Sort

The [DependencyResolver](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs) ensures views are instantiated in the correct order:

**Problem**: If `ViewB.Y = Pos.Bottom(ViewA)`, then `ViewA` must be instantiated first. The parser extracts these dependencies; the resolver orders views.

**Algorithm**: [Kahn's topological sort](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs#L13-L49):

| Step | Code Link | Action |
|------|-----------|--------|
| **Build graph** | [Lines 16-27](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs#L16-L27) | Create adjacency list: `view → [dependent views]` |
| **In-degree calculation** | [Lines 16-27](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs#L16-L27) | Track how many views each depends on |
| **Queue initialization** | [Lines 30-35](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs#L30-L35) | Start with zero-dependency views |
| **Topological sort** | [Lines 37-46](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs#L37-L46) | Dequeue views, decrement dependents' in-degrees, enqueue when zero |
| **Circular dependency handling** | [Lines 48-55](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs#L48-L55) | If sort incomplete, append missing views (error case) |

**Example:**

```
Input constraints:
  ViewB.Y = Pos.Bottom(ViewA)

Dependency graph:
  ViewA (in-degree 0) → can instantiate immediately
  ViewB (in-degree 1) → depends on ViewA

Output order: [ViewA, ViewB]
```

### 4. Emitter: Generate InitializeComponent()

The [InitializeComponentEmitter](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs#L8-10) generates the complete method:

**Generated structure** (4 phases):

| Phase | Code Link | Purpose |
|-------|-----------|---------|
| **1. Instantiate controls** | [Lines 27-30](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs#L27-L30) | Create views in dependency order using `??=` (coalesce-assign) |
| **2. Apply properties and layout** | [Lines 32-47](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs#L32-L47) | Set string properties (e.g., `Text`, `Width`) and layout constraints |
| **3. Wire bindings** | [Lines 49-68](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs#L49-L68) | Call `BindingContext.AddBinding()` for data bindings |
| **4. Add to view hierarchy** | [Lines 70-74](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs#L70-L74) | Add views to parent via `this.Add(viewName)` |

**Generated code signature:**

```csharp
partial class MyView : Terminal.Gui.Stanza.BindableView<MyViewModel>
{
    public MyView() : base() { }
    public MyView(MyViewModel viewModel) : base(viewModel) { }

    protected override void InitializeComponent()
    {
        // 1. Instantiate controls
        MyLabel ??= new();
        
        // 2. Apply properties and layout
        MyLabel.Text = "Hello";
        
        // 3. Wire bindings
        // (if any exist)
        
        // 4. Add to view hierarchy
        this.Add(MyLabel);
    }
}
```

See [Emit()](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs#L8-10) for full implementation.

### 5. Output: Generated .g.cs File

The generator registers the source with [AddSource()](../src/Terminal.Gui.Stanza.Generators/TuiViewGenerator.cs#L55):

```csharp
spc.AddSource($"{viewDecl.ClassName}.g.cs", sourceCode);
```

**Verified snapshot**: [MyView.g.verified.cs](../src/Terminal.Gui.Stanza.Generators.Tests/GeneratorSnapshotTests.GeneratesCorrectly%23MyView.g.verified.cs)

The `.g.cs` file is placed alongside the original `.cs` file, and the partial class merges at compile time.

---

## Key Concepts

### Intermediate Representation (IR) Models

The IR decouples generator logic from Roslyn symbols. All IR models are immutable records in [Terminal.Gui.Stanza.Abstractions.IR](../src/Terminal.Gui.Stanza.Abstractions/IR):

| Model | Purpose | Link |
|-------|---------|------|
| `ViewDeclaration` | Metadata for the entire view class | [ViewDeclaration.cs](../src/Terminal.Gui.Stanza.Abstractions/IR/ViewDeclaration.cs) |
| `PropertyAssignment` | A property setting (e.g., `Text = "Hello"`) | [PropertyAssignment.cs](../src/Terminal.Gui.Stanza.Abstractions/IR/PropertyAssignment.cs) |
| `LayoutConstraint` | A relative layout reference (e.g., `Y = Pos.Bottom(other)`) | [LayoutConstraint.cs](../src/Terminal.Gui.Stanza.Abstractions/IR/LayoutConstraint.cs) |
| `BindingInfo` | A data binding (e.g., `BindText = nameof(vm.Text)`) | [BindingInfo.cs](../src/Terminal.Gui.Stanza.Abstractions/IR/BindingInfo.cs) |

**Why IR matters:**

- Parser converts Roslyn symbols → IR records (no dependency on Roslyn in later stages)
- Easier to test: create IR records directly without parsing source
- Flexible: future emitters (XAML, JSON) reuse same IR

### Layout Constraints

A layout constraint captures a relative positioning relationship:

```csharp
public record LayoutConstraint(
    string SourceView,        // "ViewB"
    string TargetProperty,    // "Y"
    string ConstraintType,    // "Bottom" or "Right"
    string ReferencedView     // "ViewA"
);
```

**Interpretation**: `SourceView.TargetProperty = Pos.{ConstraintType}(ReferencedView)`

Example: `LayoutConstraint("ViewB", "Y", "Bottom", "ViewA")` → `ViewB.Y = Pos.Bottom(ViewA)`

### Binding Modes

Bindings can be one-way (VM → UI) or two-way (VM ↔ UI):

| Mode | Data Flow | Use Case |
|------|-----------|----------|
| `OneWay` | VM → UI | Read-only UI (labels) |
| `TwoWay` | VM ↔ UI | Editable fields (text boxes) |

See [BindingInfo](../src/Terminal.Gui.Stanza.Abstractions/IR/BindingInfo.cs) and [BindingMode enum](../src/Terminal.Gui.Stanza.Abstractions/BindingMode.cs).

---

## Transformation Map: Input → Output

### Example 1: Simple Label

**Input user code:**

```csharp
[TuiView]
public partial class MyView : BindableView<MyViewModel>
{
    public Label MyLabel { get; set; } = new() { Text = "Hello" };
}
```

**Parser extraction:**

- Class: `MyView` in namespace `...`
- Property: `MyLabel`
- Assignment: `PropertyAssignment("MyLabel", "Text", "\"Hello\"", isLayoutConstraint: false)`

**Dependency resolution:**

- No constraints → `[MyLabel]` (single-item list)

**Generated code:**

```csharp
protected override void InitializeComponent()
{
    MyLabel ??= new();
    MyLabel.Text = "Hello";
    this.Add(MyLabel);
}
```

**Reference**: [Snapshot test](../src/Terminal.Gui.Stanza.Generators.Tests/GeneratorSnapshotTests.GeneratesCorrectly%23MyView.g.verified.cs)

---

### Example 2: Relative Layout (Multiple Views)

**Input user code:**

```csharp
[TuiView]
public partial class MyView : BindableView<MyViewModel>
{
    public Label Title { get; set; } = new() { Text = "Title" };
    public TextBox Input { get; set; } = new() 
    { 
        Y = Pos.Bottom(Title),
        Width = 30
    };
}
```

**Parser extraction:**

- Class: `MyView`
- Assignments:
  - `PropertyAssignment("Title", "Text", "\"Title\"")`
  - `PropertyAssignment("Input", "Width", "30")`
  - `PropertyAssignment("Input", "Y", "Pos.Bottom(Title)", isLayoutConstraint: true)`
- Constraint:
  - `LayoutConstraint("Input", "Y", "Bottom", "Title")`

**Dependency resolution:**

- Graph: Title (in-degree 0) → Input (in-degree 1)
- Order: `[Title, Input]`

**Generated code:**

```csharp
protected override void InitializeComponent()
{
    // 1. Instantiate controls
    Title ??= new();
    Input ??= new();
    
    // 2. Apply properties and layout
    Title.Text = "Title";
    Input.Width = 30;
    Input.Y = Pos.Bottom(Title);
    
    // 4. Add to view hierarchy
    this.Add(Title);
    this.Add(Input);
}
```

---

## Running the Generator

### Enable Generated File Emission

The demo project already has this enabled:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

This tells the compiler to write `.g.cs` files to disk (usually under `obj/Debug/net11.0/generated/`), making them visible for inspection.

### Build and Inspect Generated Files

```bash
# Build the solution
dotnet build Terminal.Gui.Stanza.slnx

# Generated files are written to:
# samples/Terminal.Gui.Stanza.Demo/obj/Debug/net11.0/generated/Terminal.Gui.Stanza.Generators/Terminal.Gui.Stanza.Generators.TuiViewGenerator/*.g.cs
```

In VS Code, you can use **Go to Definition** (`F12`) on the partial class name to jump to the generated `.g.cs` file.

---

## Troubleshooting

### Issue: Generated code not appearing

**Cause**: `EmitCompilerGeneratedFiles` not enabled in `.csproj`

**Fix**: Add to your project file:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Then rebuild: `dotnet clean && dotnet build`

**Reference**: [Demo project configuration](samples/Terminal.Gui.Stanza.Demo/Terminal.Gui.Stanza.Demo.csproj#L7)

---

### Issue: "Cannot reference view 'OtherView' in property initializer"

**Cause**: Parser found a reference like `Y = Pos.Bottom(OtherView)` but `OtherView` doesn't exist in the class.

**Debug steps:**

1. Check [DependencyResolver.ResolveOrder()](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs#L13-L49) — look for orphaned views
2. Verify the referenced view name matches exactly (case-sensitive)
3. Check [TuiViewParser.ParseMemberInitializer()](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L155-L205) for how references are extracted

---

### Issue: Circular dependency detected

**Cause**: Layout constraints form a cycle (View A depends on View B, View B on View A).

**Fix**: Refactor the layout to break the cycle. For example:

```csharp
// ❌ Circular:
// A.Y = Pos.Bottom(B)
// B.Y = Pos.Bottom(A)

// ✅ Fixed:
// A.Y = 0
// B.Y = Pos.Bottom(A)
```

**Reference**: [Circular dependency handling](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs#L48-L55)

---

## Testing the Generator

### Snapshot Testing

The test suite uses [Verify.SourceGenerators](../src/Terminal.Gui.Stanza.Generators.Tests/GeneratorSnapshotTests.cs) to verify generated code:

```csharp
[Test]
public Task GeneratesCorrectly()
{
    var source = """
    [TuiView]
    public partial class MyView : BindableView<MyViewModel>
    {
        public Label MyLabel { get; set; } = new() { Text = "Hello" };
    }
    """;
    
    return TestHelper.Verify(source);
}
```

**How it works:**

1. Parse input source code with Roslyn
2. Run `TuiViewGenerator` against the parse tree
3. Capture generated output
4. Compare against `.verified.cs` snapshot file
5. If mismatch, save diff for review

**To update snapshots after intentional changes:**

```bash
cd src/Terminal.Gui.Stanza.Generators.Tests
dotnet test --filter GeneratorSnapshotTests -- --accept
```

**Reference**: [GeneratorSnapshotTests.cs](../src/Terminal.Gui.Stanza.Generators.Tests/GeneratorSnapshotTests.cs)

---

## Key Files Reference

| File | Purpose |
|------|---------|
| [TuiViewGenerator.cs](../src/Terminal.Gui.Stanza.Generators/TuiViewGenerator.cs) | Main `IIncrementalGenerator` entry point; orchestrates pipeline |
| [TuiViewParser.cs](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs) | Extracts [TuiView] attributes → IR models |
| [DependencyResolver.cs](../src/Terminal.Gui.Stanza.Generators/DependencyResolver.cs) | Topologically sorts views by layout constraints |
| [InitializeComponentEmitter.cs](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs) | Generates C# code for InitializeComponent() |
| [ViewDeclaration.cs](../src/Terminal.Gui.Stanza/IR/ViewDeclaration.cs) | IR model for a decorated view class |
| [LayoutConstraint.cs](../src/Terminal.Gui.Stanza/IR/LayoutConstraint.cs) | IR model for relative layout references |
| [PropertyAssignment.cs](../src/Terminal.Gui.Stanza/IR/PropertyAssignment.cs) | IR model for property initializers |
| [BindingInfo.cs](../src/Terminal.Gui.Stanza/IR/BindingInfo.cs) | IR model for MVVM data bindings |
| [TuiViewAttribute.cs](../src/Terminal.Gui.Stanza/TuiViewAttribute.cs) | Marker attribute and reserved metadata contract for generated views |
| [GeneratorSnapshotTests.cs](../src/Terminal.Gui.Stanza.Generators.Tests/GeneratorSnapshotTests.cs) | Snapshot tests with verified outputs |

---

## Architecture Decision Records

For design rationale, see the ADR files in [docs/adr/](docs/adr):

- [0001-source-generator-architecture.md](docs/adr/0001-source-generator-architecture.md) — Project separation and IR pattern
- [0002-mvvm-binding-logic.md](docs/adr/0002-mvvm-binding-logic.md) — Binding lifecycle management
- [0003-hybrid-declaration-paradigm.md](docs/adr/0003-hybrid-declaration-paradigm.md) — Combining attributes with C# 14 extensions
- [0004-centralized-style-system.md](docs/adr/0004-centralized-style-system.md) — Deferred centralized style system
- [0005-dependency-graph-initialization.md](docs/adr/0005-dependency-graph-initialization.md) — View instantiation ordering
- [0006-view-model-discovery-and-lifecycle.md](docs/adr/0006-view-model-discovery-and-lifecycle.md) — View-model inference, generated constructors, and lifecycle semantics
- [0007-nested-subview-composition.md](docs/adr/0007-nested-subview-composition.md) — Nested view composition and `ViewModel` propagation
- [0008-generator-validation-and-diagnostics.md](docs/adr/0008-generator-validation-and-diagnostics.md) — Test strategy and current diagnostic policy

---

## Quick Start for Extending the Generator

### Adding a New Property Type

1. **Update the parser** to recognize the property in [TuiViewParser.ParseMemberInitializer()](../src/Terminal.Gui.Stanza.Generators/TuiViewParser.cs#L155-L205)
2. **Add to PropertyAssignment IR** — already a flexible string model
3. **Update the emitter** to generate assignment code in [InitializeComponentEmitter.Emit()](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs#L8-10)
4. **Add snapshot test** in [GeneratorSnapshotTests.cs](../src/Terminal.Gui.Stanza.Generators.Tests/GeneratorSnapshotTests.cs)

### Adding a New Binding Type

1. **Extend BindingInfo** if needed (already supports multiple binding modes)
2. **Update parser** to detect new binding syntax (e.g., `BindVisible = nameof(vm.IsVisible)`)
3. **Add emitter logic** in [InitializeComponentEmitter.Emit()](../src/Terminal.Gui.Stanza.Generators/InitializeComponentEmitter.cs#L49-L68) to call the appropriate `BindingContext.AddBinding()` method
4. **Test with snapshot**

---

## Performance Considerations

**At compile time:**

- Parser walks AST once per decorated class: O(n) where n = properties/fields
- Dependency resolver: O(v + e) where v = views, e = constraints (topological sort)
- Emitter: O(v) to generate code strings

**At runtime:**

- Generated code has zero reflection overhead — all wiring is direct method calls
- `BindingContext` uses `PropertyChanged` event subscription (no polling)
- View instantiation follows natural C# constructor semantics

**Generated code size:** ~50-100 lines per decorated view (varies with complexity)

---

## Contributing

When modifying the generator:

1. **Update IR first** if you're adding new concepts (e.g., new constraint types)
2. **Update parser** to extract the new IR data
3. **Add resolver logic** if dependency handling changes
4. **Update emitter** to generate code for the new IR
5. **Add/update snapshot tests** before committing

See [Coding Guidelines](AGENTS.md#coding-guidelines) in AGENTS.md for patterns.

---

## Further Reading

- [DESIGN_SPEC.md](DESIGN_SPEC.md) — System-level design and philosophy
- [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) — Phase breakdown and feature roadmap
- [.NET Roslyn Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) — Syntax tree and semantic model concepts
- [C# Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) — Generator lifecycle and debugging
