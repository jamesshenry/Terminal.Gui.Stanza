using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.ClusterExplorer;

[TuiView<ClusterExplorerViewModel>]
public partial class ClusterExplorerWindow : Window
{
    public ClusterExplorerWindow()
    {
        Title = "DevOps Cluster Explorer";
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    // Level 2 Subview: The generator detects NodeGrid implements IStanzaView<ClusterExplorerViewModel>
    // and automatically generates: NodeGrid.ViewModel = this.ViewModel;
    public NodeGridPanel NodeGrid { get; private set; } =
        new() { Width = Dim.Fill(), Height = Dim.Fill(2) };

    public Label FooterStatusBar { get; private set; } =
        new()
        {
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            BindText = nameof(ClusterExplorerViewModel.ClusterName),
        };
}
