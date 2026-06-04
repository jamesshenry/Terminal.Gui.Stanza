namespace Stanza.TerminalGui;

/// <summary>
/// Manages the lifecycle of multiple data bindings and event subscriptions.
/// Primarily used by generated views to ensure that all ViewModel subscriptions
/// are cleanly released during a ViewModel swap or View disposal.
/// </summary>
/// <remarks>
/// This class is not thread-safe for additions, but ensures that all managed
/// <see cref="IDisposable"/> objects are invoked exactly once during cleanup.
/// </remarks>
public class BindingContext : IDisposable
{
    private readonly List<IDisposable> _bindings = new();
    private bool _disposed;

    /// <summary>
    /// Registers a new binding or subscription to be managed by this context.
    /// </summary>
    /// <param name="binding">The disposable token representing the active binding.</param>
    /// <exception cref="ObjectDisposedException">Thrown if attempting to add a binding to a disposed context.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="binding"/> is null.</exception>
    public void AddBinding(IDisposable binding)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(binding);

        _bindings.Add(binding);
    }

    /// <summary>
    /// Disposes all registered bindings and clears the internal tracking list.
    /// Once called, the context is considered inactive.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var binding in _bindings)
            {
                binding.Dispose();
            }
            _bindings.Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
