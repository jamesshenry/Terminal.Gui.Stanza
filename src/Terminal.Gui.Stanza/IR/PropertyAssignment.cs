namespace Terminal.Gui.Stanza.Abstractions.IR;

public record PropertyAssignment(
    string OwnerView,
    string PropertyName,
    string ValueExpression,
    bool IsLayoutConstraint = false
);
