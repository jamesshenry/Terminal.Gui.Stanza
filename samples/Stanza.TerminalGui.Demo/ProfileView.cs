using Stanza.TerminalGui;
using System.Diagnostics;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

[StanzaView<ProfileViewModel>]
public partial class ProfileView : Window
{
    [BindText(nameof(ProfileViewModel.Name))]
    public TextField NameInput { get; set; } = new() { Width = 20 };

    [BindChecked(nameof(ProfileViewModel.IsOpenSourceContributor))]
    public CheckBox ContributorCheck { get; set; } = new() { Text = "OSS Contributor" };

    [BindChecked(nameof(ProfileViewModel.HasAcceptedTerms))]
    public CheckBox TermsCheck { get; set; } = new() { Text = "Accept Terms" };

    [BindText(nameof(ProfileViewModel.Greeting), Mode = BindingMode.OneWay)]
    public Label GreetingLabel { get; set; } = new();

    [BindCommand(nameof(ProfileViewModel.SaveCommand))]
    public Button SaveButton { get; set; } = new() { Text = "Save Profile" };

    public ProfileView(ProfileViewModel viewModel)
    {
        Title = "Developer Profile Editor";
        ViewModel = viewModel;

        NameInput.X = Pos.Center();
        NameInput.Y = 2;

        ContributorCheck.X = Pos.Left(NameInput);
        ContributorCheck.Y = Pos.Bottom(NameInput) + 1;

        TermsCheck.X = Pos.Left(NameInput);
        TermsCheck.Y = Pos.Bottom(ContributorCheck);

        GreetingLabel.X = Pos.Center();
        GreetingLabel.Y = Pos.Bottom(TermsCheck) + 2;

        SaveButton.X = Pos.Center();
        SaveButton.Y = Pos.Bottom(GreetingLabel) + 1;

        Add(NameInput, ContributorCheck, TermsCheck, GreetingLabel, SaveButton);
    }

    // Manual Hook Example
    partial void OnApplyBindings(BindingContext context)
    {
        Debug.WriteLine("ProfileView: Custom Bindings Applied.");
    }
}
