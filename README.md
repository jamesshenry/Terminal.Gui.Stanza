# Stanza.TerminalGui

Stanza is a small, reflection-free MVVM binding library for **Terminal.Gui v2**. It uses C# Source Generators to eliminate boilerplate, providing a declarative way to wire up UI controls to ViewModels.

## Features

- Reflection-free enables NativeAOT compilation
- No framework dependencies: extensions are dependent on standard .NET APIs; `INotifyPropertyChanged`, `ICommand` meaning CommunityToolkit.Mvvm or Reactive Extensions can be used.
- Automatic binding management or manual management by opting out of source generator usage

## Installation

Add the package to your project:

```bash
dotnet add package Stanza.TerminalGui
```

## Quick Start

### 1. Define a ViewModel

Using `CommunityToolkit.Mvvm` (optional but recommended) to handle property notifications.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Username { get; set; } = "Stanza User";

    [RelayCommand]
    private void Reset() => Username = "Guest";
}
```

### 2. Create a View

Declare your UI using standard Terminal.Gui code, and use Stanza attributes to bind the data.

```csharp
using Stanza.TerminalGui;
using Terminal.Gui.Views;

// 1. Mark as partial
// 2. Use the generic StanzaView attribute to specify your ViewModel
[StanzaView<MainViewModel>]
public partial class MainView : Window
{
    // Bind the Text property of this Label to ViewModel.Username
    [BindText(nameof(MainViewModel.Username))]
    public Label NameLabel { get; set; } = new();

    // Bind the Button trigger to the ResetCommand
    [BindCommand(nameof(MainViewModel.ResetCommand))]
    public Button ResetButton { get; set; } = new() { Text = "Reset" };

    public MainView()
    {
        Title = "Stanza Quickstart";
        
        ResetButton.Y = Pos.Bottom(NameLabel);
        Add(NameLabel, ResetButton);
    }
}
```

### 3. Initialize and Run

Simply assign the `ViewModel` property. Stanza handles the subscription logic automatically.

```csharp
var viewModel = new MainViewModel();
var view = new MainView { ViewModel = viewModel };

Application.Run(view);
```

## Available Bindings

| Attribute | Target Property | Support |
| :--- | :--- | :--- |
| `[BindText]` | `Text` | Two-Way (TextField), One-Way (Label) |
| `[BindChecked]`| `Checked` / `Value`| Two-Way (CheckBox) |
| `[BindEnabled]` | `Enabled` | One-Way |
| `[BindVisible]` | `Visible` | One-Way |
| `[BindCommand]` | `Accepting` / `Enabled`| Execute + CanExecute (Button) |

## Advanced: Manual Bindings

If you need logic that doesn't fit a standard attribute, you can use the `OnApplyBindings` hook provided by the generator. This method is called automatically whenever a new ViewModel is attached.

```csharp
public partial class MainView
{
    // This partial method is called by the generated code
    partial void OnApplyBindings(BindingContext context)
    {
        // Manual binding with custom logic
        this.Bind(ViewModel, vm => vm.Username, val => {
            Title = $"Editing: {val}";
        }).AddTo(context);
        
        // Context is automatically disposed when the VM changes 
        // or the View is disposed.
    }
}
```

## Why Stanza?

In standard Terminal.Gui, you often find yourself writing code like this:

```csharp
// The "Old" Way (Manual, Leak-prone, No Thread Safety)
viewModel.PropertyChanged += (s, e) => {
    if (e.PropertyName == "Name") {
        Application.Invoke(() => label.Text = viewModel.Name);
    }
};
```

**Stanza replaces this with a single attribute.** It handles the property name check, the thread marshalling, the initial synchronization, and the event unsubscription for you.
