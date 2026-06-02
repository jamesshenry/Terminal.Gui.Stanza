namespace Terminal.Gui.Stanza.IR;

using Terminal.Gui.Stanza;

public record BindingInfo(
    string OwnerView,
    string PropertyName,
    string ViewModelProperty,
    BindingMode BindingMode = BindingMode.OneWay,
    string? Converter = null,
    bool RequiresToString = false
);
