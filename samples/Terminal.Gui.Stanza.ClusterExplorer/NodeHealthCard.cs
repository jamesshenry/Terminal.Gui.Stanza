using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Stanza.ClusterExplorer;

[TuiView<ClusterExplorerViewModel>]
public partial class NodeHealthCard : FrameView
{
    public Label NodeTitle { get; private set; } = new()
    {
        BindText = nameof(ClusterExplorerViewModel.NodeName)
    };

    public Label NodeIpLabel { get; private set; } = new()
    {
        Below = nameof(NodeTitle),
        BindText = nameof(ClusterExplorerViewModel.NodeIp)
    };

    // Level 4 Subview: CPU Resource Meter
    public ResourceMeterWidget CpuMeter { get; private set; } = new()
    {
        Below = nameof(NodeIpLabel),
        Width = Dim.Fill(),
        Height = 1
    };

    // Level 4 Subview: RAM Resource Meter
    public RamMeterWidget RamMeter { get; private set; } = new()
    {
        Below = nameof(CpuMeter),
        Width = Dim.Fill(),
        Height = 1
    };
}
