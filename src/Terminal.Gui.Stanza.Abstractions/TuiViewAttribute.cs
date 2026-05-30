namespace Terminal.Gui.Stanza.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TuiViewAttribute : Attribute
{
    /// <summary>
    /// Title of the view (e.g., for windows/frames).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// If true, the generator will allow relative layout references (e.g., Y = Pos.Bottom(other)).
    /// Default: true.
    /// </summary>
    public bool AllowRelativeRefs { get; set; } = true;

    /// <summary>
    /// If true, the generator will emit a partial InitializeComponent() method.
    /// Default: true.
    /// </summary>
    public bool GenerateInitializeComponent { get; set; } = true;
}
