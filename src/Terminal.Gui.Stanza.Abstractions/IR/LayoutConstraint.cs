namespace Terminal.Gui.Stanza.Abstractions.IR;

public record LayoutConstraint(
    string SourceView,
    string TargetProperty,
    string ConstraintType,
    string ReferencedView
);
