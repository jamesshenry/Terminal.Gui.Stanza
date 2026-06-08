using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

[StanzaView<SettingsViewModel>]
public partial class SettingsView : View
{
    [BindChecked(nameof(SettingsViewModel.EnableTelemetry))]
    public CheckBox EnableTelemetryCheck { get; set; } = new() { Text = "Enable telemetry" };

    [BindChecked(nameof(SettingsViewModel.UseCompactMode))]
    public CheckBox CompactModeCheck { get; set; } = new() { Text = "Use compact mode" };

    [BindText(nameof(SettingsViewModel.ThemeSummary), Mode = BindingMode.OneWay)]
    public Label ThemeSummaryLabel { get; set; } = new();

    [BindCommand(nameof(SettingsViewModel.ToggleThemeCommand))]
    public Button ToggleThemeButton { get; set; } = new() { Text = "Toggle Theme" };

    public SettingsView(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;

        EnableTelemetryCheck.X = 1;
        EnableTelemetryCheck.Y = 1;

        CompactModeCheck.X = 1;
        CompactModeCheck.Y = Pos.Bottom(EnableTelemetryCheck);

        ThemeSummaryLabel.X = 1;
        ThemeSummaryLabel.Y = Pos.Bottom(CompactModeCheck) + 1;

        ToggleThemeButton.X = 1;
        ToggleThemeButton.Y = Pos.Bottom(ThemeSummaryLabel) + 1;

        Add(EnableTelemetryCheck, CompactModeCheck, ThemeSummaryLabel, ToggleThemeButton);
    }
}
