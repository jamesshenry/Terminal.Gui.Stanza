using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.Gui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Tests;

public partial class SimpleViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "John";
}

[TuiView<SimpleViewModel>(Title = "Simple Form")]
public partial class SimpleFormView : View
{
    public Label TitleLabel { get; private set; } = new() { Text = "Form" };
    public TextField NameInput { get; private set; } =
        new() { BindText = nameof(SimpleViewModel.Name), Below = nameof(TitleLabel) };
    public Button SubmitButton { get; private set; } =
        new() { Text = "Submit", Below = nameof(NameInput) };
}

public class E2EStanzaTests
{
    [Test]
    public async Task View_Initializes_Successfully()
    {
        var vm = new SimpleViewModel { Name = "Alice" };
        var view = new SimpleFormView { ViewModel = vm };

        await Assert.That(view).IsNotNull();
        await Assert.That(view.NameInput.Text).IsEqualTo("Alice");
    }

    [Test]
    public async Task View_TwoWayBinding_UpdatesViewModelFromUi()
    {
        var vm = new SimpleViewModel { Name = "Alice" };
        var view = new SimpleFormView { ViewModel = vm };

        // Act - Simulate typing in UI (triggering ValueChanged)
        view.NameInput.Value = "Bob";

        // Assert - ViewModel property updated
        await Assert.That(vm.Name).IsEqualTo("Bob");
    }

    [Test]
    public async Task View_TwoWayBinding_UpdatesUiFromViewModel()
    {
        var vm = new SimpleViewModel { Name = "Alice" };
        var view = new SimpleFormView { ViewModel = vm };

        // Act - Update ViewModel property
        vm.Name = "Charlie";

        // Assert - UI elements updated
        await Assert.That(view.NameInput.Value).IsEqualTo("Charlie");
    }
}
