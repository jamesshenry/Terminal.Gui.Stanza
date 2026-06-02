using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Demo;

[TuiView<DashboardViewModel>]
public partial class DashboardView : Window
{
    public DashboardView()
    {
        Title = "Stanza Nested Dashboard";
    }

    // Nested Sub-Views
    public IdentitySection LeftPanel { get; set; } = new();

    public StatsSection RightPanel { get; set; } = new() { RightOf = nameof(LeftPanel) };

    // Global Footer added to the parent
    public Label FooterLabel { get; set; } =
        new()
        {
            BindText = nameof(DashboardViewModel.Summary),
            Y = Pos.AnchorEnd(1),
            X = Pos.Center(),
            Width = Dim.Fill(),
            TextAlignment = Alignment.Center,
        };
}
