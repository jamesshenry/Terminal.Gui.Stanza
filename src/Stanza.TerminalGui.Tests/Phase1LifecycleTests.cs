using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Stanza.TerminalGui;
using Terminal.Gui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Tests;

public partial class Phase1ViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;
}

[StanzaView<Phase1ViewModel>]
public partial class Phase1LifecycleView : View
{
    public Label HeaderLabel { get; private set; } = new() { Text = "Header" };

    [BindText(nameof(Phase1ViewModel.Name))]
    public TextField NameInput { get; private set; } = new();

    public Phase1LifecycleView()
    {
        NameInput.Y = Pos.Bottom(HeaderLabel);
        Add(HeaderLabel, NameInput);
    }
}

public class Phase1LifecycleTests
{
    [Test]
    public async Task ViewModel_NullDetachment_UnsubscribesOldVm()
    {
        var vmA = new Phase1ViewModel { Name = "Alpha" };
        var view = new Phase1LifecycleView { ViewModel = vmA };

        await Assert.That(view.NameInput.Text.ToString()).IsEqualTo("Alpha");

        view.ViewModel = null;
        vmA.Name = "ShouldNotFlow";

        await Assert.That(view.NameInput.Text.ToString()).IsEqualTo("Alpha");
    }

    [Test]
    public async Task ViewModel_SwapNullSwap_RecoversWithoutStaleSubscriptions()
    {
        var vmA = new Phase1ViewModel { Name = "VM_A" };
        var vmB = new Phase1ViewModel { Name = "VM_B" };
        var view = new Phase1LifecycleView();

        view.ViewModel = vmA;
        await Assert.That(view.NameInput.Text.ToString()).IsEqualTo("VM_A");

        view.ViewModel = null;
        view.ViewModel = vmB;
        await Assert.That(view.NameInput.Text.ToString()).IsEqualTo("VM_B");

        vmA.Name = "VM_A_Changed";
        await Assert.That(view.NameInput.Text.ToString()).IsEqualTo("VM_B");

        vmB.Name = "VM_B_Changed";
        await Assert.That(view.NameInput.Text.ToString()).IsEqualTo("VM_B_Changed");
    }
}
