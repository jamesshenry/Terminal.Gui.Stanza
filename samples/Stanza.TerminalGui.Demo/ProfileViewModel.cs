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

    public string Greeting => $"Hello, {Name}!" + (IsOpenSourceContributor ? " ❤️ " : "");

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(Greeting));

    partial void OnIsOpenSourceContributorChanged(bool value) =>
        OnPropertyChanged(nameof(Greeting));

    [RelayCommand(CanExecute = nameof(HasAcceptedTerms))]
    private async Task Save()
    {
        await Task.Delay(1000);
        Name = Name.ToUpper();
    }
}
