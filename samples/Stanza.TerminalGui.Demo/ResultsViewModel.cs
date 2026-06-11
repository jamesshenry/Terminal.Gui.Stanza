using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Demo;

public class ResultsViewModel : ObservableObject
{
    public bool Result => true;

    public event EventHandler? RequestClose;
    public ObservableCollection<ISeries> Series { get; } = new();

    public void Initialize()
    {
        // TODO: finish implementing display of results
    }
}
