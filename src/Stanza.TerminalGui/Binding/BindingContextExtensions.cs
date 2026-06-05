namespace Stanza.TerminalGui;

/// <summary>
/// Provides fluent extension methods for managing the lifecycle of disposables
/// within a <see cref="BindingContext"/>.
/// </summary>
public static class BindingContextExtensions
{
    /// <summary>
    /// Fluently registers a disposable binding, command, or subscription into a <see cref="BindingContext"/>.
    /// </summary>
    /// <remarks>
    /// This is the primary way to manually attach subscriptions to a view's lifecycle
    /// inside <c>OnInitialized</c> or custom view logic.
    /// </remarks>
    /// <typeparam name="TDisposable">The specific type of the disposable being registered.</typeparam>
    /// <param name="disposable">The disposable object (e.g., a binding token returned by <see cref="BindingExtensions.Bind"/>).</param>
    /// <param name="context">The context that will manage this object's lifecycle. Typically <c>this.BindingContext</c>.</param>
    /// <returns>The original <paramref name="disposable"/> instance for further chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> or <paramref name="disposable"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// ViewModel.Bind(() => ViewModel.Name, n => Title = n).AddTo(BindingContext);
    /// </code>
    /// </example>
    public static TDisposable AddTo<TDisposable>(
        this TDisposable disposable,
        BindingContext context
    )
        where TDisposable : IDisposable
    {
        ArgumentNullException.ThrowIfNull(disposable);
        ArgumentNullException.ThrowIfNull(context);

        context.AddBinding(disposable);
        return disposable;
    }
}
