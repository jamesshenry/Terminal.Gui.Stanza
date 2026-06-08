using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stanza.TerminalGui.Layout;
using Terminal.Gui.App;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration.Sources.Clear();

    builder.Services.AddSingleton<MainViewModel>();
    builder.Services.AddSingleton<ProfileViewModel>();
    builder.Services.AddSingleton<SettingsViewModel>();
    builder.Services.AddTransient<MainWindow>();
    builder.Services.AddSingleton(_ => Application.Create());
    builder.Services.AddSingleton<ScreenConfiguration>();
    using IHost host = builder.Build();
    await Run(host);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

async Task Run(IHost host)
{
    using var app = host.Services.GetRequiredService<IApplication>();
    app.Init();

    var mainWindow = host.Services.GetRequiredService<MainWindow>();
    app.Run(mainWindow);

    app.Dispose();
}
