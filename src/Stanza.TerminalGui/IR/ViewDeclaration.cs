using System.Collections.Generic;

namespace Terminal.Gui.Stanza.IR;

public record ViewDeclaration(
    string ClassName,
    string Namespace,
    string BaseType,
    IEnumerable<PropertyAssignment> PropertyAssignments,
    IEnumerable<BindingInfo> Bindings,
    IEnumerable<LayoutConstraint> LayoutConstraints,
    string? ViewModelType = null,
    bool GenerateParameterlessConstructor = false,
    bool GenerateViewModelConstructor = false,
    IEnumerable<string>? SubviewsWithViewModel = null,
    string? Title = null
);
