using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Stanza.TerminalGui.Generators;

public record BindingIR(
    string ControlName,
    string BindingType,
    string ViewModelPropertyName,
    string Mode // "OneWay" or "TwoWay"
);

public record ViewIR(
    string Namespace,
    string ClassName,
    string ViewModelType,
    ImmutableArray<BindingIR> Bindings
);

public record ViewParseResult(ViewIR? ViewIr, ImmutableArray<Diagnostic> Diagnostics);
