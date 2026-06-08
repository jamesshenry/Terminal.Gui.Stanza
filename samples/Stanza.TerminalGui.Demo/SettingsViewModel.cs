// using Microsoft.Extensions.DependencyInjection;
// using Stanza.TerminalGui;
// using Stanza.TerminalGui.Demo;
// using Terminal.Gui;
// using Terminal.Gui.App;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool EnableTelemetry { get; set; } = true;

    [ObservableProperty]
    public partial bool UseCompactMode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeSummary))]
    public partial string Theme { get; set; } = "System";

    public string ThemeSummary => $"Active theme: {Theme}";

    [RelayCommand]
    private void ToggleTheme()
    {
        Theme = Theme == "System" ? "High Contrast" : "System";
    }
}
