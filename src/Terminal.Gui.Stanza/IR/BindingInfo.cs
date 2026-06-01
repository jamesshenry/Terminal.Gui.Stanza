namespace Terminal.Gui.Stanza.Abstractions.IR;

using Terminal.Gui.Stanza.Abstractions;

public record BindingInfo(
    string OwnerView,
    string PropertyName,
    string ViewModelProperty,
    BindingMode BindingMode = BindingMode.OneWay,
    string? Converter = null
);
