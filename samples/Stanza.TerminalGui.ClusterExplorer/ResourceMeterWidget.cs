using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.ClusterExplorer;

[StanzaView<ClusterExplorerViewModel>]
public partial class ResourceMeterWidget : View
{
    public Label MeterHeaderLabel { get; private set; } = new() { Text = "CPU utilization: " };

    [BindText(nameof(ClusterExplorerViewModel.CpuUsageText))]
    public Label MeterValueLabel { get; private set; } = new();

    public ResourceMeterWidget()
    {
        MeterValueLabel.X = Pos.Right(MeterHeaderLabel);
        Add(MeterHeaderLabel, MeterValueLabel);
    }
}

[StanzaView<ClusterExplorerViewModel>]
public partial class RamMeterWidget : View
{
    public Label MeterHeaderLabel { get; private set; } = new() { Text = "RAM utilization: " };

    [BindText(nameof(ClusterExplorerViewModel.RamUsageText))]
    public Label MeterValueLabel { get; private set; } = new();

    public RamMeterWidget()
    {
        MeterValueLabel.X = Pos.Right(MeterHeaderLabel);
        Add(MeterHeaderLabel, MeterValueLabel);
    }
}
