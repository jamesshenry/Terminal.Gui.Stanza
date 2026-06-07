using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Stanza.TerminalGui.Demo;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "New Developer";

    [ObservableProperty]
    public partial bool IsOpenSourceContributor { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool HasAcceptedTerms { get; set; }

    public string Greeting => $"Hello, {Name}!" + (IsOpenSourceContributor ? " ❤️" : "");

    // Notice: We notify the greeting changed whenever Name or Contributor status changes
    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(Greeting));

    partial void OnIsOpenSourceContributorChanged(bool value) =>
        OnPropertyChanged(nameof(Greeting));

    [RelayCommand(CanExecute = nameof(HasAcceptedTerms))]
    private async Task Save()
    {
        // Simulate a background save operation (Thread Safety Test)
        await Task.Delay(1000);
        // This update is marshalled to the UI thread by Stanza automatically
        Name = Name.ToUpper();
    }
}
