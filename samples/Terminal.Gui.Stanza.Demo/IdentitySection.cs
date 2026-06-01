using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Stanza.Demo;

[TuiView<DashboardViewModel>]
public partial class IdentitySection : FrameView
{
    public IdentitySection()
    {
        Title = "Identity";
        Width = Dim.Percent(50);
        Height = Dim.Fill();
    }

    public Label UserLabel { get; set; } = new() { Text = "Username:" };
    
    public TextField UserInput { get; set; } = new() {
        BindText = nameof(DashboardViewModel.Username),
        Below = nameof(UserLabel),
        Width = Dim.Fill()
    };

    public CheckBox NotifyCheck { get; set; } = new() {
        Text = "Enable Notifications",
        BindChecked = nameof(DashboardViewModel.NotificationsEnabled),
        Below = nameof(UserInput)
    };
}
