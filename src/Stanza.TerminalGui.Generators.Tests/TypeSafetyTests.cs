using Stanza.TerminalGui.Generators.Tests;

namespace Stanza.TerminalGui.Generators.Tests
{
    public class TypeSafetyTests
    {
        [Test]
        public async Task STN022_Error_WhenCommandPropertyIsNotICommand()
        {
            var source = """
                using Stanza.TerminalGui;
                using Terminal.Gui.Views;

                public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
                    public string NotACommand { get; set; } = "";
                }

                [StanzaView<MyVm>]
                public partial class MyView : View {
                    [BindCommand("NotACommand")]
                    public Button MyBtn { get; set; } = new();
                }
                """;

            var diags = TestHelper.VerifyDiagnostics(source);
            await Assert.That(diags).Contains(d => d.Id == "STN022");
        }

        [Test]
        public async Task STN030_Error_WhenBindingBoolPropertyToText()
        {
            var source = """
                using Stanza.TerminalGui;
                using Terminal.Gui.Views;

                public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
                    public string MyString { get; set; } = "";
                }

                [StanzaView<MyVm>]
                public partial class MyView : View {
                    [BindChecked("MyString")] // Error: Requires bool
                    public CheckBox MyCheck { get; set; } = new();
                }
                """;

            var diags = TestHelper.VerifyDiagnostics(source);
            await Assert.That(diags).Contains(d => d.Id == "STN030");
        }
    }
}

public class EmitterScenarioTests
{
    [Test]
    public async Task Emitter_HandlesInheritedViewModelProperties()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class BaseVm : System.ComponentModel.INotifyPropertyChanged {
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
                public string InheritedName { get; set; }
            }

            public class DerivedVm : BaseVm { }

            [StanzaView<DerivedVm>]
            public partial class MyView : View {
                [BindText("InheritedName")]
                public Label MyLabel { get; set; } = new();
            }
            """;

        var generated = TestHelper.GetGeneratedSource(source, "MyView.g.cs");

        await Assert.That(generated).Contains("ApplyBindText(_viewModel, \"InheritedName\"");
    }

    [Test]
    public async Task Emitter_HandlesGlobalNamespace()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class Vm : System.ComponentModel.INotifyPropertyChanged {
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            }

            [StanzaView<Vm>]
            public partial class GlobalView : View { }
            """;

        var generated = TestHelper.GetGeneratedSource(source, "GlobalView.g.cs");

        // Ensure "namespace Global;" or "namespace ;" isn't generated
        await Assert.That(generated).DoesNotContain("namespace");
    }
}
