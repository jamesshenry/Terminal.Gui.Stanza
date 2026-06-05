using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
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
        var dispatcher = new View(); // Dummy dispatcher
        var uiValue = "";

        // Act - Note: Bind signature changed to require dispatcher and vm-parameterized lambda
        using var binding = vm.Bind(dispatcher, x => x.Name, val => uiValue = val);

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
        var dispatcher = new View();
        var uiValue = "";
        var binding = vm.Bind(dispatcher, x => x.Name, val => uiValue = val);

        // Act
        binding.Dispose();
        vm.Name = "ChangesAfterDispose";

        // Assert
        await Assert.That(uiValue).IsEqualTo("Initial");
    }

    [Test]
    public async Task BindCommand_ExecutesRelayCommandOnButtonAccept()
    {
        // Arrange
        var vm = new FakeViewModel();
        var button = new Button();

        // Act - Using updated signature: command.BindCommand(button)
        using var binding = vm.SaveCommand.BindCommand(button);

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
        var dispatcher = new View();
        var uiValue1 = "";
        var uiValue2 = "";

        // Use the new AddTo fluent extension as well
        vm.Bind(dispatcher, x => x.Name, val => uiValue1 = val).AddTo(ctx);
        vm.Bind(dispatcher, x => x.Name, val => uiValue2 = val).AddTo(ctx);

        // Act
        ctx.Dispose();
        vm.Name = "NewValue";

        // Assert
        await Assert.That(uiValue1).IsEqualTo("Initial");
        await Assert.That(uiValue2).IsEqualTo("Initial");
    }

    [Test]
    public async Task AddTo_ReturnsOriginalInstance()
    {
        // Arrange
        var ctx = new BindingContext();
        var disposable = new DisposableAction(() => { });

        // Act
        var returned = disposable.AddTo(ctx);

        // Assert - Verify fluent return (NativeAOT check)
        await Assert.That(returned).IsSameReferenceAs(disposable);
    }

    [Test]
    public async Task AddTo_FluentExtension_RegistersInContext()
    {
        // Arrange
        var ctx = new BindingContext();
        var wasDisposed = false;
        var disposable = new DisposableAction(() => wasDisposed = true);

        // Act
        disposable.AddTo(ctx);
        ctx.Dispose();

        // Assert
        await Assert.That(wasDisposed).IsTrue();
    }

    [Test]
    public async Task Manual_TwoWay_Update_Test()
    {
        // Arrange
        var vm = new FakeViewModel { Name = "Initial" };
        var textField = new TextField { Text = "Initial" };
        var dispatcher = textField;

        // 1. VM -> UI (One Way)
        using var binding = vm.Bind(dispatcher, x => x.Name, val => textField.Text = val);

        // 2. UI -> VM (Manual hook since no BindTwoWay exists yet)
        textField.TextChanged += (s, e) =>
        {
            vm.Name = textField.Text.ToString();
        };

        // Act - Change UI
        textField.Text = "Changed In UI";

        // Assert - VM updated
        await Assert.That(vm.Name).IsEqualTo("Changed In UI");

        // Act - Change VM
        vm.Name = "Changed In VM";

        // Assert - UI updated
        await Assert.That(textField.Text.ToString()).IsEqualTo("Changed In VM");
    }

    [Test]
    public async Task Bind_HandlesVariousLambdaStyles_Correctly()
    {
        var vm = new FakeViewModel { Name = "Initial" };
        var dispatcher = new View();
        string? capturedValue = null;

        // Style 1: Standard 'x'
        using var b1 = vm.Bind(dispatcher, x => x.Name, v => capturedValue = v);
        vm.Name = "Updated1";
        await Assert.That(capturedValue).IsEqualTo("Updated1");

        // Style 2: Different parameter name (ensures parser isn't hardcoded to 'x')
        using var b2 = vm.Bind(dispatcher, model => model.Name, v => capturedValue = v);
        vm.Name = "Updated2";
        await Assert.That(capturedValue).IsEqualTo("Updated2");

        // Style 3: Nested path (ensures parser takes the LAST segment)
        // Note: This assumes FakeViewModel has a sub-property.
        // Even if it doesn't, the string parser should return "Name" for "m => m.Sub.Name"
    }

    [Test]
    [Arguments("x => x.Name", "Name")]
    [Arguments("vm => vm.Title", "Title")]
    [Arguments("m => m.User.Profile.FirstName", "FirstName")]
    [Arguments("() => vm.Status", "Status")]
    public async Task ExtractPropertyName_ValidatesExpectedFormats(
        string expression,
        string expected
    )
    {
        await Assert.That(BindingExtensions.ExtractPropertyName(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null, "Text")]
    [Arguments("", "Text")]
    [Arguments("viewModel.Name", "Name")]
    public async Task ExtractPropertyName_HandlesEdgeCases(string? expression, string expected)
    {
        await Assert.That(BindingExtensions.ExtractPropertyName(expression)).IsEqualTo(expected);
    }
}

// [StanzaView<DemoViewModel>(Title = "Stanza MVVM Demo")]
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
