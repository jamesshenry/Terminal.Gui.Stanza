using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui.Stanza.Generators;

[Generator(LanguageNames.CSharp)]
public class TuiViewGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0,
                transform: (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(c => c != null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
        {
            var (compilation, classes) = source;

            var tuiViewAttributeSymbol = compilation.GetTypeByMetadataName("Terminal.Gui.Stanza.Abstractions.TuiViewAttribute");
            if (tuiViewAttributeSymbol == null) return;

            var parser = new TuiViewParser(tuiViewAttributeSymbol);
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

                    var orderedViews = resolver.ResolveOrder(viewDecl.LayoutConstraints, allViewNames);

                    var sourceCode = emitter.Emit(viewDecl, orderedViews);
                    spc.AddSource($"{viewDecl.ClassName}.g.cs", sourceCode);
                }
            }
        });
    }
}
