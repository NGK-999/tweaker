using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Windows.Forms;

namespace Renomeador.Forms.Components;

internal sealed class HeroBannerCard : UserControl
{
    private const int CornerRadius = 12;

    public HeroBannerCard()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        BackColor = Color.Transparent;
        ForeColor = Color.White;
        TitleText = "ApexTweaker";
        SubtitleText = "Auto-tuning com telemetria de silício, rollback e monitoramento de estabilidade em tempo real.";
        Padding = new Padding(24);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TitleText { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SubtitleText { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundedPath(bounds, CornerRadius);
        using var gradient = new LinearGradientBrush(
            bounds,
            Color.FromArgb(108, 92, 231),
            Color.FromArgb(232, 67, 147),
            LinearGradientMode.ForwardDiagonal);
        using var overlay = new LinearGradientBrush(
            bounds,
            Color.FromArgb(52, 255, 255, 255),
            Color.FromArgb(12, 255, 255, 255),
            LinearGradientMode.Vertical);
        using var border = new Pen(Color.FromArgb(120, 255, 255, 255), 1F);

        e.Graphics.FillPath(gradient, path);
        e.Graphics.FillPath(overlay, path);
        e.Graphics.DrawPath(border, path);

        using var titleFont = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
        using var subtitleFont = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        using var subtitleBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255));

        var titleRect = new Rectangle(Padding.Left, Padding.Top, Width - (Padding.Left * 2), 42);
        var subtitleRect = new Rectangle(Padding.Left, Padding.Top + 50, Width - (Padding.Left * 2), 50);

        TextRenderer.DrawText(
            e.Graphics,
            TitleText,
            titleFont,
            titleRect,
            Color.White,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        e.Graphics.DrawString(
            SubtitleText,
            subtitleFont,
            subtitleBrush,
            subtitleRect);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }
}
