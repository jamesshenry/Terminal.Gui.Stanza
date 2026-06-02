using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Tests;

public partial class BindingTests
{
    private partial class FakeViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int Score { get; set; }

        [RelayCommand]
        private void Save() => SaveCalledCount++;

        public int SaveCalledCount { get; set; }
    }

    [Test]
    public async Task Bind_OneWay_UpdatesUiOnPropertyChange()
    {
        // Arrange
        var vm = new FakeViewModel { Name = "Initial" };
        var uiValue = "";

        // Act
        using var binding = vm.Bind(() => vm.Name, val => uiValue = val);

        // Assert - Initial Value (Bind fires immediately)
        await Assert.That(uiValue).IsEqualTo("Initial");

        // Act - Change VM
        vm.Name = "Updated";

        // Assert - UI Updated
        await Assert.That(uiValue).IsEqualTo("Updated");
    }

    [Test]
    public async Task Bind_OneWay_DoesNotUpdateAfterDispose()
    {
        // Arrange
        var vm = new FakeViewModel { Name = "Initial" };
        var uiValue = "";
        var binding = vm.Bind(() => vm.Name, val => uiValue = val);

        // Act
        binding.Dispose();
        vm.Name = "ChangesAfterDispose";

        // Assert
        await Assert.That(uiValue).IsEqualTo("Initial");
    }

    [Test]
    public async Task BindText_TwoWay_UpdatesVmOnUiChange()
    {
        // Arrange
        var vm = new FakeViewModel { Name = "VM" };
        var label = new Label { Text = "UI" };

        // Act
        using var binding = vm.BindTextTo(label, () => vm.Name, val => vm.Name = val);

        // Simulate UI change
        label.Text = "ChangedInUI";

        // Assert
        await Assert.That(vm.Name).IsEqualTo("ChangedInUI");
    }

    [Test]
    public async Task BindCommand_ExecutesRelayCommandOnButtonAccept()
    {
        // Arrange
        var vm = new FakeViewModel();
        var button = new Button();

        // Act
        using var binding = vm.BindCommandTo(vm.SaveCommand, button);

        // Simulate Button Accept/Click
        button.InvokeCommand(Command.Accept);

        // Assert
        await Assert.That(vm.SaveCalledCount).IsEqualTo(1);
    }

    [Test]
    public async Task BindingContext_Dispose_CleansUpMultipleBindings()
    {
        // Arrange
        var ctx = new BindingContext();
        var vm = new FakeViewModel { Name = "Initial" };
        var uiValue1 = "";
        var uiValue2 = "";

        ctx.AddBinding(vm.Bind(() => vm.Name, val => uiValue1 = val));
        ctx.AddBinding(vm.Bind(() => vm.Name, val => uiValue2 = val));

        // Act
        ctx.Dispose();
        vm.Name = "NewValue";

        // Assert
        await Assert.That(uiValue1).IsEqualTo("Initial");
        await Assert.That(uiValue2).IsEqualTo("Initial");
    }

    // [Test]
    // public async Task Trait_View_HasDimensions_And_Instantiates_Immediately()
    // {
    //     // Arrange & Act
    //     var view = new DemoView(); // No VM set yet

    //     // Assert
    //     await Assert.That(view.Width).IsEqualTo(Dim.Fill());
    //     await Assert.That(view.TitleLabel).IsNotNull(); // InitializeComponent must have run
    //     await Assert.That(view.SubViews.Contains(view.TitleLabel)).IsTrue();
    // }
}


// [TuiView<DemoViewModel>(Title = "Stanza MVVM Demo")]
// public partial class DemoView : View
// {
//     public Label TitleLabel { get; private set; } = new()
//     {
//         Text = "Stanza.TerminalGui Declarative Demo",
//         Width = Dim.Auto(),
//         Height = 1
//     };

//     public Label InstructionLabel { get; private set; } = new()
//     {
//         Text = "Type your name below to see dynamic bindings:",
//         Width = Dim.Auto(),
//         Height = 1,
//         Below = nameof(TitleLabel)
//     };

//     public TextField NameInput { get; private set; } = new()
//     {
//         Width = 30,
//         Height = 1,
//         BindText = nameof(DemoViewModel.Name),
//         Below = nameof(InstructionLabel),
//         Enabled = true,
//     };

//     public CheckBox ShowGreetingsCheckbox { get; private set; } = new()
//     {
//         Text = "Show Greetings Banner",
//         Width = Dim.Auto(),
//         Height = 1,
//         BindChecked = nameof(DemoViewModel.ShowGreetings),
//         Below = nameof(NameInput)
//     };

//     public Label GreetingsLabel { get; private set; } = new()
//     {
//         Width = Dim.Auto(),
//         Height = Dim.Auto(),
//         BindText = nameof(DemoViewModel.GreetingMessage),
//         BindVisible = nameof(DemoViewModel.ShowGreetings),
//         Below = nameof(ShowGreetingsCheckbox)
//     };

//     public Button ResetButton { get; private set; } = new()
//     {
//         Text = "Reset Form",
//         Width = Dim.Auto(),
//         Height = Dim.Auto(),
//         BindCommand = nameof(DemoViewModel.ResetCommand),
//         Below = nameof(GreetingsLabel)
//     };
// }
// public partial class DemoViewModel : ObservableObject
// {
//     [ObservableProperty]
//     public partial string Name { get; set; } = "Stanza User";
//     [ObservableProperty]
//     public partial bool ShowGreetings { get; set; } = true;

//     public string GreetingMessage => $"Hello, {Name}! Welcome to Stanza.TerminalGui!";

//     partial void OnNameChanged(string value)
//     {
//         OnPropertyChanged(nameof(GreetingMessage));
//     }

//     [RelayCommand]
//     private void Reset()
//     {
//         Name = "Stanza User";
//         ShowGreetings = true;
//     }
// }
