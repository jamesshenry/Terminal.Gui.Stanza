using System.ComponentModel;
using System.Runtime.CompilerServices;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using System.Windows.Input;

namespace Terminal.Gui.Stanza.Binding;

public static class BindingExtensions
{
    extension(View view)
    {
        public string BindText { get => string.Empty; set { } }
        public string BindChecked { get => string.Empty; set { } }
        public string BindValue { get => string.Empty; set { } }
        public string BindCommand { get => string.Empty; set { } }
        public string BindVisible { get => string.Empty; set { } }
        public string BindEnabled { get => string.Empty; set { } }
    }

    #region Standard Extension Methods (Pre-C# 14 style or specifically for ObservableObject)

    /// <summary>
    /// Generic One-Way Binding: VM -> UI
    /// Works for strings, bools, ints, or custom objects.
    /// </summary>
    public static IDisposable Bind<T>(
        this INotifyPropertyChanged viewModel,
        Func<T> propertyExpression,
        Action<T> updateUi,
        [CallerArgumentExpression(nameof(propertyExpression))] string? expression = null
    )
    {
        string propertyName =
            expression?.Split('.').Last()?.Trim('(', ')')
            ?? throw new ArgumentException("Could not determine property name from expression.");

        return viewModel.Bind(propertyName, propertyExpression, updateUi);
    }

    public static IDisposable Bind<T>(
        this INotifyPropertyChanged viewModel,
        string propertyName,
        Func<T> propertyExpression,
        Action<T> updateUi
    )
    {
        viewModel.PropertyChanged += Handler;
        updateUi(propertyExpression());

        return new DisposableAction(() => viewModel.PropertyChanged -= Handler);

        void Handler(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal))
            {
                var newValue = propertyExpression();
                StanzaConfig.Logger?.Log($"[Bind] Property '{propertyName}' changed on ViewModel. Updating UI to '{newValue}'.");
                updateUi(newValue);
            }
        }
    }

    public static IDisposable BindTextTo(
        this INotifyPropertyChanged viewModel,
        View target,
        Func<string> getter,
        Action<string>? setter = null,
        [CallerArgumentExpression(nameof(getter))] string? expression = null
    )
    {
        string propertyName = expression?.Split('.').Last()?.Trim('(', ')') ?? "Text";
        return viewModel.BindTextTo(target, propertyName, getter, setter);
    }

    public static IDisposable BindTextTo(
        this INotifyPropertyChanged viewModel,
        View target,
        string propertyName,
        Func<string> getter,
        Action<string>? setter = null
    )
    {
        bool updatingFromUi = false;

        if (target is TextField textField)
        {
            var vmToUi = viewModel.Bind(
                propertyName,
                getter,
                val =>
                {
                    if (updatingFromUi) return;

                    if (textField.Value != val)
                    {
                        StanzaConfig.Logger?.Log($"[BindText] VM -> TextField Value update: '{val}'");
                        textField.Value = val;
                        textField.SetNeedsDraw();
                    }
                }
            );

            if (setter != null)
            {
                EventHandler<Terminal.Gui.App.ValueChangedEventArgs<string>> onValueChanged = (s, e) =>
                {
                    if (textField.Value == getter()) return;

                    updatingFromUi = true;
                    try
                    {
                        StanzaConfig.Logger?.Log($"[BindText] TextField Value -> VM update: '{textField.Value}'");
                        setter(textField.Value ?? string.Empty);
                    }
                    finally
                    {
                        updatingFromUi = false;
                    }
                };
                textField.ValueChanged += onValueChanged;

                return new DisposableAction(() =>
                {
                    vmToUi.Dispose();
                    textField.ValueChanged -= onValueChanged;
                });
            }

            return vmToUi;
        }
        else
        {
            var vmToUi = viewModel.Bind(
                propertyName,
                getter,
                val =>
                {
                    if (updatingFromUi) return;

                    if (target.Text != val)
                    {
                        StanzaConfig.Logger?.Log($"[BindText] VM -> UI text update on '{target.Id ?? target.GetType().Name}': '{val}'");
                        target.Text = val;
                        target.SetNeedsDraw();
                    }
                }
            );

            if (setter != null)
            {
                void OnTextChanged(object? s, EventArgs e)
                {
                    if (target.Text == getter()) return;

                    updatingFromUi = true;
                    try
                    {
                        StanzaConfig.Logger?.Log($"[BindText] UI -> VM text update on '{target.Id ?? target.GetType().Name}': '{target.Text}'");
                        setter(target.Text);
                    }
                    finally
                    {
                        updatingFromUi = false;
                    }
                }
                target.TextChanged += OnTextChanged;

                return new DisposableAction(() =>
                {
                    vmToUi.Dispose();
                    target.TextChanged -= OnTextChanged;
                });
            }

            return vmToUi;
        }
    }

    public static IDisposable BindCheckedTo(
        this INotifyPropertyChanged viewModel,
        CheckBox checkBox,
        Func<bool> getter,
        Action<bool> setter,
        [CallerArgumentExpression(nameof(getter))] string? expression = null
    )
    {
        string propertyName = expression?.Split('.').Last()?.Trim('(', ')') ?? "Checked";
        return viewModel.BindCheckedTo(checkBox, propertyName, getter, setter);
    }

    public static IDisposable BindCheckedTo(
        this INotifyPropertyChanged viewModel,
        CheckBox checkBox,
        string propertyName,
        Func<bool> getter,
        Action<bool> setter
    )
    {
        bool updatingFromUi = false;

        var vmToUi = viewModel.Bind(
            propertyName,
            getter,
            val =>
            {
                if (updatingFromUi) return;

                var newState = val ? CheckState.Checked : CheckState.UnChecked;
                if (checkBox.Value != newState)
                {
                    StanzaConfig.Logger?.Log($"[BindChecked] VM -> UI checked update on '{checkBox.Id ?? checkBox.GetType().Name}': '{val}'");
                    checkBox.Value = newState;
                    checkBox.SetNeedsDraw();
                }
            }
        );

        EventHandler<Terminal.Gui.App.ValueChangedEventArgs<CheckState>> onValueChanged = (s, e) =>
        {
            var isChecked = checkBox.Value == CheckState.Checked;
            if (isChecked == getter()) return;

            updatingFromUi = true;
            try
            {
                StanzaConfig.Logger?.Log($"[BindChecked] UI -> VM checked update on '{checkBox.Id ?? checkBox.GetType().Name}': '{isChecked}'");
                setter(isChecked);
            }
            finally
            {
                updatingFromUi = false;
            }
        };
        checkBox.ValueChanged += onValueChanged;

        return new DisposableAction(() =>
        {
            vmToUi.Dispose();
            checkBox.ValueChanged -= onValueChanged;
        });
    }

    public static IDisposable BindCommandTo(
        this INotifyPropertyChanged viewModel,
        ICommand command,
        Button button
    )
    {
        void UpdateEnabled(object? s, EventArgs e) => button.Enabled = command.CanExecute(null);
        void OnAccept(object? s, EventArgs e)
        {
            StanzaConfig.Logger?.Log($"[BindCommand] Button '{button.Id ?? button.GetType().Name}' clicked. Executing command.");
            command.Execute(null);
        }

        command.CanExecuteChanged += UpdateEnabled;
        button.Accepting += OnAccept;
        button.Enabled = command.CanExecute(null);

        return new DisposableAction(() =>
        {
            command.CanExecuteChanged -= UpdateEnabled;
            button.Accepting -= OnAccept;
        });
    }

    #endregion

    #region Generator Emitter Targets (Used by InitializeComponent)

    /// <summary>
    /// Two-Way String Binding: VM <-> View.Text
    /// </summary>
    public static IDisposable ApplyBindText<TViewModel>(
        this View target,
        TViewModel viewModel,
        string propertyName,
        Func<TViewModel, string> getter,
        Action<TViewModel, string>? setter = null
    ) where TViewModel : INotifyPropertyChanged
    {
        return viewModel.BindTextTo(target, propertyName, () => getter(viewModel), setter != null ? val => setter(viewModel, val) : null);
    }

    /// <summary>
    /// Two-Way Boolean Binding: VM <-> CheckBox
    /// </summary>
    public static IDisposable ApplyBindChecked<TViewModel>(
        this CheckBox checkBox,
        TViewModel viewModel,
        string propertyName,
        Func<TViewModel, bool> getter,
        Action<TViewModel, bool>? setter = null
    ) where TViewModel : INotifyPropertyChanged
    {
        return viewModel.BindCheckedTo(checkBox, propertyName, () => getter(viewModel), val => setter?.Invoke(viewModel, val));
    }

    /// <summary>
    /// Command: Connects a ViewModel command to a Button.
    /// </summary>
    public static IDisposable ApplyBindCommand<TViewModel>(
        this Button button,
        TViewModel viewModel,
        ICommand command
    ) where TViewModel : INotifyPropertyChanged
    {
        return viewModel.BindCommandTo(command, button);
    }

    /// <summary>
    /// Binds the Visible property to a VM property.
    /// </summary>
    public static IDisposable ApplyBindVisible<TViewModel>(
        this View view,
        TViewModel viewModel,
        string propertyName,
        Func<TViewModel, bool> getter
    ) where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(
            propertyName,
            () => getter(viewModel),
            val => view.Visible = val
        );
    }

    /// <summary>
    /// Binds the Enabled property to a VM property.
    /// </summary>
    public static IDisposable ApplyBindEnabled<TViewModel>(
        this View view,
        TViewModel viewModel,
        string propertyName,
        Func<TViewModel, bool> getter
    ) where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(
            propertyName,
            () => getter(viewModel),
            val => view.Enabled = val
        );
    }

    #endregion
}
