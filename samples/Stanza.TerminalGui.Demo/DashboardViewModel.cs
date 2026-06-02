using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Stanza.TerminalGui.Demo;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial string Username { get; set; } = "AdminUser";

    [ObservableProperty]
    public partial int LoginCount { get; set; } = 0;

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; } = true;

    public string Summary => $"User: {Username} | Logins: {LoginCount}";

    [RelayCommand]
    private void IncrementLogins() => LoginCount++;
}
