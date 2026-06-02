namespace Stanza.TerminalGui;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class StanzaViewAttribute : Attribute
{
    /// <summary>
    /// Static title assigned to the view in the generated parameterless constructor.
    /// Applies to views that inherit from <see cref="Terminal.Gui.ViewBase.Window"/> or any
    /// Terminal.Gui type that exposes a <c>Title</c> property.
    /// </summary>
    public string? Title { get; set; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class StanzaViewAttribute<TViewModel> : StanzaViewAttribute
    where TViewModel : System.ComponentModel.INotifyPropertyChanged { }
