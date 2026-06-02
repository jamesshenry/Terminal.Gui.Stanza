using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.ClusterExplorer;

[StanzaView<ClusterExplorerViewModel>]
public partial class NodeGridPanel : FrameView
{
    // Level 3 Subview: Handled recursively
    public NodeHealthCard PrimaryNodeCard { get; private set; } =
        new() { Width = Dim.Percent(50), Height = Dim.Fill() };

    public CpuHistoryPanel HistoryPanel { get; private set; } =
        new()
        {
            RightOf = nameof(PrimaryNodeCard),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
}
