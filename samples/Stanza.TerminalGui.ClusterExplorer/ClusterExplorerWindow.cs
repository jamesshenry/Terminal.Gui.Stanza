using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.ClusterExplorer;

[StanzaView<ClusterExplorerViewModel>]
public partial class ClusterExplorerWindow : Window
{
    // Level 2 Subview: The NodeGrid subview
    public NodeGridPanel NodeGrid { get; private set; } = new();

    [BindText(nameof(ClusterExplorerViewModel.ClusterName))]
    public Label FooterStatusBar { get; private set; } = new();

    public ClusterExplorerWindow()
    {
        Title = "DevOps Cluster Explorer";
        Width = Dim.Fill();
        Height = Dim.Fill();

        NodeGrid.Width = Dim.Fill();
        NodeGrid.Height = Dim.Fill(2);

        FooterStatusBar.Y = Pos.AnchorEnd(1);
        FooterStatusBar.Width = Dim.Fill();

        Add(NodeGrid, FooterStatusBar);
    }

    public ClusterExplorerWindow(ClusterExplorerViewModel viewModel)
        : this()
    {
        this.ViewModel = viewModel;
        NodeGrid.ViewModel = viewModel;
    }
}
