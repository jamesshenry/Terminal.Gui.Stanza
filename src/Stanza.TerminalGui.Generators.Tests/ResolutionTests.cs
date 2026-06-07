using Microsoft.CodeAnalysis;

namespace Stanza.TerminalGui.Generators.Tests;

public class ResolutionTests
{
    // src\Stanza.TerminalGui.Generators.Tests\ResolutionTests.cs

    [Test]
    public async Task STN020_Warning_OnRealTypo()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;

public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public string Name { get; set; }
}

[StanzaView<MyVm>]
public partial class MyView : View {
    [BindText("Nmae")] // Typo: Should trigger warning
    public Label MyLabel { get; set; } = new();
}
""";

        var diags = TestHelper.VerifyDiagnostics(source);
        await Assert
            .That(diags)
            .Contains(d => d.Id == "STN020" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task RelayCommand_Heuristic_SuppressesSTN020()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
                
                // This simulates the method that triggers CommunityToolkit.Mvvm
                // The 'SaveCommand' property doesn't exist yet, but the method does.
                public void Save() { } 
            }

            [StanzaView<MyVm>]
            public partial class MyView : View {
                [BindCommand("SaveCommand")] 
                public Button SaveBtn { get; set; } = new();
            }
            """;

        var diags = TestHelper.VerifyDiagnostics(source);

        // Should NOT contain STN020 because the heuristic detects 'Save()' method
        await Assert.That(diags.Any(d => d.Id == "STN020")).IsFalse();
    }

    [Test]
    public async Task STN021_Error_WhenViewModelPropertyIsPrivate()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
                private string Secret { get; set; } // Private!
            }

            [StanzaView<MyVm>]
            public partial class MyView : View {
                [BindText("Secret")]
                public Label MyLabel { get; set; } = new();
            }
            """;

        var diags = TestHelper.VerifyDiagnostics(source);
        await Assert.That(diags).Contains(d => d.Id == "STN021");
    }
}
