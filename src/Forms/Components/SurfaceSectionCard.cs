using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Windows.Forms;

namespace Renomeador.Forms.Components;

internal sealed class SurfaceSectionCard : UserControl
{
    private readonly Panel contentHost;

    public SurfaceSectionCard()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        BackColor = Color.Transparent;
        Padding = new Padding(0);
        TitleText = string.Empty;

        contentHost = new Panel
        {
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        Controls.Add(contentHost);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TitleText { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control ContentHost => contentHost;

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        contentHost.Bounds = new Rectangle(18, 48, Math.Max(0, Width - 36), Math.Max(0, Height - 66));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundedPath(bounds, 12);
        using var fillBrush = new SolidBrush(ColorTranslator.FromHtml("#2A2A2A"));
        using var borderPen = new Pen(ColorTranslator.FromHtml("#333333"), 1F);

        e.Graphics.FillPath(fillBrush, path);
        e.Graphics.DrawPath(borderPen, path);

        using var titleFont = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
        var titleRect = new Rectangle(18, 16, Width - 36, 22);
        TextRenderer.DrawText(
            e.Graphics,
            TitleText,
            titleFont,
            titleRect,
            Color.White,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
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
