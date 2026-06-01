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

[TuiView<DemoViewModel>(Title = "Stanza MVVM Demo")]
public partial class DemoView : View
{
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

public class FileLogger : ILogger, System.IDisposable
{
    private readonly string _path = "stanza_bindings.log";
    private readonly System.Collections.Concurrent.BlockingCollection<string> _queue = new();
    private readonly System.Threading.Tasks.Task _writeTask;

    public FileLogger()
    {
        System.IO.File.WriteAllText(_path, $"=== Stanza Binding Log Started at {System.DateTime.Now} ===\n");
        _writeTask = System.Threading.Tasks.Task.Run(ProcessQueue);
    }

    public void Log(string message)
    {
        _queue.Add($"[{System.DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    private void ProcessQueue()
    {
        foreach (var msg in _queue.GetConsumingEnumerable())
        {
            System.IO.File.AppendAllText(_path, msg + "\n");
        }
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _writeTask.Wait();
        _queue.Dispose();
    }
}

public static class Program
{
    public static void Main()
    {
        using var logger = new FileLogger();
        StanzaConfig.Logger = logger;

        var app = Terminal.Gui.App.Application.Create();
        app.Init();

        var vm = new DemoViewModel();
        using var view = new DemoView(vm);
        view.X = Pos.Center();
        view.Y = Pos.Center();
        var window = new Window
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            
        };
        window.Add(view);
        app.Run(window);
        app.Dispose();
    }
}
