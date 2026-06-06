using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.ClusterExplorer;

[StanzaView<ClusterExplorerViewModel>]
public partial class NodeHealthCard : FrameView
{
    [BindText(nameof(ClusterExplorerViewModel.NodeName))]
    public Label NodeTitle { get; private set; } = new();

    [BindText(nameof(ClusterExplorerViewModel.NodeIp))]
    public Label NodeIpLabel { get; private set; } = new();

    // Level 4 Subview: CPU Resource Meter
    public ResourceMeterWidget CpuMeter { get; private set; } = new();

    // Level 4 Subview: RAM Resource Meter
    public RamMeterWidget RamMeter { get; private set; } = new();

    public NodeHealthCard()
    {
        NodeIpLabel.Y = Pos.Bottom(NodeTitle);

        CpuMeter.Y = Pos.Bottom(NodeIpLabel);
        CpuMeter.Width = Dim.Fill();
        CpuMeter.Height = 1;

        RamMeter.Y = Pos.Bottom(CpuMeter);
        RamMeter.Width = Dim.Fill();
        RamMeter.Height = 1;

        Add(NodeTitle, NodeIpLabel, CpuMeter, RamMeter);
    }

    public NodeHealthCard(ClusterExplorerViewModel viewModel) : this()
    {
        this.ViewModel = viewModel;
        CpuMeter.ViewModel = viewModel;
        RamMeter.ViewModel = viewModel;
    }
}
