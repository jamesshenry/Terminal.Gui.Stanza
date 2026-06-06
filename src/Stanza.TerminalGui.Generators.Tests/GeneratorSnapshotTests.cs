using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stanza.TerminalGui.Generators;
using Terminal.Gui;
using VerifyTests;
using VerifyTUnit;

namespace Stanza.TerminalGui.Generators.Tests;

public class GeneratorSnapshotTests
{
    [Test]
    public Task GeneratesCorrectly()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public partial class MyViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name { get; set; }
}

[StanzaView<MyViewModel>]
public partial class MyView : View
{
    [BindText(nameof(MyViewModel.Name))]
    public Label MyLabel { get; set; } = new() { Text = "Hello" };
}
""";

        return TestHelper.Verify(source);
    }

    [Test]
    public Task GeneratesWithGenericAttribute()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public partial class MyViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name { get; set; }
}

[StanzaView<MyViewModel>]
public partial class MyView : View
{
    [BindText(nameof(MyViewModel.Name))]
    public Label MyLabel { get; set; } = new() { Text = "Hello" };
}
""";

        return TestHelper.Verify(source);
    }

    [Test]
    public async Task ReadOnlyBindText_DegradesToOneWay_NoSetterLambdaEmitted()
    {
        var source = """
using Stanza.TerminalGui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TestNamespace;

public class ReadOnlyVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name => "ReadOnly";
}

[StanzaView<ReadOnlyVm>]
public partial class ReadOnlyBindingView : View
{
    [BindText(nameof(ReadOnlyVm.Name))]
    public Label NameLabel { get; set; } = new();
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "ReadOnlyBindingView.g.cs");
        await Assert
            .That(
                generated.Contains(
                    "ApplyBindText(_viewModel, \"Name\", x => x.Name)",
                    StringComparison.Ordinal
                )
            )
            .IsTrue();
        await Assert
            .That(generated.Contains("(x, val) => x.Name = val", StringComparison.Ordinal))
            .IsFalse();
    }
}

public static class TestHelper
{
    public static Task Verify(string source)
    {
        var (driver, _, runResult, compileDiagnostics) = RunGenerator(source);

        if (runResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new Exception("Generator errors: " + string.Join("\n", runResult.Diagnostics));
        }

        if (compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new Exception(
                "Compilation errors:\n"
                    + string.Join(
                        "\n",
                        compileDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                    )
            );
        }

        return Verifier.Verify(driver);
    }

    public static IReadOnlyList<Diagnostic> VerifyDiagnostics(string source)
    {
        var (_, _, runResult, _) = RunGenerator(source);
        return runResult.Diagnostics;
    }

    public static IReadOnlyList<Diagnostic> GetCompileDiagnostics(string source)
    {
        var (_, _, _, compileDiagnostics) = RunGenerator(source);
        return compileDiagnostics;
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
        _ = typeof(Stanza.TerminalGui.StanzaViewAttribute).Assembly;
        _ = typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject).Assembly;
        _ = typeof(Terminal.Gui.Views.Label).Assembly;
        _ = typeof(Stanza.TerminalGui.IStanzaView<>).Assembly;

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
