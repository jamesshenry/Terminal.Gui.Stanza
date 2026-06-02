namespace Stanza.TerminalGui.IR;

using Stanza.TerminalGui;

public record BindingInfo(
    string OwnerView,
    string PropertyName,
    string ViewModelProperty,
    BindingMode BindingMode = BindingMode.OneWay,
    string? Converter = null,
    bool RequiresToString = false
);
