using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stanza.TerminalGui;
using Stanza.TerminalGui.IR;

namespace Stanza.TerminalGui.Generators;

internal class TuiViewParser
{
    private readonly INamedTypeSymbol _tuiViewAttributeSymbol;
    private readonly INamedTypeSymbol? _genericTuiViewAttributeSymbol;

    public TuiViewParser(
        INamedTypeSymbol tuiViewAttributeSymbol,
        INamedTypeSymbol? genericTuiViewAttributeSymbol
    )
    {
        _tuiViewAttributeSymbol = tuiViewAttributeSymbol;
        _genericTuiViewAttributeSymbol = genericTuiViewAttributeSymbol;
    }

    public ViewDeclaration? Parse(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol == null)
            return null;

        var vmSymbol = GetViewModelSymbol(classSymbol);
        if (vmSymbol == null)
            return null;

        var title = GetTitle(classSymbol);

        bool hasParameterlessCtor = classSymbol.InstanceConstructors.Any(c =>
            c.Parameters.Length == 0 && !c.IsImplicitlyDeclared
        );

        bool hasViewModelCtor = classSymbol.InstanceConstructors.Any(c =>
            c.Parameters.Length == 1
            && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, vmSymbol)
        );

        bool generateParameterlessCtor = vmSymbol != null && !hasParameterlessCtor;
        bool generateViewModelCtor = vmSymbol != null && !hasViewModelCtor;

        var declaredSubviews = new HashSet<string>();
        foreach (var member in classDecl.Members)
        {
            if (member is PropertyDeclarationSyntax prop)
            {
                declaredSubviews.Add(prop.Identifier.Text);
            }
            else if (member is FieldDeclarationSyntax field)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    declaredSubviews.Add(variable.Identifier.Text);
                }
            }
        }

        var assignments = new List<PropertyAssignment>();
        var bindings = new List<BindingInfo>();
        var constraints = new List<LayoutConstraint>();
        var subviewsWithViewModel = new List<string>();

        foreach (var member in classDecl.Members)
        {
            if (member is PropertyDeclarationSyntax prop)
            {
                var propSymbol = semanticModel.GetDeclaredSymbol(prop) as IPropertySymbol;
                if (
                    propSymbol != null
                    && propSymbol.Type is INamedTypeSymbol typeSymbol
                    && HasViewModelPropertyOfType(typeSymbol, vmSymbol)
                )
                {
                    subviewsWithViewModel.Add(prop.Identifier.Text);
                }
                ParseMemberInitializer(
                    prop.Identifier.Text,
                    prop.Initializer?.Value,
                    assignments,
                    bindings,
                    constraints,
                    vmSymbol,
                    declaredSubviews,
                    semanticModel
                );
            }
            else if (member is FieldDeclarationSyntax field)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (
                        fieldSymbol != null
                        && fieldSymbol.Type is INamedTypeSymbol typeSymbol
                        && HasViewModelPropertyOfType(typeSymbol, vmSymbol)
                    )
                    {
                        subviewsWithViewModel.Add(variable.Identifier.Text);
                    }
                    ParseMemberInitializer(
                        variable.Identifier.Text,
                        variable.Initializer?.Value,
                        assignments,
                        bindings,
                        constraints,
                        vmSymbol,
                        declaredSubviews,
                        semanticModel
                    );
                }
            }
        }

        return new ViewDeclaration(
            classSymbol.Name,
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.BaseType?.ToDisplayString() ?? "Terminal.Gui.View",
            assignments,
            bindings,
            constraints,
            vmSymbol?.ToDisplayString(),
            generateParameterlessCtor,
            generateViewModelCtor,
            subviewsWithViewModel,
            title
        );
    }

    private bool HasViewModelPropertyOfType(INamedTypeSymbol typeSymbol, INamedTypeSymbol? vmSymbol)
    {
        if (vmSymbol == null)
            return false;

        // Tier 1: Check uncompiled source code attributes/constructors (before generation)
        var subviewVmSymbol = GetViewModelSymbol(typeSymbol);
        if (subviewVmSymbol != null)
        {
            return SymbolEqualityComparer.Default.Equals(subviewVmSymbol, vmSymbol);
        }

        // Tier 2: Fallback for pre-compiled/referenced binary libraries
        var current = typeSymbol;
        while (current != null)
        {
            var prop = current.GetMembers("ViewModel").OfType<IPropertySymbol>().FirstOrDefault();
            if (prop != null)
            {
                return SymbolEqualityComparer.Default.Equals(prop.Type, vmSymbol);
            }
            current = current.BaseType;
        }
        return false;
    }

    private string? GetTitle(INamedTypeSymbol classSymbol)
    {
        var tuiViewAttr = classSymbol
            .GetAttributes()
            .FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, _tuiViewAttributeSymbol)
                || (
                    _genericTuiViewAttributeSymbol != null
                    && a.AttributeClass != null
                    && SymbolEqualityComparer.Default.Equals(
                        a.AttributeClass.OriginalDefinition,
                        _genericTuiViewAttributeSymbol
                    )
                )
            );

        if (tuiViewAttr == null)
            return null;

        var namedArg = tuiViewAttr.NamedArguments.FirstOrDefault(a => a.Key == "Title");

        return namedArg.Key == "Title" ? namedArg.Value.Value as string : null;
    }

    private INamedTypeSymbol? GetViewModelSymbol(INamedTypeSymbol classSymbol)
    {
        var tuiViewAttr = classSymbol
            .GetAttributes()
            .FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, _tuiViewAttributeSymbol)
                || (
                    _genericTuiViewAttributeSymbol != null
                    && a.AttributeClass != null
                    && SymbolEqualityComparer.Default.Equals(
                        a.AttributeClass.OriginalDefinition,
                        _genericTuiViewAttributeSymbol
                    )
                )
            );

        if (tuiViewAttr != null)
        {
            // 1. Resolve TViewModel from generic attribute type argument first
            if (
                tuiViewAttr.AttributeClass != null
                && tuiViewAttr.AttributeClass.IsGenericType
                && tuiViewAttr.AttributeClass.TypeArguments.Length > 0
            )
            {
                return tuiViewAttr.AttributeClass.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        // 2. Fall back to ctor parameters (Constructor Injection)
        foreach (var ctorSymbol in classSymbol.InstanceConstructors)
        {
            foreach (var param in ctorSymbol.Parameters)
            {
                var typeSymbol = param.Type as INamedTypeSymbol;
                if (typeSymbol != null && ImplementsINotifyPropertyChanged(typeSymbol))
                {
                    return typeSymbol;
                }
            }
        }

        // 3. Fall back to generic base class argument if not found in ctor
        var baseType = classSymbol.BaseType;
        if (baseType != null && baseType.IsGenericType && baseType.TypeArguments.Length > 0)
        {
            return baseType.TypeArguments[0] as INamedTypeSymbol;
        }

        return null;
    }

    private bool ImplementsINotifyPropertyChanged(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        if (typeSymbol.ToDisplayString() == "System.ComponentModel.INotifyPropertyChanged")
        {
            return true;
        }

        return typeSymbol.AllInterfaces.Any(i =>
            i.ToDisplayString() == "System.ComponentModel.INotifyPropertyChanged"
        );
    }

    private void ParseMemberInitializer(
        string memberName,
        ExpressionSyntax? initializer,
        List<PropertyAssignment> assignments,
        List<BindingInfo> bindings,
        List<LayoutConstraint> constraints,
        INamedTypeSymbol? vmSymbol,
        HashSet<string> declaredSubviews,
        SemanticModel semanticModel
    )
    {
        InitializerExpressionSyntax? initializerExpr = null;
        if (initializer is ObjectCreationExpressionSyntax objCreation)
        {
            initializerExpr = objCreation.Initializer;
        }
        else if (initializer is ImplicitObjectCreationExpressionSyntax implicitObjCreation)
        {
            initializerExpr = implicitObjCreation.Initializer;
        }

        if (initializerExpr != null)
        {
            foreach (var expr in initializerExpr.Expressions)
            {
                if (expr is AssignmentExpressionSyntax assignment)
                {
                    var left = assignment.Left.ToString();
                    var right = assignment.Right.ToString();

                    if (left.StartsWith("Bind"))
                    {
                        // Binding: BindText = nameof(vm.Name)
                        var vmProp = ExtractNameof(assignment.Right, semanticModel);
                        var isWritable = true;
                        var isString = true;
                        if (vmSymbol != null)
                        {
                            var propSymbol = vmSymbol
                                .GetMembers(vmProp)
                                .OfType<IPropertySymbol>()
                                .FirstOrDefault();
                            if (propSymbol != null)
                            {
                                isWritable = !propSymbol.IsReadOnly;
                                isString = propSymbol.Type.SpecialType == SpecialType.System_String;
                            }
                        }

                        var bindingMode = isWritable ? BindingMode.TwoWay : BindingMode.OneWay;
                        var requiresToString = false;
                        if (left == "BindText" && !isString)
                        {
                            bindingMode = BindingMode.OneWay;
                            requiresToString = true;
                        }

                        bindings.Add(
                            new BindingInfo(
                                memberName,
                                left,
                                vmProp,
                                bindingMode,
                                null,
                                requiresToString
                            )
                        );
                    }
                    else if (left == "Below" || left == "RightOf")
                    {
                        var referencedView = ExtractNameof(assignment.Right, semanticModel);
                        var targetProp = left == "Below" ? "Y" : "X";
                        var targetExpr =
                            left == "Below"
                                ? $"Pos.Bottom({referencedView})"
                                : $"Pos.Right({referencedView})";

                        assignments.Add(new PropertyAssignment(memberName, targetProp, targetExpr));
                        constraints.Add(new LayoutConstraint(memberName, referencedView));
                    }
                    else
                    {
                        // Standard property/layout assignment (X, Y, Width, Height, Text, etc.)
                        assignments.Add(new PropertyAssignment(memberName, left, right));

                        // AST-based dependency detection: Find any references to other declared subviews
                        var referencedIdentifiers = assignment
                            .Right.DescendantNodes()
                            .OfType<IdentifierNameSyntax>()
                            .Select(id => id.Identifier.Text);

                        foreach (var identifier in referencedIdentifiers)
                        {
                            if (declaredSubviews.Contains(identifier) && identifier != memberName)
                            {
                                constraints.Add(new LayoutConstraint(memberName, identifier));
                            }
                        }
                    }
                }
            }
        }
    }

    private string ExtractNameof(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (
            expression is InvocationExpressionSyntax invocation
            && invocation.Expression is IdentifierNameSyntax identifier
            && identifier.Identifier.Text == "nameof"
            && invocation.ArgumentList.Arguments.Count == 1
        )
        {
            var argExpression = invocation.ArgumentList.Arguments[0].Expression;
            if (argExpression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.Identifier.Text;
            }

            if (argExpression is IdentifierNameSyntax argIdentifier)
            {
                return argIdentifier.Identifier.Text;
            }

            var symbol = semanticModel.GetSymbolInfo(argExpression).Symbol;
            if (symbol != null)
            {
                return symbol.Name;
            }

            return argExpression.ToString().Split('.').Last();
        }

        if (
            expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression)
        )
        {
            return literal.Token.ValueText;
        }

        if (expression is IdentifierNameSyntax identifierExpression)
        {
            return identifierExpression.Identifier.Text;
        }

        if (expression is MemberAccessExpressionSyntax accessExpression)
        {
            return accessExpression.Name.Identifier.Text;
        }

        return expression.ToString().Trim('"');
    }
}
