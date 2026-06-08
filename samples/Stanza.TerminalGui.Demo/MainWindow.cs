using System.Diagnostics;
using System.Drawing;
using System.Reflection.PortableExecutable;
using Stanza.TerminalGui;
using Stanza.TerminalGui.Layout;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

[StanzaView<MainViewModel>]
public partial class MainWindow : Window
{
    private readonly ScreenConfiguration _screen;
    private readonly ProfileView _profileView;
    private readonly SettingsView _settingsView;

    // Use these fields!
    private readonly View _header = new FrameView { Title = "Stanza Demo" };
    private readonly View _sidebar = new View
    {
        Title = "Navigation",
        BorderStyle = LineStyle.Single,
    };
    private readonly View _content = new View
    {
        Title = "Main Content",
        BorderStyle = LineStyle.Rounded,
    };
    private readonly Button _showProfileButton = new() { Text = "Profile" };
    private readonly Button _showSettingsButton = new() { Text = "Settings" };
    private View? _currentContentView;
    private readonly StatusBar _statusBar = new StatusBar();

    //// Interactive Elements within Content
    //[BindText(nameof(MainViewModel.UserName))]
    //private readonly TextField _nameInput = new() { Width = 20, Enabled = true };

    //[BindText(nameof(MainViewModel.GreetingMessage))]
    //private readonly Label _greetingLabel = new();

    //[BindCommand(nameof(MainViewModel.ResetCommand))]
    //private readonly Button _resetBtn = new() { Text = "Reset" };

    public MainWindow(
        MainViewModel viewModel,
        ProfileViewModel profileViewModel,
        SettingsViewModel settingsViewModel
    )
    {
        // Initialize the screen config bound to THIS window
        _screen = new ScreenConfiguration(this);
        _profileView = new ProfileView(profileViewModel);
        _settingsView = new SettingsView(settingsViewModel);
        ViewModel = viewModel;

        Title = "Stanza Responsive v2";

        // --- Sidebar Logic ---
        _sidebar.X = 0;
        _sidebar.Y = Pos.Bottom(_header);

        // Media Query: 0 width on Small, 20 on Medium, 30 on Large+
        _sidebar.Width = Responsive.Dimension(
            _screen,
            size =>
                size switch
                {
                    ScreenSize.S => 0,
                    ScreenSize.M => 20,
                    _ => 30,
                }
        );

        _sidebar.Height = Dim.Fill(1);

        // --- Header Logic ---
        // Header height: 1 on Small, 3 on others
        _header.Height = Responsive.Dimension(_screen, size => size == ScreenSize.S ? 1 : 3);
        _header.Width = Dim.Fill();

        // --- Content Logic ---
        _content.X = Pos.Right(_sidebar);
        _content.Y = Pos.Bottom(_header);
        _content.Width = Dim.Fill();
        _content.Height = Dim.Fill(1);

        _showProfileButton.X = 1;
        _showProfileButton.Y = 1;
        _showSettingsButton.X = 1;
        _showSettingsButton.Y = Pos.Bottom(_showProfileButton);
        _showProfileButton.Accepting += (_, _) => SetCurrentView(_profileView);
        _showSettingsButton.Accepting += (_, _) => SetCurrentView(_settingsView);

        _sidebar.Add(_showProfileButton, _showSettingsButton);

        Add(_header, _sidebar, _content, _statusBar);
        SetCurrentView(_profileView);

        // Example: Content internal layout change based on screen size
        var greeting = new Label();
        _content.Add(greeting);

        // You can even use it for visibility or text
        this.ViewportChanged += (s, e) =>
        {
            greeting.Text = $"Current Viewport Size: {_screen.CurrentSize}";
        };
    }

    private void SetCurrentView(View nextView)
    {
        if (_currentContentView != null)
        {
            _content.Remove(_currentContentView);
        }

        _currentContentView = nextView;
        _currentContentView.X = 0;
        _currentContentView.Y = 0;
        _currentContentView.Width = Dim.Fill();
        _currentContentView.Height = Dim.Fill();
        _content.Add(_currentContentView);
    }
}

public static class Responsive
{
    public static Dim Dimension(ScreenConfiguration screen, Func<ScreenSize, int> layoutLogic)
    {
        return Dim.Func((_) => layoutLogic(screen.CurrentSize));
    }

    public static Pos Position(ScreenConfiguration screen, Func<ScreenSize, int> layoutLogic)
    {
        return Pos.Func((_) => layoutLogic(screen.CurrentSize));
    }
}
