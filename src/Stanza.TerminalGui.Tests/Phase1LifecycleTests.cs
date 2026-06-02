using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.Gui;
using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Tests;

public partial class Phase1ViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;
}

[TuiView<Phase1ViewModel>(Title = "Phase 1 View")]
public partial class Phase1LifecycleView : View
{
    public Label HeaderLabel { get; private set; } = new() { Text = "Header" };

    public TextField NameInput { get; private set; } =
        new() { BindText = nameof(Phase1ViewModel.Name), Below = nameof(HeaderLabel) };
}

[TuiView<Phase1ViewModel>]
public partial class Phase1ManualChildView : View
{
    public Label GeneratedLabel { get; private set; } = new() { Text = "Generated" };

    public Label ManualLabel { get; set; } = new() { Text = "Manual" };

    public Phase1ManualChildView()
    {
        Add(ManualLabel);
    }
}

public class Phase1LifecycleTests
{
    [Test]
    public async Task ViewModel_NullDetachment_UnsubscribesOldVm()
    {
        var vmA = new Phase1ViewModel { Name = "Alpha" };
        var view = new Phase1LifecycleView { ViewModel = vmA };

        await Assert.That(view.NameInput.Value).IsEqualTo("Alpha");

        view.ViewModel = null;
        vmA.Name = "ShouldNotFlow";

        await Assert.That(view.NameInput.Value).IsEqualTo("Alpha");
    }

    [Test]
    public async Task ViewModel_SwapNullSwap_RecoversWithoutStaleSubscriptions()
    {
        var vmA = new Phase1ViewModel { Name = "VM_A" };
        var vmB = new Phase1ViewModel { Name = "VM_B" };
        var view = new Phase1LifecycleView();

        view.ViewModel = vmA;
        await Assert.That(view.NameInput.Value).IsEqualTo("VM_A");

        view.ViewModel = null;
        view.ViewModel = vmB;
        await Assert.That(view.NameInput.Value).IsEqualTo("VM_B");

        vmA.Name = "VM_A_Changed";
        await Assert.That(view.NameInput.Value).IsEqualTo("VM_B");

        vmB.Name = "VM_B_Changed";
        await Assert.That(view.NameInput.Value).IsEqualTo("VM_B_Changed");
    }

    [Test]
    public async Task ViewModel_NoOpReassignment_DoesNotDuplicateSubviews()
    {
        var vm = new Phase1ViewModel { Name = "One" };
        var view = new Phase1LifecycleView();

        view.ViewModel = vm;
        var firstCount = GetSubviews(view).Count;

        view.ViewModel = vm;
        var secondCount = GetSubviews(view).Count;

        await Assert.That(secondCount).IsEqualTo(firstCount);
    }

    [Test]
    public async Task InitializeComponent_PreservesManualChildren()
    {
        var vm = new Phase1ViewModel { Name = "Manual" };
        var view = new Phase1ManualChildView();

        var countBeforeInit = GetSubviews(view).Count;

        view.ViewModel = vm;
        var subviews = GetSubviews(view);

        await Assert.That(countBeforeInit).IsEqualTo(1);
        await Assert.That(subviews.Contains(view.ManualLabel)).IsTrue();
        await Assert.That(subviews.Contains(view.GeneratedLabel)).IsTrue();
        await Assert.That(subviews.Count).IsEqualTo(2);
    }

    private static List<View> GetSubviews(View view)
    {
        var prop = view.GetType().GetProperty("Subviews") ?? view.GetType().GetProperty("SubViews");
        if (prop?.GetValue(view) is not IEnumerable enumerable)
            throw new InvalidOperationException("Could not locate subviews collection on View.");

        return enumerable.OfType<View>().ToList();
    }
}
