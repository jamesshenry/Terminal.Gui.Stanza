using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Terminal.Gui;
using Terminal.Gui.Stanza.Generators;
using VerifyTests;
using VerifyTUnit;

namespace Terminal.Gui.Stanza.Generators.Tests;

public class GeneratorSnapshotTests
{
    [Test]
    public Task GeneratesCorrectly()
    {
        var source = """
using Terminal.Gui.Stanza.Abstractions;
using Terminal.Gui.Stanza;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public partial class MyViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name { get; set; }
}

[TuiView]
public partial class MyView : View
{
    public MyView(MyViewModel viewModel)
    {
    }

    public Label MyLabel { get; set; } = new() { Text = "Hello" };
}
""";

        return TestHelper.Verify(source);
    }

    [Test]
    public Task GeneratesWithGenericAttribute()
    {
        var source = """
using Terminal.Gui.Stanza.Abstractions;
using Terminal.Gui.Stanza;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public partial class MyViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name { get; set; }
}

[TuiView<MyViewModel>]
public partial class MyView : View
{
    public Label MyLabel { get; set; } = new() { Text = "Hello" };
}
""";

        return TestHelper.Verify(source);
    }

    [Test]
    public Task GeneratesComplexLayouts()
    {
        var source = """
using Terminal.Gui.Stanza.Abstractions;
using Terminal.Gui.Stanza;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui;

namespace TestNamespace;

public class MyLabel : View
{
    public string Below { get; set; }
    public string RightOf { get; set; }
}

public partial class DashboardViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Summary { get; set; }
}

[TuiView<DashboardViewModel>]
public partial class DashboardView : Window
{
    public MyLabel LeftPanel { get; set; } = new();

    public MyLabel RightPanel { get; set; } = new() {
        RightOf = nameof(LeftPanel)
    };

    public MyLabel FooterLabel { get; set; } = new() {
        Y = Pos.AnchorEnd(1),
        X = Pos.Center(),
        Width = Dim.Fill()
    };
}
""";

        return TestHelper.Verify(source);
    }
}

public static class TestHelper
{
    public static Task Verify(string source)
    {
        // Force the JIT/runtime to load the dependent assemblies into the AppDomain
        _ = typeof(Terminal.Gui.Stanza.Abstractions.TuiViewAttribute).Assembly;
        _ = typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject).Assembly;
        _ = typeof(Terminal.Gui.Views.Label).Assembly;
        _ = typeof(Terminal.Gui.Stanza.Binding.IStanzaView<>).Assembly;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

        // Dynamically get all currently loaded assemblies in the AppDomain to populate compilation references
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TuiViewGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        if (runResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new Exception("Generator errors: " + string.Join("\n", runResult.Diagnostics));
        }

        // Add the generated trees to the compilation to do final semantic checking
        var finalCompilation = compilation;
        foreach (var generatedTree in runResult.GeneratedTrees)
        {
            finalCompilation = finalCompilation.AddSyntaxTrees(generatedTree);
        }

        var compileDiagnostics = finalCompilation.GetDiagnostics();
        if (compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new Exception("Compilation errors:\n" + string.Join("\n", compileDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        }

        return Verifier.Verify(driver);
    }
}
