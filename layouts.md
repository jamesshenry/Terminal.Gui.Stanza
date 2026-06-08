# layouts

That syntax is a great start toward a "Fluent Layout" for Terminal.Gui v2. Terminal.Gui's `Pos` and `Dim` systems are powerful but can become verbose. A layout builder can make the "Arrange" phase of your views much more readable.

To make this work well with **Stanza** and **NativeAOT**, we should aim for a design that avoids complex expression trees and stays close to the underlying `Pos` and `Dim` types.

### A Proposed Design for the Stanza Layout Builder

Here is how you could implement that DSL to be both clean and performant:

```csharp
namespace Stanza.TerminalGui;

// 1. Core types for the DSL
public record struct XY(Pos? X = null, Pos? Y = null);
public record struct Size(Dim? Width = null, Dim? Height = null);

public class LayoutBuilder
{
    private readonly View _parent;

    public LayoutBuilder(View parent) => _parent = parent;

    public LayoutBuilder Add(View child, XY pos = default, Size size = default)
    {
        if (pos.X != null) child.X = pos.X;
        if (pos.Y != null) child.Y = pos.Y;
        if (size.Width != null) child.Width = size.Width;
        if (size.Height != null) child.Height = size.Height;

        _parent.Add(child);
        return this;
    }

    // Helper to finish the chain
    public View Build() => _parent;
}

// 2. Extension method to start the chain
public static class LayoutExtensions
{
    public static LayoutBuilder Layout(this View view) => new(view);
}
```

### How it looks in your View

Using C# **Target-typed new** and **Named Arguments**, the syntax becomes very elegant:

```csharp
public StatsView(StatsViewModel viewModel)
{
    _statsLabel = new Label();
    _sourceLabel = new Label();

    this.Layout()
        .Add(_statsLabel, 
            pos:  new(X: Pos.Center(), Y: Pos.Center()), 
            size: new(Width: Dim.Fill(), Height: 1))
        .Add(_sourceLabel, 
            pos:  new(Y: Pos.Bottom(_statsLabel)), 
            size: new(Width: Dim.Auto()))
        .Build();

    ViewModel = viewModel;
}
```

### Refining for your specific syntax

If you want to match your `new Width(76)` style exactly, you can provide explicit conversion operators or constructor helpers:

```csharp
public record struct Width(Dim Dim) {
    public static implicit operator Width(int value) => new(Dim.Absolute(value));
    public static implicit operator Width(Dim value) => new(value);
}

// Then AddView looks like:
public LayoutBuilder AddView(View v, XY pos, Width? width = null, Height? height = null) { ... }
```

### Why this fits Stanza well

1. **Readability:** It separates the *creation* of the view from the *arrangement* logic.
2. **Order of Operations:** By using a builder, you naturally group your `Add()` calls together. It makes it very obvious where `ViewModel = viewModel` should go (always after `.Build()`).
3. **No Reflection:** Since you are passing the `View` instances and `Pos`/`Dim` objects directly, it remains 100% compatible with NativeAOT and is extremely fast.

### Alternative: The "Static" Style

If you want a truly declarative "Single Expression" look, you could use an object initializer style:

```csharp
this.Add(new Label {
    Text = "Stats",
    X = Pos.Center(),
    Y = Pos.Center(),
    Width = Dim.Fill()
});
```

Here is how you can implement a curried/fluent layout system for **Stanza**.

### 1. The "With" Style (Currying/Fluent API)

Instead of a bulky `AddView` method with 10 parameters, you can create extension methods that return the View itself, allowing you to chain the configuration.

```csharp
namespace Stanza.TerminalGui;

public static class LayoutExtensions
{
    // Sets Position (Curried)
    public static T WithPos<T>(this T view, Pos? x = null, Pos? y = null) where T : View
    {
        if (x != null) view.X = x;
        if (y != null) view.Y = y;
        return view;
    }

    // Sets Size (Curried)
    public static T WithSize<T>(this T view, Dim? width = null, Dim? height = null) where T : View
    {
        if (width != null) view.Width = width;
        if (height != null) view.Height = height;
        return view;
    }

    // Curried "AddTo" to finish the chain
    public static T AddTo<T>(this T child, View parent) where T : View
    {
        parent.Add(child);
        return child;
    }
}
```

### 2. How it looks in your Code

This approach allows you to "configure" the control and "add" it to the parent in a single readable line:

```csharp
public StatsView(StatsViewModel viewModel)
{
    // Create and Arrange in one fluent motion
    _statsLabel = new Label { Text = "Loading..." }
        .WithPos(x: Pos.Center(), y: Pos.Center())
        .WithSize(width: Dim.Fill(), height: 1)
        .AddTo(this);

    _sourceLabel = new Label()
        .WithPos(y: Pos.Bottom(_statsLabel))
        .WithSize(width: Dim.Auto())
        .AddTo(this);

    // Now set the ViewModel (Order of operations is safe!)
    ViewModel = viewModel;
}
```

### 3. Why this is superior for Stanza

1. **Direct Reference:** You keep the reference to `_statsLabel` clearly.
2. **No "Builder" Overhead:** There is no intermediate `LayoutBuilder` object; you are just calling tiny extension methods on the View itself.
3. **Target-Typed Convenience:** You can use `Pos.Center()` and named arguments to keep it concise.
4. **NativeAOT Friendly:** Since these are just simple generic methods with no reflection or expression trees, the compiler inlines them, and they are essentially zero-cost.

### 4. Taking it further: Helper Records

If you want the specific `XY` syntax from your earlier thought, you can combine currying with that:

```csharp
public static T At<T>(this T view, Pos x, Pos y) where T : View 
    => view.WithPos(x, y);

// Usage:
_statsLabel = new Label().At(Pos.Center(), Pos.Center()).AddTo(this);
```

### Recommendation

The **`WithPos` / `WithSize` / `AddTo`** pattern is usually the favorite among C# developers because it doesn't hide the underlying Terminal.Gui properties—it just makes them easier to set in a chain. It also makes the "Constructor Order of Operations" issue we discussed earlier very easy to spot and manage.

### 1. Using in a Switch Expression

This is perfect for a `ViewLocator` or a dynamic UI where the layout changes based on a state (like `Mode` or `Orientation`):

```csharp
var layoutMode = ViewLayout.Centered;

_statsLabel = layoutMode switch
{
    ViewLayout.Centered => new Label("Stats")
        .WithPos(Pos.Center(), Pos.Center())
        .WithSize(Dim.Fill(), 1),

    ViewLayout.Compact => new Label("S:")
        .WithPos(0, 0)
        .WithSize(10, 1),

    _ => throw new ArgumentOutOfRangeException()
};

this.Add(_statsLabel);
```

### 2. Using with Target-Typed `new()`

Since the methods return the specific type (via the `where T : View` generic constraint), you don't lose the specific properties of the control (like `Label.Text` or `Button.Text`):

```csharp
public MainView()
{
    // The type is inferred as Button, so you don't lose the Button-specific members
    _submitBtn = new Button()
        .WithPos(Pos.Center(), Pos.AnchorEnd(1))
        .WithSize(10, 3)
        .AddTo(this);
}
```

### 3. Expression-Bodied Factory Methods

If you have repetitive UI components, you can turn them into "Curried Factories":

```csharp
private Label CreateHeader(string text) => new Label(text)
    .WithPos(x: 0, y: 0)
    .WithSize(Dim.Fill(), 1)
    .AddTo(this);

// Usage in Constructor:
_titleLabel = CreateHeader("Dashboard");
```

### 4. Implementation Detail: Generics are Key

To make sure this works in switch expressions without losing the original type (e.g., keeping it a `Button` instead of casting it to a base `View`), your extensions **must** be generic:

```csharp
// Use <T> and return T to preserve the specific class type through the chain
public static T WithPos<T>(this T view, Pos? x = null, Pos? y = null) where T : View
{
    if (x != null) view.X = x;
    if (y != null) view.Y = y;
    return view;
}
```

### Why this is a "Win" for Stanza

* **Declarative:** It feels like SwiftUI or Flutter but generates standard C# calls.
* **Order of Operations:** You can create, layout, and add the view in a single expression, then immediately follow it with `ViewModel = vm`.
* **No intermediate state:** You aren't storing half-configured views in local variables; the switch expression evaluates to a fully-ready-to-go View.

**One tip:** If you use this heavily in switch expressions, ensure you add the `AddTo(this)` call at the very end of the expression or right after it, so you don't forget to actually attach the view to the hierarchy!
