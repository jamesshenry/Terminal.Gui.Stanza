namespace Terminal.Gui.Stanza;

/// <summary>
/// Global configuration for Terminal.Gui.Stanza, including logging setup.
/// </summary>
public static class StanzaConfig
{
    /// <summary>
    /// Gets or sets the logger used by Stanza to trace binding activity.
    /// </summary>
    public static ILogger? Logger { get; set; }
}
