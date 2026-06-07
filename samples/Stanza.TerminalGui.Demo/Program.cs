using Microsoft.Extensions.DependencyInjection;
using Stanza.TerminalGui;
using Stanza.TerminalGui.Demo;
using Terminal.Gui;
using Terminal.Gui.App;

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

// 4. Run
app.Run(mainView);
app.Dispose();
