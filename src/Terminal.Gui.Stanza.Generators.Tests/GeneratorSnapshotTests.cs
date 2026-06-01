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
using Terminal.Gui.Stanza;
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
using Terminal.Gui.Stanza;
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
using Terminal.Gui.Stanza;
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

    [Test]
    public async Task ReportsCircularLayoutDependency_AsStn001Error()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TestNamespace;

public partial class CircularVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
}

[TuiView<CircularVm>]
public partial class CircularView : View
{
    public View LeftPanel { get; set; } = new() { Below = nameof(RightPanel) };
    public View RightPanel { get; set; } = new() { Below = nameof(LeftPanel) };
}
""";

        var diagnostics = TestHelper.VerifyDiagnostics(source);
        await Assert.That(diagnostics.Any(d => d.Id == "STN001" && d.Severity == DiagnosticSeverity.Error)).IsTrue();
    }

    [Test]
    public async Task SemanticNameof_Variants_CompileSuccessfully()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public partial class NameofVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name { get; set; }
}

public class NameofLabel : View
{
    public string Below { get; set; } = string.Empty;
    public string BindText { get; set; } = string.Empty;
}

[TuiView<NameofVm>]
public partial class NameofView : View
{
    public NameofLabel TitleLabel { get; set; } = new();
    public NameofLabel NameLabel { get; set; } = new() { Below = nameof(TestNamespace.NameofView.TitleLabel), BindText = nameof(TestNamespace.NameofVm.Name) };
}
""";

        var diagnostics = TestHelper.GetCompileDiagnostics(source);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            throw new Exception("Semantic nameof compile errors:\n" + string.Join("\n", errors));
        }

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CtorInferredVm_ImplementsInpc_GeneratesSuccessfully()
    {
        var source = """
using System.ComponentModel;
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public class ManualInpcVm : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name { get; set; } = string.Empty;
}

[TuiView]
public partial class InpcView : View
{
    public InpcView(ManualInpcVm vm)
    {
    }
}
""";

        var diagnostics = TestHelper.GetCompileDiagnostics(source);
        await Assert.That(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
    }

    [Test]
    public async Task Topology_DisjointViews_PreservesAll()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TestNamespace;

public partial class TopologyVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
}

[TuiView<TopologyVm>]
public partial class DisjointView : View
{
    public Label A { get; set; } = new() { Text = "A" };
    public Label B { get; set; } = new() { Text = "B" };
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "DisjointView.g.cs");
        await Assert.That(generated.Contains("this.Add(A);")).IsTrue();
        await Assert.That(generated.Contains("this.Add(B);")).IsTrue();
    }

    [Test]
    public async Task Topology_LinearDependencies_OrdersAThenBThenC()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TestNamespace;

public partial class TopologyVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
}

[TuiView<TopologyVm>]
public partial class LinearView : View
{
    public Label A { get; set; } = new();
    public Label B { get; set; } = new() { Below = nameof(A) };
    public Label C { get; set; } = new() { Below = nameof(B) };
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "LinearView.g.cs");
        var addA = generated.IndexOf("this.Add(A);", StringComparison.Ordinal);
        var addB = generated.IndexOf("this.Add(B);", StringComparison.Ordinal);
        var addC = generated.IndexOf("this.Add(C);", StringComparison.Ordinal);

        await Assert.That(addA >= 0 && addB >= 0 && addC >= 0).IsTrue();
        await Assert.That(addA < addB && addB < addC).IsTrue();
    }

    [Test]
    public async Task Topology_BranchingDependencies_PlacesRootFirst()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TestNamespace;

public partial class TopologyVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
}

[TuiView<TopologyVm>]
public partial class BranchingView : View
{
    public Label A { get; set; } = new();
    public Label B { get; set; } = new() { Below = nameof(A) };
    public Label C { get; set; } = new() { RightOf = nameof(A) };
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "BranchingView.g.cs");
        var addA = generated.IndexOf("this.Add(A);", StringComparison.Ordinal);
        var addB = generated.IndexOf("this.Add(B);", StringComparison.Ordinal);
        var addC = generated.IndexOf("this.Add(C);", StringComparison.Ordinal);

        await Assert.That(addA >= 0 && addB >= 0 && addC >= 0).IsTrue();
        await Assert.That(addA < addB).IsTrue();
        await Assert.That(addA < addC).IsTrue();
    }

    [Test]
    public async Task TitleAttribute_EmitsTitleAssignment_InGeneratedParameterlessCtor()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public partial class TitleVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
}

[TuiView<TitleVm>(Title = "My Form")]
public partial class TitleWindow : Window
{
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "TitleWindow.g.cs");
        await Assert.That(generated.Contains("this.Title = \"My Form\";", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task LayoutConstraint_EmitsAssignmentOnOwnerControl()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public partial class LayoutVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
}

public class LayoutLabel : View
{
    public string Below { get; set; } = string.Empty;
}

[TuiView<LayoutVm>]
public partial class LayoutView : View
{
    public LayoutLabel TitleLabel { get; set; } = new();
    public LayoutLabel NameInput { get; set; } = new() { Below = nameof(TitleLabel) };
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "LayoutView.g.cs");
        await Assert.That(generated.Contains("NameInput.Y = Pos.Bottom(TitleLabel);", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task SyntheticBelow_Assignment_IsInterceptedAndTranslated()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public partial class SyntheticVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
}

public class SyntheticLabel : View
{
    public string Below { get; set; } = string.Empty;
}

[TuiView<SyntheticVm>]
public partial class SyntheticView : View
{
    public SyntheticLabel OtherLabel { get; set; } = new();
    public SyntheticLabel MyLabel { get; set; } = new() { Below = nameof(OtherLabel) };
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "SyntheticView.g.cs");
        await Assert.That(generated.Contains("MyLabel.Y = Pos.Bottom(OtherLabel);", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ReadOnlyBindText_DegradesToOneWay_NoSetterLambdaEmitted()
    {
        var source = """
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;

namespace TestNamespace;

public class ReadOnlyVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Name => "ReadOnly";
}

public class BindingLabel : View
{
    public string BindText { get; set; } = string.Empty;
}

[TuiView<ReadOnlyVm>]
public partial class ReadOnlyBindingView : View
{
    public BindingLabel NameLabel { get; set; } = new() { BindText = nameof(ReadOnlyVm.Name) };
}
""";

        var generated = TestHelper.GetGeneratedSource(source, "ReadOnlyBindingView.g.cs");
        await Assert.That(generated.Contains("BindingContext.AddBinding(NameLabel.ApplyBindText(this.ViewModel!, \"Name\", vm => vm.Name));", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("(vm, val) => vm.Name = val", StringComparison.Ordinal)).IsFalse();
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
            throw new Exception("Compilation errors:\n" + string.Join("\n", compileDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
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
        var generated = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(gs => gs.HintName == hintName);

        if (generated.HintName is null)
        {
            throw new InvalidOperationException($"Generated source '{hintName}' was not found.");
        }

        return generated.SourceText.ToString();
    }

    private static (GeneratorDriver Driver, CSharpCompilation Compilation, GeneratorDriverRunResult RunResult, IReadOnlyList<Diagnostic> CompileDiagnostics) RunGenerator(string source)
    {
        // Force the JIT/runtime to load the dependent assemblies into the AppDomain
        _ = typeof(Terminal.Gui.Stanza.TuiViewAttribute).Assembly;
        _ = typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject).Assembly;
        _ = typeof(Terminal.Gui.Views.Label).Assembly;
        _ = typeof(Terminal.Gui.Stanza.IStanzaView<>).Assembly;

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
