namespace Stanza.TerminalGui;

/// <summary>
/// A simple disposable action that executes a delegate when disposed.
/// Used for cleaning up event handlers and bindings.
/// </summary>
public class DisposableAction : IDisposable
{
    private readonly Action _action;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableAction"/> class.
    /// </summary>
    /// <param name="action">The logic to execute upon disposal. Must not be null.</param>
    public DisposableAction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _action = action;
    }

    /// <summary>
    /// Executes the encapsulated cleanup action. Subsequent calls are ignored.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _action();
            _disposed = true;
        }
    }
}
