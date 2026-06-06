using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Demo;

[StanzaView<DashboardViewModel>]
public partial class IdentitySection : FrameView
{
    public Label UserLabel { get; set; } = new() { Text = "Username:" };

    [BindText(nameof(DashboardViewModel.Username))]
    public TextField UserInput { get; set; } = new() { Width = Dim.Fill() };

    [BindChecked(nameof(DashboardViewModel.NotificationsEnabled))]
    public CheckBox NotifyCheck { get; set; } = new() { Text = "Enable Notifications" };

    public IdentitySection()
    {
        Title = "Identity";
        Width = Dim.Percent(50);
        Height = Dim.Fill();

        UserInput.Y = Pos.Bottom(UserLabel);
        NotifyCheck.Y = Pos.Bottom(UserInput);

        Add(UserLabel, UserInput, NotifyCheck);
    }
}
