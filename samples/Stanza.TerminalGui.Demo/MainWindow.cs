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

    // Use these fields!
    private readonly View _header = new FrameView { Title = "Stanza Demo" };
    private readonly View _sidebar = new View { Title = "Navigation", BorderStyle = LineStyle.Single };
    private readonly View _content = new View { Title = "Main Content", BorderStyle = LineStyle.Rounded };
    private readonly StatusBar _statusBar = new StatusBar();

    //// Interactive Elements within Content
    //[BindText(nameof(MainViewModel.UserName))]
    //private readonly TextField _nameInput = new() { Width = 20, Enabled = true };

    //[BindText(nameof(MainViewModel.GreetingMessage))]
    //private readonly Label _greetingLabel = new();

    //[BindCommand(nameof(MainViewModel.ResetCommand))]
    //private readonly Button _resetBtn = new() { Text = "Reset" };

    public MainWindow(MainViewModel viewModel)
    {
        // Initialize the screen config bound to THIS window
        _screen = new ScreenConfiguration(this);

        Title = "Stanza Responsive v2";

        // --- Sidebar Logic ---
        _sidebar.X = 0;
        _sidebar.Y = Pos.Bottom(_header);

        // Media Query: 0 width on Small, 20 on Medium, 30 on Large+
        _sidebar.Width = Responsive.Dimension(_screen, size => size switch {
            ScreenSize.S => 0,
            ScreenSize.M => 20,
            _ => 30
        });

        _sidebar.Height = Dim.Fill(1);

        // --- Header Logic ---
        // Header height: 1 on Small, 3 on others
        _header.Height = Responsive.Dimension(_screen, size =>
            size == ScreenSize.S ? 1 : 3
        );
        _header.Width = Dim.Fill();

        // --- Content Logic ---
        _content.X = Pos.Right(_sidebar);
        _content.Y = Pos.Bottom(_header);
        _content.Width = Dim.Fill();
        _content.Height = Dim.Fill(1);

        Add(_header, _sidebar, _content, _statusBar);

        // Example: Content internal layout change based on screen size
        var greeting = new Label();
        _content.Add(greeting);

        // You can even use it for visibility or text
        this.ViewportChanged += (s, e) => {
            greeting.Text = $"Current Viewport Size: {_screen.CurrentSize}";
        };
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
