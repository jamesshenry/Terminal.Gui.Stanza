namespace Terminal.Gui.Stanza.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class TuiViewAttribute : Attribute
{
    public string? Title { get; set; }
}
