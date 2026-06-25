using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfUserControl = System.Windows.Controls.UserControl;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using Renomeador.Services;

namespace ApexTweaker.UI.Wpf.Views;

public partial class TelemetryView : WpfUserControl
{
    private const int MaxSamples = 90;
    private const int MaxConsoleBlocks = 320;
    private readonly Queue<double> fpsSamples = new();
    private readonly Queue<double> onePercentSamples = new();

    public event Func<Task>? ToggleTelemetryRequested;

    public TelemetryView()
    {
        InitializeComponent();
        ConsoleBox.Document = CreateDocument();
    }

    public void SetBusy(bool busy)
    {
        BenchmarkButton.IsEnabled = !busy;
    }

    public void SetMonitoringButtonText(string text)
    {
        BenchmarkButton.Content = text;
    }

    public void SetMetrics(TelemetryMetricsSnapshot snapshot)
    {
        DpcLatencyText.Text = $"{snapshot.PeakDpcLatencyMicros:0} \u00B5s";
        BoostDropText.Text = $"{snapshot.BoostDropMhz:0} MHz";
        CpuPackageText.Text = snapshot.CpuPackageTemperatureC > 0
            ? $"{snapshot.CpuPackageTemperatureC:0} \u00B0C"
            : "-- \u00B0C";
        EffectiveClockText.Text = snapshot.EffectiveGameClockMhz > 0
            ? $"{snapshot.EffectiveGameClockMhz:0} MHz"
            : "0 MHz";
    }

    public void AddTelemetryPoint(TelemetryHistoryPoint point)
    {
        Enqueue(fpsSamples, point.FPS);
        Enqueue(onePercentSamples, point.OnePercentLowFps);
        ChartPlaceholderText.Visibility = Visibility.Collapsed;
        RedrawChart();
    }

    public void ClearConsole()
    {
        ConsoleBox.Document = CreateDocument();
    }

    public void SetConsoleLines(IReadOnlyList<string> lines)
    {
        var document = CreateDocument();
        foreach (var line in lines)
        {
            document.Blocks.Add(CreateParagraph(line));
        }

        ConsoleBox.Document = document;
        ConsoleBox.ScrollToEnd();
    }

    public void AppendConsoleLine(string line)
    {
        var document = ConsoleBox.Document ?? CreateDocument();
        document.Blocks.Add(CreateParagraph(line));

        while (document.Blocks.Count > MaxConsoleBlocks)
        {
            document.Blocks.Remove(document.Blocks.FirstBlock);
        }

        ConsoleBox.Document = document;
        ConsoleBox.ScrollToEnd();
    }

    private async void BenchmarkButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ToggleTelemetryRequested is not null)
        {
            await ToggleTelemetryRequested.Invoke();
        }
    }

    private void ChartCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RedrawChart();
    }

    private void RedrawChart()
    {
        if (ChartCanvas.ActualWidth <= 2D || ChartCanvas.ActualHeight <= 2D || fpsSamples.Count == 0)
        {
            return;
        }

        FpsPolyline.Points = BuildPoints(fpsSamples, ChartCanvas.ActualWidth, ChartCanvas.ActualHeight);
        OnePercentPolyline.Points = BuildPoints(onePercentSamples, ChartCanvas.ActualWidth, ChartCanvas.ActualHeight);
    }

    private static FlowDocument CreateDocument()
    {
        return new FlowDocument
        {
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.Gainsboro,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left
        };
    }

    private static Paragraph CreateParagraph(string line)
    {
        return new Paragraph(new Run(line))
        {
            Margin = new Thickness(0),
            Foreground = ResolveBrush(line)
        };
    }

    private static System.Windows.Media.Brush ResolveBrush(string line)
    {
        if (line.Contains("[AVISO]", StringComparison.OrdinalIgnoreCase))
        {
            return System.Windows.Media.Brushes.Gold;
        }

        if (line.Contains("erro", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("falha", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("bloque", StringComparison.OrdinalIgnoreCase))
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 85));
        }

        if (line.Contains("aplicado", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("conclu", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("ativo", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("otimizado", StringComparison.OrdinalIgnoreCase))
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 102));
        }

        return System.Windows.Media.Brushes.Gainsboro;
    }

    private static PointCollection BuildPoints(IEnumerable<double> source, double width, double height)
    {
        var values = source.ToArray();
        if (values.Length == 0)
        {
            return [];
        }

        var max = Math.Max(240D, values.Max());
        var points = new PointCollection(values.Length);
        var xStep = values.Length == 1 ? width : width / Math.Max(1D, values.Length - 1D);

        for (var index = 0; index < values.Length; index++)
        {
            var x = index * xStep;
            var normalized = max <= 0D ? 0D : values[index] / max;
            var y = (height - 8D) - ((height - 16D) * normalized);
            points.Add(new System.Windows.Point(x, Math.Max(8D, Math.Min(height - 8D, y))));
        }

        return points;
    }

    private static void Enqueue(Queue<double> queue, double value)
    {
        queue.Enqueue(Math.Max(0D, value));
        while (queue.Count > MaxSamples)
        {
            _ = queue.Dequeue();
        }
    }
}


