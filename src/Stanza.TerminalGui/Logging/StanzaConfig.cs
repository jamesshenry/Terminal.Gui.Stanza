// File: src\Stanza.TerminalGui\Logging\ILogger.cs
namespace Stanza.TerminalGui;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
}

public static class StanzaConfig
{
    public static ILogger? Logger { get; set; }

    internal static void Trace(
        string message,
        LogLevel level = LogLevel.Debug,
        [System.Runtime.CompilerServices.CallerFilePath] string? category = null
    )
    {
        Logger?.Log(level, message, Path.GetFileNameWithoutExtension(category) ?? "Stanza");
    }
}
