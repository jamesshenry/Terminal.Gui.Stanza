using Terminal.Gui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Stanza;
using Terminal.Gui.Stanza.Abstractions;
using Terminal.Gui.Stanza.Binding;
using Terminal.Gui.Stanza.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Terminal.Gui.Stanza.Demo;

public partial class DemoViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "Stanza User";
    [ObservableProperty]
    public partial bool ShowGreetings { get; set; } = true;


    public string GreetingMessage => $"Hello, {Name}! Welcome to Terminal.Gui.Stanza!";

    partial void OnNameChanged(string value)
    {
        OnPropertyChanged(nameof(GreetingMessage));
    }

    [RelayCommand]
    private void Reset()
    {
        Name = "Stanza User";
        ShowGreetings = true;
    }
}

[TuiView(Title = "Stanza MVVM Demo")]
public partial class DemoView : BindableView<DemoViewModel>
{
    public DemoView(DemoViewModel viewModel) : base(viewModel)
    {
    }

    public Label TitleLabel { get; private set; } = new() 
    { 
        Text = "Terminal.Gui.Stanza Declarative Demo",
        Width = Dim.Auto(),
        Height = 1
    };

    public Label InstructionLabel { get; private set; } = new()
    {
        Text = "Type your name below to see dynamic bindings:",
        Width = Dim.Auto(),
        Height = 1,
        Below = nameof(TitleLabel)
    };

    public TextField NameInput { get; private set; } = new()
    {
        Width = 30,
        Height = 1,
        BindText = nameof(DemoViewModel.Name),
        Below = nameof(InstructionLabel),
        Enabled = true,
    };

    public CheckBox ShowGreetingsCheckbox { get; private set; } = new()
    {
        Text = "Show Greetings Banner",
        Width = Dim.Auto(),
        Height = 1,
        BindChecked = nameof(DemoViewModel.ShowGreetings),
        Below = nameof(NameInput)
    };

    public Label GreetingsLabel { get; private set; } = new()
    {
        Width = Dim.Auto(),
        Height = Dim.Auto(),
        BindText = nameof(DemoViewModel.GreetingMessage),
        BindVisible = nameof(DemoViewModel.ShowGreetings),
        Below = nameof(ShowGreetingsCheckbox)
    };

    public Button ResetButton { get; private set; } = new()
    {
        Text = "Reset Form",
        Width = Dim.Auto(),
        Height = Dim.Auto(),
        BindCommand = nameof(DemoViewModel.ResetCommand),
        Below = nameof(GreetingsLabel)
    };
}

public class FileLogger : ILogger
{
    private readonly string _path = "stanza_bindings.log";
    private readonly object _lock = new();

    public FileLogger()
    {
        lock (_lock)
        {
            System.IO.File.WriteAllText(_path, $"=== Stanza Binding Log Started at {System.DateTime.Now} ===\n");
        }
    }

    public void Log(string message)
    {
        lock (_lock)
        {
            System.IO.File.AppendAllText(_path, $"[{System.DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
    }
}

public static class Program
{
    public static void Main()
    {
        StanzaConfig.Logger = new FileLogger();

        var app = Terminal.Gui.App.Application.Create();
        app.Init();

        var vm = new DemoViewModel();
        using var view = new DemoView(vm);
        var window = new Window
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        window.Add(view);
        app.Run(window);
        app.Dispose();
    }
}
