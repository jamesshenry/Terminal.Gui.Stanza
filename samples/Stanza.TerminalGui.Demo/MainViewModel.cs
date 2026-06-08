using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GreetingMessage))]
    public partial string UserName { get; set; } = "Stanza User";

    [ObservableProperty]
    public partial bool ShowSidebar { get; set; } = true;

    public string GreetingMessage => $"Hello, {UserName}! Welcome to Stanza.";

    [RelayCommand]
    private void Reset()
    {
        UserName = "Stanza User 1";
    }

    [RelayCommand]
    private void ToggleSidebar() => ShowSidebar = !ShowSidebar;
}
