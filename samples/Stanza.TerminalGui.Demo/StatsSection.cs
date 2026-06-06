using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Demo;

[StanzaView<DashboardViewModel>]
public partial class StatsSection : FrameView
{
    public Label CountLabel { get; set; } = new() { Text = "Total Logins:", X = 1 };

    [BindText(nameof(DashboardViewModel.LoginCount), Mode = BindingMode.OneWay)]
    public Label CountDisplay { get; set; } = new();

    [BindCommand(nameof(DashboardViewModel.IncrementLoginsCommand))]
    public Button AddLoginBtn { get; set; } =
        new()
        {
            Text = "Log Visit",
            X = Pos.Center(),
            Visible = true,
        };

    public StatsSection()
    {
        Title = "Statistics";
        Width = Dim.Fill();
        Height = Dim.Fill();

        CountDisplay.X = Pos.Right(CountLabel);
        AddLoginBtn.Y = Pos.Bottom(CountLabel);

        Add(CountLabel, CountDisplay, AddLoginBtn);
    }
}
