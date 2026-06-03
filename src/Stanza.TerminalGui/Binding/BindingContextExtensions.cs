namespace Stanza.TerminalGui;

public static class BindingContextExtensions
{
    /// <summary>
    /// Fluently registers any disposable binding or subscription into a BindingContext.
    /// </summary>
    public static TDisposable AddTo<TDisposable>(
        this TDisposable disposable,
        BindingContext context
    )
        where TDisposable : IDisposable
    {
        context.AddBinding(disposable);
        return disposable;
    }
}
