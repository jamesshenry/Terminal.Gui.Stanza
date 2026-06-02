namespace Stanza.TerminalGui;

/// <summary>
/// A lightweight logging interface for Stanza binding and lifecycle events.
/// </summary>
public interface ILogger
{
    void Log(string message);
}
