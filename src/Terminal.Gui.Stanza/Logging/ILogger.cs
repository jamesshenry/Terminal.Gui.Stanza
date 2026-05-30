namespace Terminal.Gui.Stanza;

/// <summary>
/// A lightweight logging interface for Stanza binding and lifecycle events.
/// </summary>
public interface ILogger
{
    void Log(string message);
}
