namespace Terminal.Gui.Stanza;

/// <summary>
/// A simple disposable action that executes a delegate when disposed.
/// Used for cleaning up event handlers and bindings.
/// </summary>
public class DisposableAction : IDisposable
{
    private readonly Action _action;
    private bool _disposed;

    public DisposableAction(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _action();
            _disposed = true;
        }
    }
}
