using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfUserControl = System.Windows.Controls.UserControl;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfBrush = System.Windows.Media.Brush;
using ApexTweaker.Services;

namespace ApexTweaker.UI.Wpf.Views;

public partial class TelemetryView : WpfUserControl
{
    private const int MaxSamples = 90;
    private const int MaxConsoleLines = 320;
    private const int ChartRedrawIntervalMs = 66;

    private readonly Queue<double> fpsSamples = new();
    private readonly Queue<double> onePercentSamples = new();
    private readonly ObservableCollection<ConsoleLineViewModel> consoleLines = [];
    private readonly DispatcherTimer chartRedrawTimer;
    private bool chartDirty;
    private DateTime lastMetricsUpdateUtc = DateTime.MinValue;
    private TelemetryMetricsSnapshot? pendingMetrics;

    public event Func<Task>? ToggleTelemetryRequested;

    public TelemetryView()
    {
        InitializeComponent();

        ConsoleList.ItemsSource = consoleLines;

        chartRedrawTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(ChartRedrawIntervalMs)
        };
        chartRedrawTimer.Tick += (_, _) =>
        {
            if (!chartDirty)
            {
                return;
            }

            chartDirty = false;
            RedrawChart();
        };
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
        pendingMetrics = snapshot;

        var now = DateTime.UtcNow;
        if ((now - lastMetricsUpdateUtc).TotalMilliseconds < 120D)
        {
            return;
        }

        lastMetricsUpdateUtc = now;
        ApplyMetrics(snapshot);
    }

    public void FlushPendingMetrics()
    {
        if (pendingMetrics is not null)
        {
            ApplyMetrics(pendingMetrics);
        }
    }

    public void AddTelemetryPoint(TelemetryHistoryPoint point)
    {
        Enqueue(fpsSamples, point.FPS);
        Enqueue(onePercentSamples, point.OnePercentLowFps);
        ChartPlaceholderText.Visibility = Visibility.Collapsed;

        chartDirty = true;
        if (!chartRedrawTimer.IsEnabled)
        {
            chartRedrawTimer.Start();
        }
    }

    public void ClearConsole()
    {
        consoleLines.Clear();
    }

    public void SetConsoleLines(IReadOnlyList<string> lines)
    {
        consoleLines.Clear();
        foreach (var line in lines)
        {
            consoleLines.Add(ConsoleLineViewModel.FromLine(line));
        }

        ScrollConsoleToEnd();
    }

    public void AppendConsoleLine(string line)
    {
        consoleLines.Add(ConsoleLineViewModel.FromLine(line));
        TrimConsoleLines();
        ScrollConsoleToEnd();
    }

    public void AppendConsoleLines(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        foreach (var line in lines)
        {
            consoleLines.Add(ConsoleLineViewModel.FromLine(line));
        }

        TrimConsoleLines();
        ScrollConsoleToEnd();
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
        chartDirty = true;
        RedrawChart();
    }

    private void ApplyMetrics(TelemetryMetricsSnapshot snapshot)
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

    private void RedrawChart()
    {
        if (ChartCanvas.ActualWidth <= 2D || ChartCanvas.ActualHeight <= 2D || fpsSamples.Count == 0)
        {
            return;
        }

        FpsPolyline.Points = BuildPoints(fpsSamples, ChartCanvas.ActualWidth, ChartCanvas.ActualHeight);
        OnePercentPolyline.Points = BuildPoints(onePercentSamples, ChartCanvas.ActualWidth, ChartCanvas.ActualHeight);
    }

    private void TrimConsoleLines()
    {
        while (consoleLines.Count > MaxConsoleLines)
        {
            consoleLines.RemoveAt(0);
        }
    }

    private void ScrollConsoleToEnd()
    {
        if (consoleLines.Count == 0)
        {
            return;
        }

        ConsoleList.ScrollIntoView(consoleLines[^1]);
    }

    private static PointCollection BuildPoints(IEnumerable<double> source, double width, double height)
    {
        var values = source as double[] ?? source.ToArray();
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

    private sealed class ConsoleLineViewModel
    {
        public required string Text { get; init; }

        public required WpfBrush Foreground { get; init; }

        public static ConsoleLineViewModel FromLine(string line)
        {
            return new ConsoleLineViewModel
            {
                Text = line,
                Foreground = ResolveBrush(line)
            };
        }

        private static WpfBrush ResolveBrush(string line)
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
    }
}
