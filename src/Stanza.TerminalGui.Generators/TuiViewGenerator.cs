using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stanza.TerminalGui.Generators;

[Generator(LanguageNames.CSharp)]
public class StanzaViewGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor CircularLayoutDiagnostic = new(
        id: "STN001",
        title: "Circular layout dependency",
        messageFormat: "Circular layout dependency detected in '{0}'. Layout order may be unstable.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) =>
                    node is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0,
                transform: (ctx, _) => (ClassDeclarationSyntax)ctx.Node
            )
            .Where(c => c != null);

        var compilationAndClasses = context.CompilationProvider.Combine(
            classDeclarations.Collect()
        );

        context.RegisterSourceOutput(
            compilationAndClasses,
            (spc, source) =>
            {
                var (compilation, classes) = source;

                var stanzaViewAttributeSymbol = compilation.GetTypeByMetadataName(
                    "Stanza.TerminalGui.StanzaViewAttribute"
                );
                if (stanzaViewAttributeSymbol == null)
                    return;

                var genericStanzaViewAttributeSymbol = compilation.GetTypeByMetadataName(
                    "Stanza.TerminalGui.StanzaViewAttribute`1"
                );

                var parser = new StanzaViewParser(
                    stanzaViewAttributeSymbol,
                    genericStanzaViewAttributeSymbol
                );
                var resolver = new DependencyResolver();
                var emitter = new InitializeComponentEmitter();

                foreach (var classDecl in classes)
                {
                    var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                    var viewDecl = parser.Parse(classDecl, semanticModel);
                    if (viewDecl != null)
                    {
                        var allViewNames = new HashSet<string>();
                        foreach (var assignment in viewDecl.PropertyAssignments)
                        {
                            allViewNames.Add(assignment.OwnerView);
                        }
                        foreach (var constraint in viewDecl.LayoutConstraints)
                        {
                            allViewNames.Add(constraint.SourceView);
                            allViewNames.Add(constraint.ReferencedView);
                        }
                        foreach (var binding in viewDecl.Bindings)
                        {
                            allViewNames.Add(binding.OwnerView);
                        }

                        allViewNames.Remove("this");

                        var orderedViews = resolver.ResolveOrder(
                            viewDecl.LayoutConstraints,
                            allViewNames,
                            out var hasCycle
                        );
                        if (hasCycle)
                        {
                            var diagnostic = Diagnostic.Create(
                                CircularLayoutDiagnostic,
                                classDecl.Identifier.GetLocation(),
                                viewDecl.ClassName
                            );
                            spc.ReportDiagnostic(diagnostic);
                        }

                        var sourceCode = emitter.Emit(viewDecl, orderedViews);
                        spc.AddSource($"{viewDecl.ClassName}.g.cs", sourceCode);
                    }
                }
            }
        );
    }
}
