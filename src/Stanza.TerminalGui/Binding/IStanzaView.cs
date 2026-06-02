namespace Stanza.TerminalGui;

public interface IStanzaView<TViewModel> : IDisposable
    where TViewModel : System.ComponentModel.INotifyPropertyChanged
{
    TViewModel? ViewModel { get; set; }
    BindingContext BindingContext { get; }
}
