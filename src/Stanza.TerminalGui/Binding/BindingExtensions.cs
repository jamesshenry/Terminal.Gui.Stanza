using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

[assembly: InternalsVisibleTo("Stanza.TerminalGui.Tests")]

namespace Stanza.TerminalGui;

/// <summary>
/// Provides high-performance, reflection-free extension methods for binding
/// ViewModel properties to Terminal.Gui Views.
/// </summary>
/// <remarks>
/// These methods are designed to be NativeAOT compatible by avoiding runtime reflection
/// and utilizing compile-time captured member names via <see cref="CallerArgumentExpressionAttribute"/>.
/// </remarks>
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
    /// Establishes a thread-safe, one-way binding between a ViewModel property and a UI update action.
    /// Automatically marshals property updates to the Terminal.Gui Main Loop using the provided <paramref name="dispatcher"/>.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the ViewModel, which must implement <see cref="INotifyPropertyChanged"/>.</typeparam>
    /// <typeparam name="TValue">The type of the property value being bound.</typeparam>
    /// <param name="viewModel">The ViewModel instance containing the source property.</param>
    /// <param name="dispatcher">The <see cref="View"/> used to access the application main loop for thread-safe UI updates.</param>
    /// <param name="propertyExpression">A lambda expression used to retrieve the current property value from the ViewModel.</param>
    /// <param name="updateUi">An action executed on the UI thread whenever the property value changes.</param>
    /// <param name="expression">The string representation of the property expression, automatically captured at compile-time via <see cref="CallerArgumentExpressionAttribute"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the property change subscription and stops UI synchronization when disposed.</returns>
    public static IDisposable Bind<TViewModel, TValue>(
        this TViewModel viewModel,
        View dispatcher,
        Func<TViewModel, TValue> propertyExpression,
        Action<TValue> updateUi,
        [CallerArgumentExpression(nameof(propertyExpression))] string? expression = null
    )
        where TViewModel : INotifyPropertyChanged
    {
        string propertyName = ExtractPropertyName(expression);

        // 1. Subscribe to property changes [1]
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            if (string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal))
            {
                var newValue = propertyExpression(viewModel);

                // Safe UI thread marshalling [1]
                if (dispatcher.App != null)
                    dispatcher.App.Invoke(() => updateUi(newValue));
                else
                    updateUi(newValue);
            }
        };

        viewModel.PropertyChanged += handler;

        // 2. Perform initial synchronization [1]
        var initialValue = propertyExpression(viewModel);
        if (dispatcher.App != null)
            dispatcher.App.Invoke(() => updateUi(initialValue));
        else
            updateUi(initialValue);

        // 3. Return cleanup token [1]
        return new DisposableAction(() => viewModel.PropertyChanged -= handler);
    }

    /// <summary>
    /// Binds an <see cref="ICommand"/> to a Terminal.Gui <see cref="Button"/>.
    /// Synchronizes the button's <see cref="View.Enabled"/> state with <see cref="ICommand.CanExecute"/>
    /// and triggers <see cref="ICommand.Execute"/> when the button is accepted.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="button">The target button that will trigger the command and reflect its enabled state.</param>
    /// <returns>An <see cref="IDisposable"/> that unhooks the command and button events when disposed.</returns>
    public static IDisposable BindCommand(this ICommand command, Button button)
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

    /// <summary>
    /// Establishes a thread-safe, two-way binding between a ViewModel property and a UI element.
    /// </summary>
    public static IDisposable BindTwoWay<TViewModel, TValue>(
        this TViewModel viewModel,
        View dispatcher,
        Func<TViewModel, TValue> vmGetter,
        Action<TValue> vmSetter,
        Action<Action> subscribeUiChange,
        Func<TValue> uiGetter,
        Action<TValue> uiSetter,
        [CallerArgumentExpression(nameof(vmGetter))] string? expression = null
    )
        where TViewModel : INotifyPropertyChanged
    {
        string propertyName = ExtractPropertyName(expression);
        bool updating = false;

        // 1. VM -> UI (Uses your existing One-Way logic)
        var vmToUi = viewModel.Bind(
            dispatcher,
            vmGetter,
            val =>
            {
                if (updating)
                    return;
                updating = true;
                try
                {
                    uiSetter(val);
                }
                finally
                {
                    updating = false;
                }
            },
            expression
        );

        // 2. UI -> VM
        // The subscribeUiChange action should hook into events like TextChanged or ValueChanged
        var uiHandler = new Action(() =>
        {
            if (updating)
                return;

            var newVal = uiGetter();
            if (EqualityComparer<TValue>.Default.Equals(newVal, vmGetter(viewModel)))
                return;

            updating = true;
            try
            {
                vmSetter(newVal);
            }
            finally
            {
                updating = false;
            }
        });

        subscribeUiChange(uiHandler);

        // 3. Cleanup
        return new DisposableAction(() =>
        {
            vmToUi.Dispose();
            // Note: This primitive assumes the UI event unsubscription
            // is handled by the caller or by the View's lifecycle.
        });
    }

    #region Generator Apply Methods

    /// <summary>
    /// Generator target for text binding.
    /// </summary>
    public static IDisposable ApplyBindText<TViewModel>(
        this View target,
        TViewModel viewModel,
        string propertyName,
        Func<TViewModel, object> getter,
        Action<TViewModel, string>? setter = null
    )
        where TViewModel : INotifyPropertyChanged
    {
        if (setter != null)
        {
            return viewModel.BindTwoWay(
                target,
                getter,
                val => setter(viewModel, val?.ToString() ?? string.Empty),
                handler => target.TextChanged += (s, e) => handler(),
                () => target.Text?.ToString() ?? string.Empty,
                val => target.Text = val?.ToString() ?? string.Empty,
                propertyName
            );
        }
        else
        {
            return viewModel.Bind(
                target,
                getter,
                val => target.Text = val?.ToString() ?? string.Empty,
                propertyName
            );
        }
    }

    /// <summary>
    /// Generator target for checked binding.
    /// </summary>
    public static IDisposable ApplyBindChecked<TViewModel>(
        this CheckBox target,
        TViewModel viewModel,
        string propertyName,
        Func<TViewModel, bool> getter,
        Action<TViewModel, bool>? setter = null
    )
        where TViewModel : INotifyPropertyChanged
    {
        if (setter != null)
        {
            return viewModel.BindTwoWay(
                target,
                getter,
                val => setter(viewModel, val),
                handler => target.ValueChanged += (s, e) => handler(),
                () => target.Value == Terminal.Gui.Views.CheckState.Checked,
                val =>
                    target.Value = val
                        ? Terminal.Gui.Views.CheckState.Checked
                        : Terminal.Gui.Views.CheckState.UnChecked,
                propertyName
            );
        }
        else
        {
            return viewModel.Bind(
                target,
                getter,
                val =>
                    target.Value = val
                        ? Terminal.Gui.Views.CheckState.Checked
                        : Terminal.Gui.Views.CheckState.UnChecked,
                propertyName
            );
        }
    }

    /// <summary>
    /// Generator target for visible binding.
    /// </summary>
    public static IDisposable ApplyBindVisible<TViewModel>(
        this View target,
        TViewModel viewModel,
        string propertyName,
        Func<TViewModel, bool> getter
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(target, getter, val => target.Visible = val, propertyName);
    }

    /// <summary>
    /// Generator target for enabled binding.
    /// </summary>
    public static IDisposable ApplyBindEnabled<TViewModel>(
        this View target,
        TViewModel viewModel,
        string propertyName,
        Func<TViewModel, bool> getter
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(target, getter, val => target.Enabled = val, propertyName);
    }

    /// <summary>
    /// Generator target for command binding.
    /// </summary>
    public static IDisposable ApplyBindCommand<TViewModel>(
        this Button target,
        TViewModel viewModel,
        ICommand command
    )
        where TViewModel : INotifyPropertyChanged
    {
        return command.BindCommand(target);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Robust, non-reflection compile-time expression string parsing.
    /// Handles standard member lambdas, full paths, and local scope expressions cleanly [1].
    /// </summary>
    internal static string ExtractPropertyName(string? expression)
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
