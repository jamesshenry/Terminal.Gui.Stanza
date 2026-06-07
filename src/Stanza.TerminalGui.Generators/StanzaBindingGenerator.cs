using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stanza.TerminalGui.Generators;

[Generator(LanguageNames.CSharp)]
public class StanzaBindingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var viewProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Stanza.TerminalGui.StanzaViewAttribute`1",
            predicate: (node, _) => node is ClassDeclarationSyntax,
            transform: GetViewIR
        );

        context.RegisterSourceOutput(
            viewProvider,
            (spc, result) =>
            {
                foreach (var diag in result.Diagnostics)
                {
                    spc.ReportDiagnostic(diag);
                }

                if (result.ViewIr != null)
                {
                    var source = Emitter.Emit(result.ViewIr);
                    spc.AddSource($"{result.ViewIr.ClassName}.g.cs", source);
                }
            }
        );
    }

    private static ViewParseResult GetViewIR(
        GeneratorAttributeSyntaxContext context,
        CancellationToken token
    )
    {
        var classSymbol = context.TargetSymbol as INamedTypeSymbol;
        var classDeclaration = context.TargetNode as ClassDeclarationSyntax;

        if (classSymbol == null || classDeclaration == null)
            return new ViewParseResult(null, ImmutableArray<Diagnostic>.Empty);

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        // 1. STN010: Structural Check - Partial
        if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            diagnostics.Add(
                Diagnostic.Create(
                    StanzaDiagnostics.ClassMustBePartial,
                    classDeclaration.Identifier.GetLocation(),
                    classSymbol.Name
                )
            );
            // Fatal structural error - stop here
            return new ViewParseResult(null, diagnostics.ToImmutable());
        }

        // 2. STN011: Structural Check - Inherits from View
        // We check both the specific namespace and the short name to be resilient in test environments
        if (
            !classSymbol.IsSubtypeOf("Terminal.Gui.ViewBase.View")
            && classSymbol.BaseType?.Name != "View"
        )
        {
            diagnostics.Add(
                Diagnostic.Create(
                    StanzaDiagnostics.MustInheritFromView,
                    classDeclaration.Identifier.GetLocation(),
                    classSymbol.Name
                )
            );
            return new ViewParseResult(null, diagnostics.ToImmutable());
        }

        // 3. STN012: Member Collisions
        var reservedNames = new[]
        {
            "ViewModel",
            "BindingContext",
            "_bindingContext",
            "ApplyBindings",
        };
        foreach (var reserved in reservedNames)
        {
            var existing = classSymbol.GetMembers(reserved).FirstOrDefault();
            // Only flag if it was declared in the same syntax tree (the user's source)
            if (
                existing != null
                && existing.DeclaringSyntaxReferences.Any(r =>
                    r.SyntaxTree == classDeclaration.SyntaxTree
                )
            )
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        StanzaDiagnostics.MemberCollision,
                        existing.Locations.FirstOrDefault()
                            ?? classDeclaration.Identifier.GetLocation(),
                        classSymbol.Name,
                        reserved
                    )
                );
            }
        }

        var attribute = context.Attributes.First(a =>
            a.AttributeClass?.Name.Contains("StanzaViewAttribute") == true
        );
        var vmTypeSymbol =
            attribute.AttributeClass?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;

        if (vmTypeSymbol == null)
            return new ViewParseResult(null, diagnostics.ToImmutable());

        var vmType = vmTypeSymbol.ToDisplayString();
        var bindings = ImmutableArray.CreateBuilder<BindingIR>();
        var members = classSymbol
            .GetMembers()
            .Where(m => m is IPropertySymbol || m is IFieldSymbol);

        foreach (var member in members)
        {
            token.ThrowIfCancellationRequested();

            var memberBindings = member
                .GetAttributes()
                .Where(a =>
                    a.AttributeClass?.Name.StartsWith("Bind") == true
                    && a.AttributeClass.Name.EndsWith("Attribute")
                )
                .ToList();

            if (memberBindings.Count > 1)
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        StanzaDiagnostics.DuplicateBinding,
                        member.Locations.FirstOrDefault(),
                        member.Name
                    )
                );
            }

            foreach (var attr in memberBindings)
            {
                var attrName = attr.AttributeClass!.Name;
                var bindingType = attrName.Substring(0, attrName.Length - "Attribute".Length);
                var vmPropName = attr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                var attrLocation = attr.ApplicationSyntaxReference?.GetSyntax(token).GetLocation();

                if (string.IsNullOrEmpty(vmPropName))
                    continue;

                // STN020: Resolution - Search the full hierarchy for the property
                var vmProperty = vmTypeSymbol.FindPropertyInHierarchy(vmPropName);
                if (vmProperty == null)
                {
                    // Heuristic: Is this a CommunityToolkit Command that hasn't been generated yet?
                    bool isLikelyGeneratedCommand =
                        bindingType == "BindCommand"
                        && (vmPropName?.EndsWith("Command") == true)
                        && vmTypeSymbol
                            .GetMembers(
                                vmPropName.Substring(0, vmPropName.Length - "Command".Length)
                            )
                            .Any(m => m is IMethodSymbol);

                    // If it's not a known generated pattern, we should warn the user
                    if (!isLikelyGeneratedCommand)
                    {
                        diagnostics.Add(
                            Diagnostic.Create(
                                StanzaDiagnostics.PropertyNotFound,
                                attrLocation,
                                vmPropName,
                                vmType
                            )
                        );
                    }

                    // Even if it's likely generated, we must proceed with caution.
                    // We'll add it to the IR because the emitted code will use the string name anyway.
                    // If the property TRULY doesn't exist, the final C# compilation of the .g.cs will fail.
                    bindings.Add(new BindingIR(member.Name, bindingType, vmPropName!, "OneWay"));
                    continue;
                }

                // STN021: Resolution - Accessibility
                if (vmProperty.DeclaredAccessibility < Accessibility.Internal)
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            StanzaDiagnostics.PropertyInaccessible,
                            attrLocation,
                            vmPropName,
                            vmType
                        )
                    );
                    continue;
                }

                // STN022: Command Type Safety
                if (bindingType == "BindCommand")
                {
                    bool implementsCommand = vmProperty.Type.AllInterfaces.Any(i =>
                        i.ToDisplayString() == "System.Windows.Input.ICommand"
                        || i.Name == "ICommand"
                    );
                    if (!implementsCommand)
                    {
                        diagnostics.Add(
                            Diagnostic.Create(
                                StanzaDiagnostics.NotAnICommand,
                                attrLocation,
                                vmPropName
                            )
                        );
                        continue;
                    }
                }

                // STN030: Value Type Safety
                var boolRequired = new[] { "BindChecked", "BindVisible", "BindEnabled" };
                if (
                    boolRequired.Contains(bindingType)
                    && vmProperty.Type.SpecialType != SpecialType.System_Boolean
                )
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            StanzaDiagnostics.TypeMismatch,
                            attrLocation,
                            bindingType,
                            "bool",
                            vmPropName,
                            vmProperty.Type.ToDisplayString()
                        )
                    );
                    continue;
                }

                // Read-Only Logic
                bool isReadOnly = vmProperty.IsReadOnly;
                string mode =
                    (bindingType == "BindText" || bindingType == "BindChecked")
                        ? "TwoWay"
                        : "OneWay";
                bool isExplicitMode = false;

                var modeArg = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Mode");
                if (modeArg.Key != null && modeArg.Value.Value is int modeValue)
                {
                    mode = modeValue == 0 ? "OneWay" : "TwoWay";
                    isExplicitMode = true;
                }

                if (mode == "TwoWay" && isReadOnly)
                {
                    if (isExplicitMode)
                    {
                        diagnostics.Add(
                            Diagnostic.Create(
                                StanzaDiagnostics.InvalidTwoWayBinding,
                                attrLocation,
                                vmPropName
                            )
                        );
                        continue;
                    }
                    else
                    {
                        mode = "OneWay";
                        diagnostics.Add(
                            Diagnostic.Create(
                                StanzaDiagnostics.AutoDegradedBinding,
                                attrLocation,
                                vmPropName
                            )
                        );
                    }
                }

                bindings.Add(new BindingIR(member.Name, bindingType, vmPropName!, mode));
            }
        }

        // Resolve Namespace safely for Emitter
        string ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? "Global"
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ViewParseResult(
            new ViewIR(ns, classSymbol.Name, vmType, bindings.ToImmutable()),
            diagnostics.ToImmutable()
        );
    }
}
