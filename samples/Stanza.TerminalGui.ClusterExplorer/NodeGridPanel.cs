using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.ClusterExplorer;

[StanzaView<ClusterExplorerViewModel>]
public partial class NodeGridPanel : FrameView
{
    // Level 3 Subview: Handled recursively
    public NodeHealthCard PrimaryNodeCard { get; private set; } = new();

    public CpuHistoryPanel HistoryPanel { get; private set; } = new();

    public NodeGridPanel()
    {
        PrimaryNodeCard.Width = Dim.Percent(50);
        PrimaryNodeCard.Height = Dim.Fill();

        HistoryPanel.X = Pos.Right(PrimaryNodeCard);
        HistoryPanel.Width = Dim.Fill();
        HistoryPanel.Height = Dim.Fill();

        Add(PrimaryNodeCard, HistoryPanel);
    }

    public NodeGridPanel(ClusterExplorerViewModel viewModel)
        : this()
    {
        this.ViewModel = viewModel;
        PrimaryNodeCard.ViewModel = viewModel;
        HistoryPanel.ViewModel = viewModel;
    }
}
