using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class GamerCard : Panel
{
    private static readonly Color CardBack = Color.FromArgb(17, 22, 34);
    private static readonly Color CardBorder = Color.FromArgb(31, 41, 61);

    public GamerCard()
    {
        BackColor = Color.Transparent;
        DoubleBuffered = true;
        Padding = new Padding(14);
        Margin = new Padding(0, 0, 0, 12);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = CardBack;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = CardBorder;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        using var path = CreateRoundedRectangle(bounds, 12);
        using var fill = new SolidBrush(FillColor);
        using var border = new Pen(BorderColor, 1F);

        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    protected override void OnResize(System.EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Invalidate();
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
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
