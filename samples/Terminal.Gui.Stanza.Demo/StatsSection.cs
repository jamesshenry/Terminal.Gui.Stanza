using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Stanza.Demo;

[TuiView<DashboardViewModel>]
public partial class StatsSection : FrameView
{
    public StatsSection() 
    {
        Title = "Statistics";
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    public Label CountLabel { get; set; } = new() {
        Text = "Total Logins:",
        X = 1
    };

    public Label CountDisplay { get; set; } = new() {
        BindText = nameof(DashboardViewModel.LoginCount),
        RightOf = nameof(CountLabel),
    };

    public Button AddLoginBtn { get; set; } = new() {
        Text = "Log Visit",
        BindCommand = nameof(DashboardViewModel.IncrementLoginsCommand),
        Below = nameof(CountLabel),
        X = Pos.Center()
    };
}
