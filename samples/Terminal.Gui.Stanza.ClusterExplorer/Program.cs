using Terminal.Gui.App;

namespace Terminal.Gui.Stanza.ClusterExplorer;

public static class Program
{
    public static void Main()
    {
        var app = Application.Create();
        app.Init();

        using var vm = new ClusterExplorerViewModel();
        using var mainWindow = new ClusterExplorerWindow(vm);

        app.Run(mainWindow);
        app.Dispose();
    }
}
    