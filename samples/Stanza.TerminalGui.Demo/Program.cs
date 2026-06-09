using Microsoft.Extensions.DependencyInjection;
using Stanza.TerminalGui;
using Stanza.TerminalGui.Demo;
using Terminal.Gui;
using Terminal.Gui.App;

StanzaConfig.Logger = new StanzaDebugLogger();

var app = Application.Create();

// 1. Setup DI
var services = new ServiceCollection();
services.AddTransient<ProfileViewModel>();
services.AddTransient<ProfileView>();

var serviceProvider = services.BuildServiceProvider();

// 2. Initialize Terminal.Gui
app.Init();

// 3. Resolve the View (DI injects the ViewModel)
var mainView = serviceProvider.GetRequiredService<ProfileView>();

app.Dispose();

// In your Demo App Project
public class StanzaDebugLogger : ILogger
{
    public void Log(LogLevel level, string message, string category)
    {
        System.Diagnostics.Debug.WriteLine($"[{level}][{category}] {message}");
    }
}

// In Program.cs before App.Init()
