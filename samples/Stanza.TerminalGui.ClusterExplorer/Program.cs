using Stanza.TerminalGui.ClusterExplorer;
using Terminal.Gui.App;

var app = Application.Create();
app.Init();

using var vm = new ClusterExplorerViewModel();
using var mainWindow = new ClusterExplorerWindow(vm);

app.Run(mainWindow);
app.Dispose();
