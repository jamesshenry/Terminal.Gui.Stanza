using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Stanza;

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

    #region Standard Extension Methods (VM -> UI / UI -> VM Master Methods)

    public static IDisposable BindOneWay<TViewModel, TValue, TView>(
        this TViewModel viewModel,
        string propertyName,
        TView view,
        System.Func<TViewModel, TValue> vmGetter,
        System.Action<TView, TValue> uiSetter
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(propertyName, () => vmGetter(viewModel), val => uiSetter(view, val));
    }

    public static IDisposable BindTwoWay<TViewModel, TValue, TView>(
        this TViewModel viewModel,
        string propertyName,
        TView view,
        System.Func<TViewModel, TValue> vmGetter,
        System.Action<TViewModel, TValue> vmSetter,
        System.Func<TView, TValue> uiGetter,
        System.Action<TView, TValue> uiSetter,
        System.Func<TView, System.Action, System.IDisposable> subscribeUiChange
    )
        where TViewModel : INotifyPropertyChanged
    {
        bool updating = false;

        var vmToUi = viewModel.Bind(
            propertyName,
            () => vmGetter(viewModel),
            val =>
            {
                if (updating)
                    return;
                updating = true;
                try
                {
                    uiSetter(view, val);
                }
                finally
                {
                    updating = false;
                }
            }
        );

        var uiSubscription = subscribeUiChange(
            view,
            () =>
            {
                if (updating)
                    return;
                var newVal = uiGetter(view);
                if (
                    System.Collections.Generic.EqualityComparer<TValue>.Default.Equals(
                        newVal,
                        vmGetter(viewModel)
                    )
                )
                    return;

                var viewId = (view as View)?.Id ?? view?.GetType().Name ?? "Unknown";
                StanzaConfig.Logger?.Log(
                    $"[BindTwoWay] UI -> VM update on '{viewId}' for property '{propertyName}': '{newVal}'"
                );

                updating = true;
                try
                {
                    vmSetter(viewModel, newVal);
                }
                finally
                {
                    updating = false;
                }
            }
        );

        return new DisposableAction(() =>
        {
            vmToUi.Dispose();
            uiSubscription.Dispose();
        });
    }

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
        if (target is TextField textField)
        {
            if (setter != null)
            {
                return viewModel.BindTwoWay(
                    propertyName,
                    textField,
                    _ => getter(),
                    (_, val) => setter(val),
                    tf => tf.Value ?? string.Empty,
                    (tf, val) =>
                    {
                        if (tf.Value != val)
                        {
                            StanzaConfig.Logger?.Log(
                                $"[BindText] VM -> TextField Value update: '{val}'"
                            );
                            tf.Value = val;
                        }
                    },
                    (tf, onChange) =>
                    {
                        EventHandler<Terminal.Gui.App.ValueChangedEventArgs<string>> handler = (
                            s,
                            e
                        ) =>
                        {
                            if (e.NewValue == e.OldValue)
                                return;
                            onChange();
                        };
                        tf.ValueChanged += handler;
                        return new DisposableAction(() => tf.ValueChanged -= handler);
                    }
                );
            }
            else
            {
                return viewModel.BindOneWay(
                    propertyName,
                    textField,
                    _ => getter(),
                    (tf, val) =>
                    {
                        if (tf.Value != val)
                        {
                            StanzaConfig.Logger?.Log(
                                $"[BindText] VM -> TextField Value update (OneWay): '{val}'"
                            );
                            tf.Value = val;
                        }
                    }
                );
            }
        }
        else
        {
            if (setter != null)
            {
                return viewModel.BindTwoWay(
                    propertyName,
                    target,
                    _ => getter(),
                    (_, val) => setter(val),
                    t => t.Text,
                    (t, val) =>
                    {
                        if (t.Text != val)
                        {
                            StanzaConfig.Logger?.Log(
                                $"[BindText] VM -> UI text update on '{t.Id ?? t.GetType().Name}': '{val}'"
                            );
                            t.Text = val;
                        }
                    },
                    (t, onChange) =>
                    {
                        EventHandler handler = (s, e) => onChange();
                        t.TextChanged += handler;
                        return new DisposableAction(() => t.TextChanged -= handler);
                    }
                );
            }
            else
            {
                return viewModel.BindOneWay(
                    propertyName,
                    target,
                    _ => getter(),
                    (t, val) =>
                    {
                        if (t.Text != val)
                        {
                            StanzaConfig.Logger?.Log(
                                $"[BindText] VM -> UI text update on '{t.Id ?? t.GetType().Name}' (OneWay): '{val}'"
                            );
                            t.Text = val;
                        }
                    }
                );
            }
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
        return viewModel.BindTwoWay(
            propertyName,
            checkBox,
            _ => getter(),
            (_, val) => setter(val),
            cb => cb.Value == CheckState.Checked,
            (cb, val) =>
            {
                var newState = val ? CheckState.Checked : CheckState.UnChecked;
                if (cb.Value != newState)
                {
                    StanzaConfig.Logger?.Log(
                        $"[BindChecked] VM -> UI checked update on '{cb.Id ?? cb.GetType().Name}': '{val}'"
                    );
                    cb.Value = newState;
                }
            },
            (cb, onChange) =>
            {
                EventHandler<Terminal.Gui.App.ValueChangedEventArgs<CheckState>> handler = (s, e) =>
                {
                    if (e.NewValue == e.OldValue)
                        return;
                    onChange();
                };
                cb.ValueChanged += handler;
                return new DisposableAction(() => cb.ValueChanged -= handler);
            }
        );
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
            StanzaConfig.Logger?.Log(
                $"[BindCommand] Button '{button.Id ?? button.GetType().Name}' clicked. Executing command."
            );
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
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.BindTextTo(
            target,
            propertyName,
            () => getter(viewModel),
            setter != null ? val => setter(viewModel, val) : null
        );
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
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.BindCheckedTo(
            checkBox,
            propertyName,
            () => getter(viewModel),
            val => setter?.Invoke(viewModel, val)
        );
    }

    /// <summary>
    /// Command: Connects a ViewModel command to a Button.
    /// </summary>
    public static IDisposable ApplyBindCommand<TViewModel>(
        this Button button,
        TViewModel viewModel,
        ICommand command
    )
        where TViewModel : INotifyPropertyChanged
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
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(
            propertyName,
            () => getter(viewModel),
            val =>
            {
                if (view.Visible != val)
                {
                    StanzaConfig.Logger?.Log(
                        $"[BindVisible] VM -> UI visibility update on '{view.Id ?? view.GetType().Name}': '{val}'"
                    );
                    view.Visible = val;
                }
            }
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
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(
            propertyName,
            () => getter(viewModel),
            val =>
            {
                if (view.Enabled != val)
                {
                    StanzaConfig.Logger?.Log(
                        $"[BindEnabled] VM -> UI enabled update on '{view.Id ?? view.GetType().Name}': '{val}'"
                    );
                    view.Enabled = val;
                }
            }
        );
    }

    #endregion
}
