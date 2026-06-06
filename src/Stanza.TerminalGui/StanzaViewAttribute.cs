namespace Stanza.TerminalGui;

/// <summary>
/// Marks a class for automatic UI and Binding generation by the Stanza Source Generator.
/// The target class must be declared as <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class StanzaViewAttribute : Attribute
{
    /// <summary>
    /// Static title assigned to the view in the generated parameterless constructor.
    /// Applies to views that inherit from <see cref="Terminal.Gui.Views.Window"/> or any
    /// Terminal.Gui type that exposes a <c>Title</c> property.
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// Marks a class for automatic generation with an explicit ViewModel type.
/// </summary>
/// <typeparam name="TViewModel">The type of the ViewModel to bind against.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class StanzaViewAttribute<TViewModel> : StanzaViewAttribute
    where TViewModel : System.ComponentModel.INotifyPropertyChanged { }
    
// 1. The Sizing Union
public union Sizing (Fill, Percent, Absolute);

public record struct Fill(int Margin = 0);
public record struct Percent(int Value);
public record struct Absolute(int Value);
