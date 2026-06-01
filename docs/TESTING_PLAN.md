# Testing Plan

## Phase 1: Lifecycle, Binding & Control Integrity (Foundation)

**Goal:** Establish a safe, state-driven lifecycle for ViewModel initialization, swapping, and core control bindings.

### 1.1 State Machine for the `ViewModel` Setter

The generated `ViewModel` setter must follow a deterministic state machine to handle transitions between active ViewModels and `null` safely.

* **Logic Specification:**

    ```csharp
    private BindingContext _bindingContext = new();
    public BindingContext BindingContext => _bindingContext;

    public TestNamespace.MyViewModel? ViewModel {
        get => field;
        set {
            if (ReferenceEquals(field, value)) return;

            // 1. Always dispose of the active bindings to prevent leaks
            _bindingContext?.Dispose();

            field = value;

            if (value != null) {
                // 2. State: Transition to a new VM
                _bindingContext = new BindingContext();
                InitializeComponent();
            } else {
                // 3. State: Transition to null
                // Re-create a fresh, empty BindingContext to prevent NullReferenceExceptions 
                // in code-behind that might still access it, but do NOT run InitializeComponent().
                _bindingContext = new BindingContext();
            }
        }
    }
    ```

* **Test Case (VM Swap Sequence):**
  * **Sequence:** Set `ViewModel = VM_A` -> Set `ViewModel = null` -> Set `ViewModel = VM_B`.
  * **Assertion:** Verify that changing properties on `VM_A` has no effect, while changing properties on `VM_B` updates the UI as expected. Verify no `ObjectDisposedException` is thrown during this recovery sequence.
* **Test Case (Null Detachment):**
  * **Sequence:** Set `ViewModel = VM_A` -> Set `ViewModel = null` -> Change a bound property on `VM_A`.
  * **Assertion:** Verify that the UI elements do not update (proving that all event handlers from `VM_A` were successfully unsubscribed).
* **Test Case (No-Op Reassignment):**
  * **Sequence:** Set `ViewModel = VM_A` -> Set `ViewModel = VM_A` again.
  * **Assertion:** Verify the setter exits early: no hierarchy changes, no duplicate bindings, and no re-run side effects from `InitializeComponent()`.

### 1.2 Non-Destructive Hierarchy Management

* **Implementation Change:** Avoid using `RemoveAll()`. Instead, use a check-before-add pattern in `InitializeComponentEmitter.cs`:

    ```csharp
    if (!this.Subviews.Contains(viewName)) { this.Add(viewName); }
    ```

* **Test Case (Regression):**
  * In the View's manual constructor, execute `this.Add(new Label("Manual"));`.
  * Assign a `ViewModel` (triggering `InitializeComponent`).
  * **Assertion:** Verify the manually added label is preserved in the `Subviews` collection alongside the generated views.

### 1.3 UI Control & DataType Binding Matrix

This section verifies that individual controls and data types bind correctly under both One-Way and Two-Way paradigms.

* **Test Case (`TextField` One-Way with Read-Only VM Property):**
  * **Precondition:** Bind a **read-only** string property of a VM (e.g., `public string Name { get; }`) to a `TextField` using `BindText`. This forces the parser to select `OneWay` binding.
  * **Sequence:** Change the VM property internally (triggering `PropertyChanged`). Programmatically change `textField.Value`.
  * **Assertion:** `textField.Value` reflects the new value. Changing `textField.Value` programmatically does **not** update the VM.
* **Test Case (`TextField` Two-Way):**
  * **Sequence:** Bind a writable string property of a VM to a `TextField` using `BindText` with a VM setter defined. Raise `textField.ValueChanged`.
  * **Assertion:** The VM property updates in response to the UI event.
* **Test Case (General View `Text` One-Way):**
  * **Sequence:** Bind a string property to a generic `Label` or `View`. Change VM property.
  * **Assertion:** Since the target view does not expose `Value`, the binding falls back to targeting the `Text` property.
* **Test Case (`CheckBox` State Synchronization):**
  * **Sequence:** Bind a boolean property to a `CheckBox` using `BindChecked`. Toggle VM between `true` and `false`.
  * **Assertion:** `checkBox.Value` maps correctly to `CheckState.Checked` and `CheckState.UnChecked`.
* **Test Case (Implicit String Conversion):**
  * **Sequence:** Bind an `int` property (e.g., `Score = 42`) to a `Label` using `BindText`.
  * **Assertion:** The generator detects the non-string type, flags `RequiresToString = true`, emits a `.ToString()` call, and updates `Label.Text` to `"42"`.

### 1.4 Command Interaction & Cleanup

Verifies how actions and permission states propagate through `ICommand` bindings.

* **Test Case (Command Trigger):**
  * **Sequence:** Bind a VM command to a `Button`. Trigger the button's `Accepting` event.
  * **Assertion:** The command executes once.
* **Test Case (Command Permission Prop):**
  * **Sequence:** Toggle the VM command's `CanExecute` capability.
  * **Assertion:** The button's `Enabled` state updates dynamically.
* **Test Case (Unsubscription on Dispose):**
  * **Sequence:** Dispose of the command binding's returned `IDisposable` and trigger `button.Accepting`.
  * **Assertion:** The command does not execute.

### 1.5 Memory Lifecycle & GC Safety

Ensures that active event handlers do not prevent views or ViewModels from being reclaimed by the garbage collector. This protocol is based on [GC1] and [GC2].

* **GC Collection Execution Protocol:**
  To guarantee deterministic collection of dereferenced targets during test execution, the test must encapsulate allocation within a separate scope and run a multi-step collection sequence:

  ```csharp
  // Run allocations inside an isolated local function to prevent 
  // the JIT compiler from holding strong local references on the stack.
  WeakReference weakVmRef = AllocateAndBind(); 

  GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
  GC.WaitForPendingFinalizers();
  GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true); // Second pass collects finalizable targets

  await Assert.That(weakVmRef.IsAlive).IsFalse();
  ```

* **Test Case (Garbage Collection):**
  * **Sequence:** Instantiate a view and ViewModel, set `view.ViewModel`, call `view.Dispose()`, and clear all strong references using the GC Collection Protocol.
  * **Assertion:** The weak reference confirms that both the View and ViewModel are successfully garbage collected.

---

## Phase 2: Generator Reliability (The Compiler Tier)

**Goal:** Migrate parsing logic to semantic models, implement sorting mathematics, and verify diagnostic reporting.

### 2.1 Diagnostic-Specific Test Harness

* **Constraint:** The existing `TestHelper.Verify` snapshot test helper in `GeneratorSnapshotTests.cs` throws immediately on any generator errors and cannot be used for negative test cases.
* **Implementation Task:** Implement a dedicated non-throwing diagnostic helper `TestHelper.VerifyDiagnostics(source)` that bypasses Verify's assertion engine and returns the raw compiler `Diagnostic` list.
* **Test Case (Diagnostic):** Verify that a circular layout dependency (e.g., `LeftPanel.Below = nameof(RightPanel)` and `RightPanel.Below = nameof(LeftPanel)`) returns an error diagnostic with ID `STN001` and `DiagnosticSeverity.Error`.

### 2.2 Semantic `nameof` Resolution

* **Implementation Task:** Replace fragile substring parsing in `TuiViewParser.ExtractNameof` with Roslyn `SemanticModel.GetSymbolInfo` or structural syntax analysis to extract identifier names.
* **Test Case (Snapshot):** Verify that variations such as `nameof(this.MyProperty)` or `nameof(Namespace.Class.Prop)` compile successfully and emit clean identifier assignments.

### 2.3 Broadening ViewModel Inference

* **Implementation Task (Staged):** Update `TuiViewParser.cs` to check if the target symbol implements `System.ComponentModel.INotifyPropertyChanged` rather than strictly inheriting from `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
* **Test Case (Generator):** Create a snapshot test featuring a custom class that manually implements `INotifyPropertyChanged`. Verify code is generated successfully.

### 2.4 Topological Layout Sort Mathematics

Ensures that views are instantiated in the exact sequence required to resolve coordinates.

* **Test Case (Independent Disjoint Views):**
  * **Input:** Views `[A, B]` with no layout dependencies.
  * **Assertion:** The sort algorithm retains both views in any deterministic order without silently dropping elements.
* **Test Case (Linear Dependency Chain):**
  * **Input:** Constraints `[B below A, C below B]`.
  * **Assertion:** Resolves to the strict sequence `[A, B, C]`.
* **Test Case (Branching Dependencies):**
  * **Input:** Constraints `[B below A, C right-of A]`.
  * **Assertion:** `A` is placed first, followed by `B` and `C`.

---

## Phase 3: Property & Attribute Logic (The Feature Tier)

**Goal:** Verify correct translation of layout properties and attributes.

### 3.1 Title Attribute Propagation

* **Input Constraint:** The target View class must not define a custom parameterless constructor (allowing the generator to emit one).
* **Test Case (Snapshot/Runtime):**
  * Apply `[TuiView<VM>(Title = "My Form")]` to a class inheriting from `Window`.
  * **Assertion:** Generated `MyView.g.cs` contains `this.Title = "My Form";` within its parameterless constructor.

### 3.2 Layout Constraint Translation

* **Assertion Correction:** Verify that layout property assignments apply to the **owner control**, not to `this`.
  * *Example:* If `NameInput` has `Below = nameof(TitleLabel)`, the generated code must assert that `NameInput.Y = Pos.Bottom(TitleLabel);` is emitted.

### 3.3 Synthetic Assignment Interception

Verifies that DSL property assignments inside member initializers are recognized and correctly translated by the parser.

* **Test Case (Snapshot):**
  * **Input:** Field declarations utilizing synthetic properties:

    ```csharp
    public Label MyLabel { get; set; } = new() { Below = nameof(OtherLabel) };
    ```

  * **Assertion:** The parser intercepts the synthetic property `Below`, registers the layout constraint, and successfully emits `MyLabel.Y = Pos.Bottom(OtherLabel);` on the owner control.

### 3.4 Read-Only Property Graceful Degradation

* **Test Case (Snapshot):**
  * **Input:** A view property containing `BindText = nameof(VM.ReadOnlyProperty)`, where the VM property only defines a `get` accessor.
  * **Assertion:** The parser recognizes that the property cannot be written, overrides the mode to `BindingMode.OneWay`, and generates a safe compilation output without attempting to emit a VM setter assignment.

---

## Consolidated "Must-Pass" Matrix (TUnit Integration)

This matrix organizes all 18+ high-level test cases. Test files will be grouped under the existing `Terminal.Gui.Stanza.Tests` project using folder-based segmentation: `Tests/Lifecycle/`, `Tests/Binding/`, and `Tests/Layout/`.

|Target Area|Test Input Scenario|Action / Sequence|Expected Output / Assertion|
|:---|:---|:---|:---|
|**VM Swap**|`VM_A` -> `VM_B`|Property change on `VM_A`|UI does not update and no stale subscriptions are observed.|
|**Null Recovery**|`VM_A` -> `null` -> `VM_B`|Set `VM_B`|Bindings function; no `ObjectDisposedException`.|
|**No-Op Reassign**|`VM_A` -> `VM_A`|Re-assign same reference|No duplicate subviews/bindings; initialization does not re-run.|
|**Manual Layout**|Pre-existing child in ctor|Assign `ViewModel`|Pre-existing child remains in `Subviews`.|
|**Binding Matrix**|`TextField` One-Way|Read-only VM property change|`textField.Value` updates; programmatic UI edits do not sync.|
|**Binding Matrix**|`TextField` Two-Way|Raise `ValueChanged`|VM property updates in response to the UI change.|
|**Binding Matrix**|Generic `Label` One-Way|Change VM property|Updates `label.Text` using the standard text path.|
|**Binding Matrix**|`CheckBox` Two-Way|Toggle VM property|Maps `bool` state correctly to `CheckState.Checked`/`UnChecked`.|
|**Binding Matrix**|Non-string DataType|Bind `int` to `BindText`|Generates `.ToString()` conversion successfully.|
|**Command Binding**|Button command execution|Trigger `Accepting` event|Command executes exactly once.|
|**Command Binding**|Toggle `CanExecute` state|Change command state|`Button.Enabled` updates in sync with command.|
|**GC Safety**|View disposed|Clear strong references (Protocol)|WeakReference confirms VM and View are collected.|
|**Topological Sort**|Disjoint Views|Run sort algorithm|Both views are preserved without being dropped.|
|**Topological Sort**|Linear dependency chain|Run sort algorithm|Correct sequence `[A, B, C]` is returned.|
|**Topological Sort**|Branching dependencies|Run sort algorithm|Root view `A` is sorted first, then branches `B` and `C`.|
|**Layout Emission**|Control `Below = nameof(Target)`|Parse & Emit|Emits `Control.Y = Pos.Bottom(Target);` on the owner control.|
|**DSL Translation**|Synthetic `Below = nameof(Other)`|Parse & Emit|Intercepts initializer assignment and translates to coordinate constraints.|
|**Cycles**|Circular references in layout|Execute Generator|`VerifyDiagnostics` contains `STN001` with `DiagnosticSeverity.Error`.|
|**Parser Safety**|Read-Only VM Property|Parse & Emit|Mode overridden to `OneWay`; no setter compilation error emitted.|

## References

* GC1: [On weak references and how to test them](https://papafe.dev/posts/weak-reference/)
* GC2: [How to test if an object was garbage collected in a unit test (NUnit)](https://www.reddit.com/r/csharp/comments/npp5gg/how_to_test_if_an_object_was_garbage_collected_in/)
