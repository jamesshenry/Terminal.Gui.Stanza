using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stanza.TerminalGui.Generators;
using Terminal.Gui;
using VerifyTests;
using VerifyTUnit;

namespace Stanza.TerminalGui.Generators.Tests;

public class GeneratorSnapshotTests
{
    [Test]
    public Task GeneratesCorrectly_BindText_TwoWay()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using CommunityToolkit.Mvvm.ComponentModel; // For ObservableObject

namespace TestNamespace;

public partial class MyViewModel : ObservableObject
{
    public string Name { get; set; }
}

[StanzaView<MyViewModel>]
public partial class MyView : View
{
    [BindText(nameof(MyViewModel.Name))]
    public Label MyLabel { get; set; } = new() { Text = "Hello" };
}
""";

        return TestHelper.VerifyGenerator(source);
    }

    [Test]
    public async Task ReadOnlyBindText_DegradesToOneWay_NoSetterLambdaEmitted()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TestNamespace;

public class ReadOnlyVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name => "ReadOnly";
}

[StanzaView<ReadOnlyVm>]
public partial class ReadOnlyBindingView : View
{
    [BindText(nameof(ReadOnlyVm.Name))]
    public Label NameLabel { get; set; } = new();
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "ReadOnlyBindingView.g.cs");
        await Assert
            .That(
                generated.Contains(
                    "ApplyBindText(_viewModel, \"Name\", x => x.Name)",
                    StringComparison.Ordinal
                )
            )
            .IsTrue();
        await Assert
            .That(generated.Contains("(x, val) => x.Name = val", StringComparison.Ordinal))
            .IsFalse();
    }

    [Test]
    public Task GeneratesCorrectly_BindCommand()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TestNamespace;

public partial class MyViewModel : ObservableObject
{
    [RelayCommand]
    private void Save() { } // CommunityToolkit will generate SaveCommand
}

[StanzaView<MyViewModel>]
public partial class MyView : View
{
    [BindCommand(nameof(MyViewModel.SaveCommand))]
    public Button SaveButton { get; set; } = new();
}
""";
        return TestHelper.VerifyGenerator(source, "CS0117", "CS1061");
    }

    [Test]
    public Task GeneratesCorrectly_BindChecked_TwoWay()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TestNamespace;

public partial class MyViewModel : ObservableObject
{
    public bool IsChecked { get; set; }
}

[StanzaView<MyViewModel>]
public partial class MyView : View
{
    [BindChecked(nameof(MyViewModel.IsChecked))]
    public CheckBox MyCheckBox { get; set; } = new();
}
""";
        return TestHelper.VerifyGenerator(source);
    }

    [Test]
    public Task GeneratesCorrectly_BindVisible_OneWay()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TestNamespace;

public partial class MyViewModel : ObservableObject
{
    public bool ShowElement { get; set; }
}

[StanzaView<MyViewModel>]
public partial class MyView : View
{
    [BindVisible(nameof(MyViewModel.ShowElement))]
    public Label MyLabel { get; set; } = new();
}
""";
        return TestHelper.VerifyGenerator(source);
    }

    [Test]
    public Task GeneratesCorrectly_BindEnabled_OneWay()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TestNamespace;

public partial class MyViewModel : ObservableObject
{
    public bool CanEdit { get; set; }
}

[StanzaView<MyViewModel>]
public partial class MyView : View
{
    [BindEnabled(nameof(MyViewModel.CanEdit))]
    public TextField MyTextField { get; set; } = new();
}
""";
        return TestHelper.VerifyGenerator(source);
    }

    [Test]
    public Task GeneratesCorrectly_WithNoBindings()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TestNamespace;

public partial class MyViewModel : ObservableObject
{
    public string Name { get; set; }
}

[StanzaView<MyViewModel>]
public partial class MyView : View
{
    // No bindings here
    public Label MyLabel { get; set; } = new();
}
""";
        return TestHelper.VerifyGenerator(source);
    }

    [Test]
    public Task GeneratesCorrectly_GlobalNamespace()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class GlobalViewModel : ObservableObject
{
    public string Status { get; set; }
}

[StanzaView<GlobalViewModel>]
public partial class GlobalView : View
{
    [BindText(nameof(GlobalViewModel.Status))]
    public Label StatusLabel { get; set; } = new();
}
""";
        return TestHelper.VerifyGenerator(source);
    }
}
