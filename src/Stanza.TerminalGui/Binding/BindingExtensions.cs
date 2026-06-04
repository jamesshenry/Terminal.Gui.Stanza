using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui;

public static class BindingExtensions
{
    extension(View view)
    {
        public string BindText
        {
            get => string.Empty;
            set { }
        }
        public string BindChecked
        {
            get => string.Empty;
            set { }
        }
        public string BindValue
        {
            get => string.Empty;
            set { }
        }
        public string BindCommand
        {
            get => string.Empty;
            set { }
        }
        public string BindVisible
        {
            get => string.Empty;
            set { }
        }
        public string BindEnabled
        {
            get => string.Empty;
            set { }
        }
    }
/// <summary>
    /// Universally binds any ViewModel property to a UI action safely across threads [1].
    /// </summary>
    public static IDisposable Bind<TViewModel, TValue>(
        this TViewModel viewModel,
        View dispatcher,
        Func<TViewModel, TValue> propertyExpression,
        Action<TValue> updateUi,
        [CallerArgumentExpression(nameof(propertyExpression))] string? expression = null
    ) where TViewModel : INotifyPropertyChanged
    {
        string propertyName = ExtractPropertyName(expression);

        // 1. Subscribe to property changes [1]
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            if (string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal))
            {
                var newValue = propertyExpression(viewModel);
                
                // Safe UI thread marshalling [1]
                if (dispatcher.App != null) dispatcher.App.Invoke(() => updateUi(newValue));
                else updateUi(newValue);
            }
        };

        viewModel.PropertyChanged += handler;

        // 2. Perform initial synchronization [1]
        var initialValue = propertyExpression(viewModel);
        if (dispatcher.App != null) dispatcher.App.Invoke(() => updateUi(initialValue));
        else updateUi(initialValue);

        // 3. Return cleanup token [1]
        return new DisposableAction(() => viewModel.PropertyChanged -= handler);
    }
    /// <summary>
    /// Binds an ICommand to a Button, handling click execution and thread-safe enablement [1].
    /// </summary>
    public static IDisposable BindCommand(
        this ICommand command,
        Button button
    )
    {
        void UpdateEnabled(object? s, EventArgs e)
        {
            if (button.App != null) 
                button.App.Invoke(() => button.Enabled = command.CanExecute(null)); // Safe marshal [1]
            else 
                button.Enabled = command.CanExecute(null);
        }
        
        void OnAccept(object? s, EventArgs e) => command.Execute(null);

        command.CanExecuteChanged += UpdateEnabled;
        button.Accepting += OnAccept;
        
        // Initial sync [1]
        UpdateEnabled(null, EventArgs.Empty);

        return new DisposableAction(() =>
        {
            command.CanExecuteChanged -= UpdateEnabled;
            button.Accepting -= OnAccept;
        });
    }
    #region Private Helpers

    /// <summary>
    /// Robust, non-reflection compile-time expression string parsing.
    /// Handles standard member lambdas, full paths, and local scope expressions cleanly [1].
    /// </summary>
    private static string ExtractPropertyName(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return "Text";
        }

        // 1. Strip the lambda arrow operator (e.g. "() => viewModel.Name" or "vm => vm.Name")
        if (expression.Contains("=>"))
        {
            expression = expression.Split(["=>"], StringSplitOptions.None).Last().Trim();
        }

        // 2. Extract final property name after any dots (e.g. "viewModel.Name" -> "Name")
        string propertyName = expression.Split('.').Last().Trim('(', ')', ' ');

        return propertyName;
    }

    #endregion
}
