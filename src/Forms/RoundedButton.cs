using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class RoundedButton : Button
{
    private bool isHovering;

    public RoundedButton()
    {
        BorderRadius = 8;
        BorderColor = Color.FromArgb(42, 50, 66);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BorderRadius { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBackColor { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        isHovering = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        isHovering = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = CreateRoundedPath(new Rectangle(0, 0, Width, Height), BorderRadius);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0)
        {
            base.OnPaint(e);
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundedPath(rect, BorderRadius);
        using var background = new SolidBrush(isHovering && HoverBackColor != Color.Empty ? HoverBackColor : BackColor);
        using var border = new Pen(BorderColor, 1F);

        e.Graphics.FillPath(background, path);
        e.Graphics.DrawPath(border, path);

        var flags =
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.SingleLine;

        TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor, flags);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rectangle, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height)));
        var arc = new Rectangle(rectangle.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = rectangle.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rectangle.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rectangle.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }
}
