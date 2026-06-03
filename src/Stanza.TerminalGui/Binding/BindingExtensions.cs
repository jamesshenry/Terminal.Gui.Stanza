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

    #region Core Standalone & Thread-Safe Primitives

    /// <summary>
    /// Thread-safe binding primitive used to bind a ViewModel property
    /// to an arbitrary UI action, using the target View as the thread dispatcher [1].
    /// </summary>
    public static IDisposable Bind<TViewModel, TValue>(
        this TViewModel viewModel,
        View dispatcher,
        Func<TValue> propertyExpression,
        Action<TValue> updateUi
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(
            propertyExpression,
            val =>
            {
                if (dispatcher.App != null)
                {
                    dispatcher.App.Invoke(() => updateUi(val)); // Marshal safely to UI thread [1]
                }
                else
                {
                    updateUi(val);
                }
            }
        );
    }

    /// <summary>
    /// Base binding method that extracts property names via compile-time argument expressions.
    /// </summary>
    public static IDisposable Bind<T>(
        this INotifyPropertyChanged viewModel,
        Func<T> propertyExpression,
        Action<T> updateUi,
        [CallerArgumentExpression(nameof(propertyExpression))] string? expression = null
    )
    {
        string propertyName = ExtractPropertyName(expression);
        return viewModel.Bind(propertyName, propertyExpression, updateUi);
    }

    /// <summary>
    /// Underlying reflection-free property subscription handler.
    /// </summary>
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
                updateUi(propertyExpression());
            }
        }
    }

    #endregion

    #region Thread-Safe Master Synchronizers (Internal)

    internal static IDisposable BindOneWay<TViewModel, TValue, TView>(
        this TViewModel viewModel,
        string propertyName,
        TView view,
        Func<TViewModel, TValue> vmGetter,
        Action<TView, TValue> uiSetter
    )
        where TViewModel : INotifyPropertyChanged
    {
        return viewModel.Bind(
            propertyName,
            () => vmGetter(viewModel),
            val =>
            {
                if (view is View v && v.App != null)
                {
                    v.App.Invoke(() => uiSetter(view, val)); // Marshal safely [1]
                }
                else
                {
                    uiSetter(view, val);
                }
            }
        );
    }

    internal static IDisposable BindTwoWay<TViewModel, TValue, TView>(
        this TViewModel viewModel,
        string propertyName,
        TView view,
        Func<TViewModel, TValue> vmGetter,
        Action<TViewModel, TValue> vmSetter,
        Func<TView, TValue> uiGetter,
        Action<TView, TValue> uiSetter,
        Func<TView, Action, IDisposable> subscribeUiChange
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
                    if (view is View v && v.App != null)
                    {
                        v.App.Invoke(() => uiSetter(view, val)); // Marshal safely [1]
                    }
                    else
                    {
                        uiSetter(view, val);
                    }
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
                if (EqualityComparer<TValue>.Default.Equals(newVal, vmGetter(viewModel)))
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

    #endregion

    #region Standard View Bindings (Auto-Marshalled)

    public static IDisposable BindTextTo(
        this INotifyPropertyChanged viewModel,
        View target,
        Func<string> getter,
        Action<string>? setter = null,
        [CallerArgumentExpression(nameof(getter))] string? expression = null
    )
    {
        string propertyName = ExtractPropertyName(expression);
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
                        tf.Value = val;
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

            return viewModel.BindOneWay(
                propertyName,
                textField,
                _ => getter(),
                (tf, val) =>
                {
                    tf.Value = val;
                }
            );
        }

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
                    t.Text = val;
                },
                (t, onChange) =>
                {
                    EventHandler handler = (s, e) => onChange();
                    t.TextChanged += handler;
                    return new DisposableAction(() => t.TextChanged -= handler);
                }
            );
        }

        return viewModel.BindOneWay(
            propertyName,
            target,
            _ => getter(),
            (t, val) =>
            {
                t.Text = val;
            }
        );
    }

    public static IDisposable BindCheckedTo(
        this INotifyPropertyChanged viewModel,
        CheckBox checkBox,
        Func<bool> getter,
        Action<bool> setter,
        [CallerArgumentExpression(nameof(getter))] string? expression = null
    )
    {
        string propertyName = ExtractPropertyName(expression);
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
                cb.Value = newState;
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
        void UpdateEnabled(object? s, EventArgs e)
        {
            if (button.App != null)
                button.App.Invoke(() => button.Enabled = command.CanExecute(null));
            else
                button.Enabled = command.CanExecute(null);
        }

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

    #endregion

    #region Generator Emitter Targets (Forwarders)

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
        return viewModel.BindOneWay(
            propertyName,
            view,
            _ => getter(viewModel),
            (v, val) =>
            {
                v.Visible = val;
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
        return viewModel.BindOneWay(
            propertyName,
            view,
            _ => getter(viewModel),
            (v, val) =>
            {
                v.Enabled = val;
            }
        );
    }

    #endregion

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
