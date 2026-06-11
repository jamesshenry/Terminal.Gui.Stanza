using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Demo;

[StanzaView<ResultsViewModel>]
public partial class ResultsDialog : Dialog
{
    private readonly GraphView _graphView;

    protected override bool OnAccepting(CommandEventArgs args)
    {
        if (base.OnAccepting(args))
        {
            return true;
        }
        return false;
    }

    public ResultsDialog(ResultsViewModel viewModel)
    {
        AddButton(new() { Text = "_Cancel" });
        AddButton(new() { Text = "_Ok" });
        Add(new CheckBox());
        ViewModel = viewModel;

        _graphView = new GraphView();
    }
}
