namespace Stanza.TerminalGui;

/// <summary>
/// Marks a class for automatic generation with an explicit ViewModel type.
/// </summary>
/// <typeparam name="TViewModel">The type of the ViewModel to bind against.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class StanzaViewAttribute<TViewModel> : Attribute
    where TViewModel : System.ComponentModel.INotifyPropertyChanged
{
    /// <summary>
    /// Static title assigned to the view in the generated parameterless constructor.
    /// Applies to views that inherit from <see cref="Terminal.Gui.Views.Window"/> or any
    /// Terminal.Gui type that exposes a <c>Title</c> property.
    /// </summary>
    public string? Title { get; set; }
}
