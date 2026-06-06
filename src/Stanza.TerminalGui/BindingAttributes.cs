namespace Stanza.TerminalGui;

/// <summary>
/// Base attribute for all Stanza MVVM bindings.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    AllowMultiple = true,
    Inherited = true
)]
public abstract class StanzaBindingAttribute : Attribute
{
    /// <summary>
    /// The name of the ViewModel property to bind to.
    /// </summary>
    public string ViewModelPropertyName { get; }

    protected StanzaBindingAttribute(string viewModelPropertyName)
    {
        if (string.IsNullOrWhiteSpace(viewModelPropertyName))
            throw new ArgumentException(
                "ViewModel property name cannot be null or empty.",
                nameof(viewModelPropertyName)
            );

        ViewModelPropertyName = viewModelPropertyName;
    }
}

/// <summary>
/// Binds the Text property of the control to a ViewModel property.
/// Defaults to TwoWay binding for input controls (like TextField) and OneWay for statics (like Label).
/// </summary>
public sealed class BindTextAttribute : StanzaBindingAttribute
{
    public BindingMode Mode { get; set; } = BindingMode.TwoWay;

    public BindTextAttribute(string viewModelPropertyName)
        : base(viewModelPropertyName) { }
}

/// <summary>
/// Binds the Checked or Value state of a toggleable control (like CheckBox) to a boolean ViewModel property.
/// </summary>
public sealed class BindCheckedAttribute : StanzaBindingAttribute
{
    public BindingMode Mode { get; set; } = BindingMode.TwoWay;

    public BindCheckedAttribute(string viewModelPropertyName)
        : base(viewModelPropertyName) { }
}

/// <summary>
/// Binds the Visible property of a View to a boolean ViewModel property.
/// </summary>
public sealed class BindVisibleAttribute : StanzaBindingAttribute
{
    public BindVisibleAttribute(string viewModelPropertyName)
        : base(viewModelPropertyName) { }
}

/// <summary>
/// Binds the Enabled property of a View to a boolean ViewModel property.
/// </summary>
public sealed class BindEnabledAttribute : StanzaBindingAttribute
{
    public BindEnabledAttribute(string viewModelPropertyName)
        : base(viewModelPropertyName) { }
}

/// <summary>
/// Binds an ICommand from the ViewModel to a Button or interactive View.
/// Automatically synchronizes the Enabled state with CanExecute.
/// </summary>
public sealed class BindCommandAttribute : StanzaBindingAttribute
{
    public BindCommandAttribute(string viewModelPropertyName)
        : base(viewModelPropertyName) { }
}
