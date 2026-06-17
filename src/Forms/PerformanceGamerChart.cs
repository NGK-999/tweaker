using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Renomeador.Services;

namespace Renomeador.Forms;

internal sealed class PerformanceGamerChart : UserControl
{
    private const int WindowSeconds = 60;
    private static readonly Color ChartBack = Color.FromArgb(17, 22, 37);
    private static readonly Color GridColor = Color.FromArgb(33, 40, 54);
    private static readonly Color FpsColor = Color.FromArgb(0, 180, 216);
    private static readonly Color OnePercentLowColor = Color.FromArgb(74, 222, 128);
    private static readonly Color ZeroPointOnePercentLowColor = Color.FromArgb(250, 204, 21);
    private static readonly Color CpuColor = Color.FromArgb(0, 123, 255);
    private static readonly Color RamColor = Color.FromArgb(168, 85, 247);
    private static readonly Color TempColor = Color.FromArgb(248, 113, 113);
    private static readonly Color TextColor = Color.FromArgb(255, 255, 255);

    private readonly object pointsSync = new();
    private readonly List<TelemetryHistoryPoint> points = [];
    private volatile bool suppressRendering;
    private string statusText = "Aguardando in\u00EDcio do jogo...";

    public PerformanceGamerChart()
    {
        DoubleBuffered = true;
        BackColor = ChartBack;
        Margin = new Padding(0);
        Padding = new Padding(0);
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SuppressRendering
    {
        get => suppressRendering;
        set => suppressRendering = value;
    }

    public void AddPoint(TelemetryHistoryPoint point)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddPoint(point)));
            return;
        }

        lock (pointsSync)
        {
            statusText = "Coletando telemetria...";
            points.Add(point);
            if (points.Count > WindowSeconds)
            {
                points.RemoveRange(0, points.Count - WindowSeconds);
            }
        }

        InvalidateIfAllowed();
    }

    public void Clear()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(Clear));
            return;
        }

        lock (pointsSync)
        {
            points.Clear();
        }

        InvalidateIfAllowed();
    }

    public void SetStatusText(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatusText(text)));
            return;
        }

        lock (pointsSync)
        {
            statusText = text;
        }

        InvalidateIfAllowed();
    }

    public void SetPoints(IEnumerable<TelemetryHistoryPoint> historyPoints)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetPoints(historyPoints)));
            return;
        }

        lock (pointsSync)
        {
            points.Clear();
            points.AddRange(historyPoints.TakeLast(WindowSeconds));
        }

        InvalidateIfAllowed();
    }

    private void InvalidateIfAllowed()
    {
        if (!suppressRendering)
        {
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(ChartBack);

        TelemetryHistoryPoint[] pointSnapshot;
        string currentStatusText;
        lock (pointsSync)
        {
            pointSnapshot = [.. points];
            currentStatusText = statusText;
        }

        var plot = new Rectangle(46, 36, Math.Max(1, Width - 80), Math.Max(1, Height - 68));
        DrawGrid(e.Graphics, plot);
        DrawLegend(e.Graphics, pointSnapshot.Length > 0 ? pointSnapshot[^1] : null);

        if (pointSnapshot.Length < 2)
        {
            DrawEmptyState(e.Graphics, plot, currentStatusText);
            return;
        }

        DrawSeries(e.Graphics, plot, pointSnapshot, point => Clamp(point.FPS, 0, 360), 360, FpsColor);
        DrawSeries(e.Graphics, plot, pointSnapshot, point => Clamp(point.CpuUsagePercentage, 0, 100), 100, CpuColor);
        DrawSeries(e.Graphics, plot, pointSnapshot, point => Clamp(point.RamUsagePercentage, 0, 100), 100, RamColor);
        DrawSeries(e.Graphics, plot, pointSnapshot, point => Clamp(Math.Max(point.CpuTemp, point.GpuTemp), 0, 110), 110, TempColor);
        DrawStutterNodes(e.Graphics, plot, pointSnapshot);
        DrawCurrentValueLabels(e.Graphics, plot, pointSnapshot);
    }

    private static void DrawGrid(Graphics graphics, Rectangle plot)
    {
        using var axisPen = new Pen(Color.FromArgb(45, 55, 72), 1F);
        using var gridPen = new Pen(GridColor, 1F);
        using var labelBrush = new SolidBrush(Color.FromArgb(107, 114, 128));
        using var font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);

        graphics.DrawRectangle(axisPen, plot);
        for (var i = 1; i < 5; i++)
        {
            var y = plot.Top + plot.Height * i / 5;
            graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
        }

        graphics.DrawString("360", font, labelBrush, 8, plot.Top - 2);
        graphics.DrawString("180", font, labelBrush, 8, plot.Top + plot.Height / 2 - 8);
        graphics.DrawString("0", font, labelBrush, 20, plot.Bottom - 12);
    }

    private static void DrawLegend(Graphics graphics, TelemetryHistoryPoint? latest)
    {
        var items = latest is null
            ? new[]
            {
                ("FPS", FpsColor),
                ("1% LOW", OnePercentLowColor),
                ("0.1% LOW", ZeroPointOnePercentLowColor),
                ("CPU", CpuColor),
                ("RAM", RamColor),
                ("TEMP", TempColor)
            }
            : new[]
        {
            ($"FPS {latest.FPS:0}", FpsColor),
            ($"1% {latest.OnePercentLowFps:0}", OnePercentLowColor),
            ($"0.1% {latest.ZeroPointOnePercentLowFps:0}", ZeroPointOnePercentLowColor),
            ($"CPU {latest.CpuUsagePercentage:0}%", CpuColor),
            ($"RAM {latest.RamUsagePercentage:0}%", RamColor),
            ($"TEMP {Math.Max(latest.CpuTemp, latest.GpuTemp):0}C", TempColor)
            };

        using var font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        var x = 48;
        foreach (var (label, color) in items)
        {
            using var brush = new SolidBrush(color);
            graphics.FillEllipse(brush, x, 6, 8, 8);
            graphics.DrawString(label, font, brush, x + 12, 2);
            x += Math.Max(78, (int)graphics.MeasureString(label, font).Width + 28);
        }
    }

    private static void DrawEmptyState(Graphics graphics, Rectangle plot, string text)
    {
        using var brush = new SolidBrush(Color.FromArgb(107, 114, 128));
        using var font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Aguardando pontos de telemetria...";
        }

        var size = graphics.MeasureString(text, font);
        graphics.DrawString(
            text,
            font,
            brush,
            plot.Left + (plot.Width - size.Width) / 2F,
            plot.Top + (plot.Height - size.Height) / 2F);
    }

    private static void DrawSeries(
        Graphics graphics,
        Rectangle plot,
        IReadOnlyList<TelemetryHistoryPoint> pointSnapshot,
        Func<TelemetryHistoryPoint, double> selector,
        double maxValue,
        Color color)
    {
        var series = BuildSeries(plot, pointSnapshot, selector, maxValue);
        if (series.Length < 2)
        {
            return;
        }

        using var path = new GraphicsPath();
        path.AddCurve(series, 0.35F);

        using var fillPath = (GraphicsPath)path.Clone();
        fillPath.AddLine(series[^1].X, series[^1].Y, series[^1].X, plot.Bottom);
        fillPath.AddLine(series[^1].X, plot.Bottom, series[0].X, plot.Bottom);
        fillPath.CloseFigure();

        using var areaBrush = new LinearGradientBrush(
            plot,
            Color.FromArgb(50, color),
            ChartBack,
            LinearGradientMode.Vertical);
        graphics.FillPath(areaBrush, fillPath);

        using var glowPen = new Pen(Color.FromArgb(70, color), 5F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var linePen = new Pen(color, 2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        graphics.DrawPath(glowPen, path);
        graphics.DrawPath(linePen, path);
    }

    private static PointF[] BuildSeries(
        Rectangle plot,
        IReadOnlyList<TelemetryHistoryPoint> pointSnapshot,
        Func<TelemetryHistoryPoint, double> selector,
        double maxValue)
    {
        var count = pointSnapshot.Count;
        var xStep = plot.Width / (float)(WindowSeconds - 1);
        var offset = WindowSeconds - count;
        var result = new PointF[count];

        for (var i = 0; i < count; i++)
        {
            var normalized = Clamp(selector(pointSnapshot[i]) / maxValue, 0, 1);
            result[i] = new PointF(
                plot.Left + xStep * (offset + i),
                plot.Bottom - (float)(normalized * plot.Height));
        }

        return result;
    }

    private static void DrawStutterNodes(Graphics graphics, Rectangle plot, IReadOnlyList<TelemetryHistoryPoint> pointSnapshot)
    {
        var count = pointSnapshot.Count;
        if (count == 0)
        {
            return;
        }

        using var nodeBrush = new SolidBrush(Color.FromArgb(248, 113, 113));
        var xStep = plot.Width / (float)(WindowSeconds - 1);
        var offset = WindowSeconds - count;

        for (var i = 0; i < count; i++)
        {
            var point = pointSnapshot[i];
            if (!point.SevereStutter)
            {
                continue;
            }

            var normalized = Clamp(point.FPS / 360D, 0, 1);
            var x = plot.Left + xStep * (offset + i);
            var y = plot.Bottom - (float)(normalized * plot.Height);
            graphics.FillEllipse(nodeBrush, x - 4, y - 4, 8, 8);
        }
    }

    private static void DrawCurrentValueLabels(Graphics graphics, Rectangle plot, IReadOnlyList<TelemetryHistoryPoint> pointSnapshot)
    {
        if (pointSnapshot.Count == 0)
        {
            return;
        }

        var latest = pointSnapshot[^1];
        DrawCurrentValueLabel(graphics, plot, latest.FPS, 360, $"{latest.FPS:0}", FpsColor);
        DrawCurrentValueLabel(graphics, plot, latest.CpuUsagePercentage, 100, $"{latest.CpuUsagePercentage:0}%", CpuColor);
        DrawCurrentValueLabel(graphics, plot, latest.RamUsagePercentage, 100, $"{latest.RamUsagePercentage:0}%", RamColor);
        DrawCurrentValueLabel(graphics, plot, Math.Max(latest.CpuTemp, latest.GpuTemp), 110, $"{Math.Max(latest.CpuTemp, latest.GpuTemp):0}C", TempColor);
    }

    private static void DrawCurrentValueLabel(Graphics graphics, Rectangle plot, double value, double maxValue, string text, Color color)
    {
        var normalized = Clamp(value / maxValue, 0, 1);
        var x = plot.Right + 6;
        var y = plot.Bottom - (float)(normalized * plot.Height);

        using var brush = new SolidBrush(color);
        using var font = new Font("Segoe UI Semibold", 8F, FontStyle.Regular, GraphicsUnit.Point);
        graphics.FillEllipse(brush, plot.Right - 4, y - 4, 8, 8);
        graphics.DrawString(text, font, brush, x, Math.Max(plot.Top, Math.Min(plot.Bottom - 14, y - 7)));
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Min(max, Math.Max(min, value));
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
    {
        using var path = new GraphicsPath();
        var diameter = radius * 2F;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
