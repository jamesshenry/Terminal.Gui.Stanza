using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


namespace Terminal.Gui.Stanza.Binding;

public static class BindingExtensions
{
    /// <summary>
    /// Generic One-Way Binding: VM -> UI
    /// Works for strings, bools, ints, or custom objects.
    /// </summary>
    public static IDisposable Bind<T>(
        this ObservableObject viewModel,
        Func<T> propertyExpression,
        Action<T> updateUi,
        [CallerArgumentExpression(nameof(propertyExpression))] string? expression = null
    )
    {
        string propertyName =
            expression?.Split('.').Last()
            ?? throw new ArgumentException("Could not determine property name from expression.");

        viewModel.PropertyChanged += Handler;
        updateUi(propertyExpression());

        return new DisposableAction(() => viewModel.PropertyChanged -= Handler);

        void Handler(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal))
            {
                updateUi(propertyExpression());
            }
        }
    }

    /// <summary>
    /// Two-Way String Binding: VM <-> TextField
    /// </summary>
    public static IDisposable BindText(
        this ObservableObject viewModel,
        View target,
        Func<string> getter,
        Action<string>? setter = null
    )
    {
        var vmToUi = viewModel.Bind(
            getter,
            val =>
            {
                if (target.Text != val)
                {
                    target.Text = val;
                    target.SetNeedsDraw();
                }
            }
        );

        if (setter != null)
        {
            void OnTextChanged(object? s, EventArgs e) => setter(target.Text);
            target.TextChanged += OnTextChanged;

            return new DisposableAction(() =>
            {
                vmToUi.Dispose();
                target.TextChanged -= OnTextChanged;
            });
        }

        return vmToUi;
    }

    /// <summary>
    /// Two-Way Boolean Binding: VM <-> CheckBox
    /// </summary>
    public static IDisposable BindChecked(
        this ObservableObject viewModel,
        CheckBox checkBox,
        Func<bool> getter,
        Action<bool> setter
    )
    {
        var vmToUi = viewModel.Bind(
            getter,
            val =>
            {
                var newState = val ? CheckState.Checked : CheckState.UnChecked;
                if (checkBox.Value != newState)
                {
                    checkBox.Value = newState;
                    checkBox.SetNeedsDraw();
                }
            }
        );

        void OnUiChanged(object? s, EventArgs e) => setter(checkBox.Value == CheckState.Checked);
        checkBox.Accepted += OnUiChanged;

        return new DisposableAction(() =>
        {
            vmToUi.Dispose();
            checkBox.Accepted -= OnUiChanged;
        });
    }

    /// <summary>
    /// Command: Connects a ViewModel command to a Button.
    /// </summary>
    public static IDisposable BindCommand(
        this ObservableObject _,
        IRelayCommand command,
        Button button
    )
    {
        void UpdateEnabled(object? s, EventArgs e) => button.Enabled = command.CanExecute(null);
        void OnAccept(object? s, EventArgs e) => command.Execute(null);

        command.CanExecuteChanged += UpdateEnabled;
        button.Accepting += OnAccept;
        button.Enabled = command.CanExecute(null);

        return new DisposableAction(() =>
        {
            command.CanExecuteChanged -= UpdateEnabled;
            button.Accepting -= OnAccept;
        });
    }
}
