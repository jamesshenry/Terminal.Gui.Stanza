namespace Terminal.Gui.Stanza;
/// <summary>
/// Manages the lifecycle of multiple bindings, providing centralized cleanup.
/// </summary>
public class BindingContext : IDisposable
{
    private readonly List<IDisposable> _bindings = new();
    private bool _disposed;

    /// <summary>
    /// Adds a binding to be managed by this context.
    /// </summary>
    public void AddBinding(IDisposable binding)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BindingContext));

        _bindings.Add(binding);
    }

    /// <summary>
    /// Disposes all managed bindings.
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
