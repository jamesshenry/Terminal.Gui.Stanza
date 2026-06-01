To transition from your current base-class-inheritance model to a C# 13 "Trait/Mixin" model utilizing the refactored `BindingExtensions`, follow these five steps.

---

### 1. Update Abstractions (The Contract)

Replace the physical `BindableView<T>` class with a marker interface. This interface serves as the API contract for both the user and the Source Generator.

* **Action:** Delete `src\Terminal.Gui.Stanza\BindableView.cs`.
* **Action:** Create `src\Terminal.Gui.Stanza.Abstractions\IStanzaView.cs`:

```csharp
namespace Terminal.Gui.Stanza.Abstractions;

public interface IStanzaView<TViewModel> : IDisposable 
    where TViewModel : System.ComponentModel.INotifyPropertyChanged
{
    TViewModel? ViewModel { get; set; }
    Terminal.Gui.Stanza.Binding.BindingContext BindingContext { get; }``
}
```

---

### 2. Upgrade the Parser (Constructor & Metadata Awareness)

The generator needs to analyze the target class to decide which members to "inject" without causing compiler errors.

* **Action:** Update `TuiViewParser.cs` to identify existing constructors:

```csharp
// Inside TuiViewParser.cs
bool hasParameterlessCtor = classSymbol.InstanceConstructors
    .Any(c => c.Parameters.Length == 0 && !c.IsImplicitlyDeclared);

bool hasViewModelCtor = classSymbol.InstanceConstructors
    .Any(c => c.Parameters.Length == 1 && 
         SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, vmSymbol));
```

---

### 3. Refactor the Emitter (The Mixin & Refactored Extensions)

The emitter now uses C# 13 `field` and targets the simplified `ApplyBind...` extensions. This reduces the generated code volume and centralizes logic in your library.

* **Action:** Update `InitializeComponentEmitter.cs` to emit these specific blocks:

**The Mixin State & Helper:**

```csharp
// Using C# 13 'field' keyword
public TViewModel? ViewModel {
    get => field;
    set {
        field = value;
        if (value != null) InitializeComponent();
    }
}

private readonly BindingContext _bindingContext = new();
public BindingContext BindingContext => _bindingContext;

// User-facing helper for manual bindings
protected void Bind<T>(Func<T> getter, Action<T> update, [CallerArgumentExpression(nameof(getter))] string expr = "") 
    => _bindingContext.AddBinding(this.ViewModel.Bind(expr, getter, update));

public void Dispose() {
    _bindingContext.Dispose();
    base.Dispose(); // Terminal.Gui.View.Dispose()
}
```

**The Generated Wiring (inside InitializeComponent):**
Update the emitter to use the refactored `Apply` methods. This eliminates the need for the generator to handle `TextField` vs `View` logic or reentrancy checks manually.

```csharp
// Example generated wiring for a CheckBox
BindingContext.AddBinding(MyCheckBox.ApplyBindChecked(this.ViewModel!, nameof(ViewModel.IsActive), () => ViewModel.IsActive, v => ViewModel.IsActive = v));

// Example generated wiring for a TextField/Label
BindingContext.AddBinding(NameInput.ApplyBindText(this.ViewModel!, nameof(ViewModel.Name), () => ViewModel.Name, v => ViewModel.Name = v));
```

---

### 4. Extensive Testing Strategy

With behavior now injected as a trait, verify the generator handles various class structures.

* **Test 1: Smart Constructor Inclusion.**
    Verify that if a user writes `public MyView() { ... }`, the generator **skips** emitting the parameterless constructor but **still emits** the `MyView(TViewModel vm)` constructor.
* **Test 2: Reentrancy Protection.**
    Using a `TextField` and a `ViewModel`, verify that updating the property doesn't trigger an infinite loop (proving `BindTwoWay` in the extensions is working).
* **Test 3: Cleanup.**
    Verify that calling `view.Dispose()` clears the `BindingContext` and that `PropertyChanged` handlers are detached from the ViewModel.
* **Action:** Update `GeneratorSnapshotTests.cs` to verify these patterns.

---

### 5. Final Integration & Cleanup

Update the environment to support the new syntax and extensions.

* **Action:** Update all `.csproj` files to include `<LangVersion>latest</LangVersion>` (required for `field`).
* **Action:** Replace the old `BindingExtensions.cs` with the refactored version containing `BindOneWay`, `BindTwoWay`, and the `Apply...` targets.
* **Action:** Refactor `E2EStanzaTests.cs` to prove inheritance freedom:

```csharp
[TuiView<SimpleViewModel>]
public partial class SimpleFormView : Window // No longer BindableView<T>
{
    public CheckBox ActiveCheck { get; set; } = new() {
        BindChecked = nameof(SimpleViewModel.IsActive)
    };
}
```

---

### Summary of Change

| Feature | Current State | New State |
| :--- | :--- | :--- |
| **Inheritance** | Rigid `BindableView<T>` base class. | Flexible; inherit any `Terminal.Gui` type. |
| **Backing Fields** | Manual `_viewModel` field. | C# 13 `field` keyword in property. |
| **Binding Logic** | Duplicated logic for every UI type. | Unified `BindTwoWay` master logic. |
| **UI Quirks** | Generator handles `TextField` vs `View`. | `ApplyBindText` abstracts UI differences. |
| **Lifecycle** | Inherited `Dispose`. | Generated `Dispose` that cleans up `BindingContext`. |
