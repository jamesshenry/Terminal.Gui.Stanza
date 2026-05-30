using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminal.Gui.Stanza.Abstractions;
using Terminal.Gui.Stanza.Abstractions.IR;

namespace Terminal.Gui.Stanza.Generators;

internal class TuiViewParser
{
    private readonly INamedTypeSymbol _tuiViewAttributeSymbol;

    public TuiViewParser(INamedTypeSymbol tuiViewAttributeSymbol)
    {
        _tuiViewAttributeSymbol = tuiViewAttributeSymbol;
    }

    public ViewDeclaration? Parse(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol == null) return null;

        var tuiViewAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _tuiViewAttributeSymbol));

        if (tuiViewAttr == null) return null;

        // 1. Resolve TViewModel from ctor parameters first (Constructor Injection)
        INamedTypeSymbol? vmSymbol = null;
        foreach (var ctorSymbol in classSymbol.InstanceConstructors)
        {
            foreach (var param in ctorSymbol.Parameters)
            {
                var typeSymbol = param.Type as INamedTypeSymbol;
                if (typeSymbol != null && InheritsFromObservableObject(typeSymbol))
                {
                    vmSymbol = typeSymbol;
                    break;
                }
            }
            if (vmSymbol != null) break;
        }

        // 2. Fall back to generic base class argument if not found in ctor
        if (vmSymbol == null)
        {
            var baseType = classSymbol.BaseType;
            if (baseType != null && baseType.IsGenericType && baseType.TypeArguments.Length > 0)
            {
                vmSymbol = baseType.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        var assignments = new List<PropertyAssignment>();
        var bindings = new List<BindingInfo>();
        var constraints = new List<LayoutConstraint>();

        foreach (var member in classDecl.Members)
        {
            if (member is PropertyDeclarationSyntax prop)
            {
                ParseMemberInitializer(prop.Identifier.Text, prop.Initializer?.Value, assignments, bindings, constraints, vmSymbol);
            }
            else if (member is FieldDeclarationSyntax field)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    ParseMemberInitializer(variable.Identifier.Text, variable.Initializer?.Value, assignments, bindings, constraints, vmSymbol);
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
            vmSymbol?.ToDisplayString()
        );
    }

    private bool InheritsFromObservableObject(ITypeSymbol? typeSymbol)
    {
        var current = typeSymbol;
        while (current != null)
        {
            if (current.ToDisplayString() == "CommunityToolkit.Mvvm.ComponentModel.ObservableObject")
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    private void ParseMemberInitializer(
        string memberName,
        ExpressionSyntax? initializer,
        List<PropertyAssignment> assignments,
        List<BindingInfo> bindings,
        List<LayoutConstraint> constraints,
        INamedTypeSymbol? vmSymbol)
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
                        var vmProp = ExtractNameof(right);
                        var isWritable = true;
                        if (vmSymbol != null)
                        {
                            var propSymbol = vmSymbol.GetMembers(vmProp).OfType<IPropertySymbol>().FirstOrDefault();
                            if (propSymbol != null)
                            {
                                isWritable = !propSymbol.IsReadOnly;
                            }
                        }
                        var bindingMode = isWritable ? BindingMode.TwoWay : BindingMode.OneWay;
                        bindings.Add(new BindingInfo(memberName, left, vmProp, bindingMode));
                    }
                    else if (left == "Below" || left == "RightOf")
                    {
                        var referencedView = ExtractNameof(right);
                        var targetProp = left == "Below" ? "Y" : "X";
                        var constraintType = left == "Below" ? "Bottom" : "Right";
                        
                        assignments.Add(new PropertyAssignment(memberName, left, right, true));
                        constraints.Add(new LayoutConstraint(memberName, targetProp, constraintType, referencedView));
                    }
                    else if (right.Contains("Pos.") || left == "X" || left == "Y" || left == "Width" || left == "Height" || left == "PositionX" || left == "PositionY")
                    {
                        // Layout constraint
                        assignments.Add(new PropertyAssignment(memberName, left, right, true));
                        
                        // Try to extract relative reference
                        if (right.Contains("Pos."))
                        {
                            // Very crude extraction for MVP: Pos.Bottom(otherView)
                            var start = right.IndexOf('(');
                            var end = right.LastIndexOf(')');
                            if (start > 0 && end > start)
                            {
                                var referencedView = right.Substring(start + 1, end - start - 1);
                                var constraintType = right.Contains("Bottom") ? "Bottom" : "Right"; // Simplified
                                constraints.Add(new LayoutConstraint(memberName, left, constraintType, referencedView));
                            }
                        }
                    }
                    else
                    {
                        assignments.Add(new PropertyAssignment(memberName, left, right));
                    }
                }
            }
        }
    }

    private string ExtractNameof(string expression)
    {
        if (expression.StartsWith("nameof("))
        {
            return expression.Substring(7, expression.Length - 8).Split('.').Last();
        }
        return expression.Trim('"');
    }
}
