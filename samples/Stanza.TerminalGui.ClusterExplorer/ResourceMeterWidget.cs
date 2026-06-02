using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.ClusterExplorer;

[TuiView<ClusterExplorerViewModel>]
public partial class ResourceMeterWidget : View
{
    public Label MeterHeaderLabel { get; private set; } = new() { Text = "CPU utilization: " };

    public Label MeterValueLabel { get; private set; } =
        new()
        {
            RightOf = nameof(MeterHeaderLabel),
            BindText = nameof(ClusterExplorerViewModel.CpuUsageText),
        };
}

[TuiView<ClusterExplorerViewModel>]
public partial class RamMeterWidget : View
{
    public Label MeterHeaderLabel { get; private set; } = new() { Text = "RAM utilization: " };

    public Label MeterValueLabel { get; private set; } =
        new()
        {
            RightOf = nameof(MeterHeaderLabel),
            BindText = nameof(ClusterExplorerViewModel.RamUsageText),
        };
}
