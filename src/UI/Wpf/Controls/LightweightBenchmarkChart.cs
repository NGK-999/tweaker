using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace ApexTweaker.UI.Wpf.Controls;

public sealed record BenchmarkChartPoint(
    int Second,
    double JavaMemoryMb,
    double AvailableMemoryMb,
    double CpuPercent,
    long CommitUsedMb);

public sealed class LightweightBenchmarkChart : FrameworkElement
{
    public static readonly DependencyProperty PointsSourceProperty = DependencyProperty.Register(
        nameof(PointsSource),
        typeof(IEnumerable),
        typeof(LightweightBenchmarkChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPointsChanged));

    private INotifyCollectionChanged? observedCollection;

    public IEnumerable? PointsSource
    {
        get => (IEnumerable?)GetValue(PointsSourceProperty);
        set => SetValue(PointsSourceProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        drawingContext.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromRgb(16, 24, 36)),
            new Pen(new SolidColorBrush(Color.FromRgb(45, 59, 78)), 1),
            bounds,
            10,
            10);

        var points = PointsSource?.Cast<BenchmarkChartPoint>().ToArray() ?? [];
        if (points.Length < 2 || ActualWidth < 40 || ActualHeight < 40)
        {
            return;
        }

        var plot = new Rect(14, 12, ActualWidth - 28, ActualHeight - 26);
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 92, 110, 136)), 1);
        for (var index = 1; index < 4; index++)
        {
            var y = plot.Top + plot.Height * index / 4d;
            drawingContext.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
        }

        DrawSeries(drawingContext, plot, points.Select(point => point.CpuPercent).ToArray(), 100, Color.FromRgb(245, 185, 66));
        var memoryMaximum = Math.Max(2048d, points.Max(point => Math.Max(point.JavaMemoryMb, point.AvailableMemoryMb)));
        DrawSeries(drawingContext, plot, points.Select(point => point.JavaMemoryMb).ToArray(), memoryMaximum, Color.FromRgb(71, 209, 140));
        DrawSeries(drawingContext, plot, points.Select(point => point.AvailableMemoryMb).ToArray(), memoryMaximum, Color.FromRgb(77, 163, 255));
    }

    private static void DrawSeries(
        DrawingContext drawingContext,
        Rect plot,
        IReadOnlyList<double> values,
        double maximum,
        Color color)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            for (var index = 0; index < values.Count; index++)
            {
                var x = plot.Left + plot.Width * index / Math.Max(1, values.Count - 1);
                var ratio = Math.Clamp(values[index] / Math.Max(1, maximum), 0, 1);
                var point = new Point(x, plot.Bottom - plot.Height * ratio);
                if (index == 0)
                {
                    context.BeginFigure(point, false, false);
                }
                else
                {
                    context.LineTo(point, true, false);
                }
            }
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(null, new Pen(new SolidColorBrush(color), 2), geometry);
    }

    private static void OnPointsChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        var chart = (LightweightBenchmarkChart)target;
        if (chart.observedCollection is not null)
        {
            chart.observedCollection.CollectionChanged -= chart.OnCollectionChanged;
        }

        chart.observedCollection = args.NewValue as INotifyCollectionChanged;
        if (chart.observedCollection is not null)
        {
            chart.observedCollection.CollectionChanged += chart.OnCollectionChanged;
        }

        chart.InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        InvalidateVisual();
    }
}
