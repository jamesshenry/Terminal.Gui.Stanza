using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Stanza.TerminalGui.Generators.Tests;

public class DiagnosticTests
{
    [Test]
    public async Task STN010_Error_WhenClassIsNotPartial()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            }

            [StanzaView<MyVm>]
            public class MyView : View { } // Missing 'partial'
            """;

        var diags = TestHelper.VerifyDiagnostics(source);

        await Assert.That(diags.Any(d => d.Id == "STN010")).IsTrue();
    }

    [Test]
    public async Task STN011_Error_WhenNotInheritingFromView()
    {
        var source = """
            using Stanza.TerminalGui;

            public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            }

            [StanzaView<MyVm>]
            public partial class MyView { } // Doesn't inherit from View
            """;

        var diags = TestHelper.VerifyDiagnostics(source);

        await Assert.That(diags.Any(d => d.Id == "STN011")).IsTrue();
    }

    [Test]
    public async Task STN020_Error_WhenPropertyDoesNotExistOnVm()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            }

            [StanzaView<MyVm>]
            public partial class MyView : View {
                [BindText("NonExistentProperty")]
                public Label MyLabel { get; set; } = new();
            }
            """;

        var diags = TestHelper.VerifyDiagnostics(source);

        await Assert.That(diags.Any(d => d.Id == "STN020")).IsTrue();
    }

    [Test]
    public async Task STN030_Error_WhenBindCheckedUsedOnString()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
                public string NotABool { get; set; } = "";
            }

            [StanzaView<MyVm>]
            public partial class MyView : View {
                [BindChecked(nameof(MyVm.NotABool))]
                public CheckBox MyCheck { get; set; } = new();
            }
            """;

        var diags = TestHelper.VerifyDiagnostics(source);

        await Assert.That(diags.Any(d => d.Id == "STN030")).IsTrue();
    }

    [Test]
    public async Task STN012_Error_OnMemberCollision()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            }

            [StanzaView<MyVm>]
            public partial class MyView : View {
                // Collision with generated plumbing
                public string ViewModel { get; set; } = ""; 
            }
            """;

        var diags = TestHelper.VerifyDiagnostics(source);

        await Assert.That(diags.Any(d => d.Id == "STN012")).IsTrue();
    }

    [Test]
    public async Task STN004_Error_OnExplicitTwoWayReadOnly()
    {
        var source = """
            using Stanza.TerminalGui;
            using Terminal.Gui.Views;

            public class MyVm : System.ComponentModel.INotifyPropertyChanged { 
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
                public string ReadOnlyProp => "Hello";
            }

            [StanzaView<MyVm>]
            public partial class MyView : View {
                [BindText(nameof(MyVm.ReadOnlyProp), Mode = BindingMode.TwoWay)]
                public Label MyLabel { get; set; } = new();
            }
            """;

        var diags = TestHelper.VerifyDiagnostics(source);

        await Assert.That(diags.Any(d => d.Id == "STN004")).IsTrue();
    }
}
