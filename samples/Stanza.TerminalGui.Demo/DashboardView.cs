using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Demo;

[StanzaView<DashboardViewModel>]
public partial class DashboardView : Window
{
    // Nested Sub-Views
    public IdentitySection LeftPanel { get; set; } = new();

    public StatsSection RightPanel { get; set; } = new();

    // Global Footer added to the parent
    [BindText(nameof(DashboardViewModel.Summary))]
    public Label FooterLabel { get; set; } = new();

    public DashboardView()
    {
        Title = "Stanza Nested Dashboard";

        RightPanel.X = Pos.Right(LeftPanel);
        FooterLabel.Y = Pos.AnchorEnd(1);
        FooterLabel.X = Pos.Center();
        FooterLabel.Width = Dim.Fill();
        FooterLabel.TextAlignment = Alignment.Center;

        Add(LeftPanel, RightPanel, FooterLabel);
    }

    public DashboardView(DashboardViewModel viewModel) : this()
    {
        this.ViewModel = viewModel;
        LeftPanel.ViewModel = viewModel;
        RightPanel.ViewModel = viewModel;
    }
}
