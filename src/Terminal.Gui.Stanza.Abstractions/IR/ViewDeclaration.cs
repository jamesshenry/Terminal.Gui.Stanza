using System.Collections.Generic;

namespace Terminal.Gui.Stanza.Abstractions.IR;

public record ViewDeclaration(
    string ClassName,
    string Namespace,
    string BaseType,
    IEnumerable<PropertyAssignment> PropertyAssignments,
    IEnumerable<BindingInfo> Bindings,
    IEnumerable<LayoutConstraint> LayoutConstraints,
    string? ViewModelType = null
);
