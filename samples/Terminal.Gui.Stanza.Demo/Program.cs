using Terminal.Gui.Drawing;

namespace Terminal.Gui.Stanza.Demo;
public static class Program
{
    public static void Main()
    {
                using var logger = new FileLogger();
        StanzaConfig.Logger = logger;
        var app = Terminal.Gui.App.Application.Create();
        app.Init();

        var vm = new DashboardViewModel();
        
        // The View just works because the trait injected 
        // the ViewModel-based constructor!
        using var mainView = new DashboardView(vm);

        app.Run(mainView);
        app.Dispose();
    }
}
