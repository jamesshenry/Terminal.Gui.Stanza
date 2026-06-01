using System.Drawing;
using System.Linq;
using Terminal.Gui.Stanza;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Stanza.ClusterExplorer;

[TuiView<ClusterExplorerViewModel>]
public partial class CpuHistoryPanel : FrameView
{
    private ScatterSeries? _cpuSeries;
    private PathAnnotation? _cpuTrend;
    private const float VisibleHistoryWindowMs = 32000f;
    private bool _graphConfigured;
    private bool _refreshLoopStarted;

    public CpuHistoryPanel()
    {
        Title = "CPU History";
    }

    public GraphView HistoryGraph { get; private set; } = new()
    {
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };

    partial void OnInitialized()
    {
        HistoryGraph.Initialized += HistoryGraphOnInitialized;
    }

    private void HistoryGraphOnInitialized(object? sender, EventArgs e)
    {
        HistoryGraph.Initialized -= HistoryGraphOnInitialized;
        ConfigureGraph();
        UpdateGraph(ViewModel?.CpuHistory ?? []);

        if (_refreshLoopStarted)
        {
            return;
        }
App?.AddTimeout(TimeSpan.FromMilliseconds(250), () =>
        {
            UpdateGraph(ViewModel?.CpuHistory ?? []);
            return true;
        });

        _refreshLoopStarted = true;
    }

    private void ConfigureGraph()
    {
        _cpuSeries ??= new ScatterSeries();
        _cpuTrend ??= new PathAnnotation { BeforeSeries = true };

        HistoryGraph.Reset();
        HistoryGraph.MarginBottom = 2;
        HistoryGraph.MarginLeft = 5;
        var graphWidth = Math.Max(1, HistoryGraph.Viewport.Width - (int)HistoryGraph.MarginLeft);
        HistoryGraph.CellSize = new PointF(VisibleHistoryWindowMs / graphWidth, 2);
        HistoryGraph.ScrollOffset = new PointF(0, 0);
        HistoryGraph.AxisX.Increment = 5000;
        HistoryGraph.AxisX.ShowLabelsEvery = 1;
        HistoryGraph.AxisX.Text = "time ->";
        HistoryGraph.AxisX.LabelGetter = value => $"{value.Value / 1000:0}s";
        HistoryGraph.AxisX.Minimum = 0;
        HistoryGraph.AxisY.Increment = 10;
        HistoryGraph.AxisY.ShowLabelsEvery = 1;
        HistoryGraph.AxisY.Text = "%";
        HistoryGraph.AxisY.LabelGetter = value => ((int)value.Value).ToString();
        HistoryGraph.AxisY.Minimum = 0;

        HistoryGraph.Series.Clear();
        HistoryGraph.Annotations.Clear();
        HistoryGraph.Series.Add(_cpuSeries);
        HistoryGraph.Annotations.Add(_cpuTrend);
        _graphConfigured = true;
    }

    private void UpdateGraph(float[] cpuHistory)
    {
        if (!_graphConfigured)
        {
            return;
        }

        if (cpuHistory.Length == 0)
        {
            return;
        }

        _cpuSeries!.Points.Clear();
        _cpuTrend!.Points.Clear();

        var minCpu = cpuHistory.Min();
        var maxCpu = cpuHistory.Max();
        var graphHeight = Math.Max(1, HistoryGraph.Viewport.Height - (int)HistoryGraph.MarginBottom);
        var paddedMin = Math.Max(0, minCpu - 5);
        var paddedRange = Math.Max(10, (maxCpu - paddedMin) + 10);

        HistoryGraph.ScrollOffset = new PointF(0, paddedMin);
        HistoryGraph.CellSize = new PointF(HistoryGraph.CellSize.X, paddedRange / graphHeight);

        for (var index = 0; index < cpuHistory.Length; index++)
        {
            var point = new PointF(index * ClusterExplorerViewModel.SampleIntervalMs, cpuHistory[index]);
            _cpuSeries.Points.Add(point);
            _cpuTrend.Points.Add(point);
        }

        HistoryGraph.SetNeedsDraw();
    }
}
