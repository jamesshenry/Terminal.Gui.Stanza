using System;
using System.Collections.Specialized;
using System.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Stanza.TerminalGui.Demo;

[StanzaView<ResultsViewModel>]
public partial class ResultsDialog : Dialog
{
    private readonly GraphView graphView;

    protected override bool OnAccepting(CommandEventArgs args)
    {
        if (base.OnAccepting(args))
        {
            return true;
        }
        return false;
    }

    public ResultsDialog(ResultsViewModel viewModel)
    {
        ViewModel = viewModel;
        graphView = new GraphView()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill(),
            Width = Dim.Fill(),
        };

        Add(graphView);
        AddButton(new() { Text = "_Cancel" });
        AddButton(new() { Text = "_Ok" });
    }

    partial void OnApplyBindings(BindingContext context)
    {
        if (ViewModel == null)
            return;

        ViewModel
            .Snapshots.OnCollectionChanged(
                (s, e) =>
                {
                    graphView.Text = "This graph shows a sine wave";

                    ScatterSeries points = new();

                    PathAnnotation line = new()
                    {
                        // Draw line first so it does not draw over top of points or axis labels
                        BeforeSeries = true,
                    };

                    // Generate line graph with 2,000 points
                    for (float x = -500; x < 500; x += 0.5f)
                    {
                        points.Points.Add(new PointF(x, (float)Math.Sin(x)));
                        line.Points.Add(new PointF(x, (float)Math.Sin(x)));
                    }

                    graphView.Series.Add(points);
                    graphView.Annotations.Add(line);

                    // How much graph space each cell of the console depicts
                    graphView.CellSize = new PointF(0.1f, 0.1f);

                    // leave space for axis labels
                    graphView.MarginBottom = 2;
                    graphView.MarginLeft = 3;

                    // One axis tick/label per
                    graphView.AxisX.Increment = 0.5f;
                    graphView.AxisX.ShowLabelsEvery = 2;
                    graphView.AxisX.Text = "X →";
                    graphView.AxisX.LabelGetter = v => v.Value.ToString("N2");

                    graphView.AxisY.Increment = 0.2f;
                    graphView.AxisY.ShowLabelsEvery = 2;
                    graphView.AxisY.Text = "↑Y";
                    graphView.AxisY.LabelGetter = v => v.Value.ToString("N2");

                    graphView.ScrollOffset = new PointF(-2.5f, -1);

                    graphView.SetNeedsDraw();
                }
            )
            .AddTo(context);
    }
}
