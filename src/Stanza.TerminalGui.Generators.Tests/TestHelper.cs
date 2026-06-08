using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Stanza.TerminalGui.Generators.Tests;

public static class TestHelper
{
    public static Task VerifyGenerator(string source, params string[] ignoredDiagnostics)
    {
        var (driver, _, runResult, compileDiagnostics) = RunGenerator(source);

        if (runResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new Exception("Generator errors: " + string.Join("\n", runResult.Diagnostics));
        }

        var errors = compileDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Where(d => !ignoredDiagnostics.Contains(d.Id)) // Filter out CS0117, CS1061, etc.
            .ToList();

        if (errors.Count != 0)
        {
            throw new Exception(
                "Compilation errors:\n"
                    + string.Join(
                        "\n",
                        compileDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                    )
            );
        }

        return Verify(driver);
    }

    public static IReadOnlyList<Diagnostic> VerifyDiagnostics(string source)
    {
        var (_, _, runResult, _) = RunGenerator(source);
        return runResult.Diagnostics;
    }

    public static string GetGeneratedSource(string source, string hintName)
    {
        var (_, _, runResult, _) = RunGenerator(source);
        var generated = runResult
            .Results.SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(gs => gs.HintName == hintName);

        if (generated.HintName is null)
        {
            throw new InvalidOperationException($"Generated source '{hintName}' was not found.");
        }

        return generated.SourceText.ToString();
    }

    private static (
        GeneratorDriver Driver,
        CSharpCompilation Compilation,
        GeneratorDriverRunResult RunResult,
        IReadOnlyList<Diagnostic> CompileDiagnostics
    ) RunGenerator(string source)
    {
        // Force the JIT/runtime to load the dependent assemblies into the AppDomain
        _ = typeof(StanzaViewAttribute<>).Assembly;
        _ = typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject).Assembly;
        _ = typeof(Terminal.Gui.Views.Label).Assembly;
        _ = typeof(IStanzaView<>).Assembly;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Preview)
        );

        // Dynamically get all currently loaded assemblies in the AppDomain to populate compilation references
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new StanzaBindingGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview)
        );

        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        // Add the generated trees to the compilation to do final semantic checking
        var finalCompilation = compilation;
        foreach (var generatedTree in runResult.GeneratedTrees)
        {
            finalCompilation = finalCompilation.AddSyntaxTrees(generatedTree);
        }

        var compileDiagnostics = finalCompilation.GetDiagnostics();
        return (driver, compilation, runResult, compileDiagnostics);
    }
}
