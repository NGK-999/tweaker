using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class RoundedButton : Button
{
    private bool isHovering;

    protected override bool ShowFocusCues => false;

    public RoundedButton()
    {
        BorderRadius = 8;
        BorderColor = Color.FromArgb(42, 50, 66);
        NormalBorderColor = BorderColor;
        HoverBorderColor = BorderColor;
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
    public Color NormalBorderColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBorderColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBackColor { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        isHovering = true;
        BorderColor = HoverBorderColor;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        isHovering = false;
        BorderColor = NormalBorderColor;
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

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0)
        {
            base.OnPaint(e);
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundedPath(rect, BorderRadius);
        var fillColor = isHovering && HoverBackColor != Color.Empty ? HoverBackColor : BackColor;
        using var border = new Pen(BorderColor, 1F);
        e.Graphics.Clear(ResolveBackgroundColor(Parent));

        if (fillColor.A > 0)
        {
            using var background = new SolidBrush(fillColor);
            e.Graphics.FillPath(background, path);
        }

        e.Graphics.DrawPath(border, path);

        var textRect = Rectangle.FromLTRB(
            rect.Left + Padding.Left,
            rect.Top + Padding.Top,
            rect.Right - Padding.Right,
            rect.Bottom - Padding.Bottom);
        var flags = ResolveTextFlags(TextAlign);
        if (!UsesIconFont(Font))
        {
            flags |= TextFormatFlags.NoPadding;
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, flags);
    }

    private static TextFormatFlags ResolveTextFlags(ContentAlignment alignment)
    {
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;

        flags |= alignment switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => TextFormatFlags.Left,
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.HorizontalCenter
        };

        flags |= alignment switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => TextFormatFlags.Top,
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => TextFormatFlags.Bottom,
            _ => TextFormatFlags.VerticalCenter
        };

        return flags;
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

    private static bool UsesIconFont(Font font)
    {
        var family = font.FontFamily.Name;
        return family.Equals("Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase) ||
               family.Equals("Segoe MDL2 Assets", StringComparison.OrdinalIgnoreCase);
    }

    private static Color ResolveBackgroundColor(Control? control)
    {
        while (control is not null)
        {
            if (control.BackColor.A == 255 && control.BackColor != Color.Transparent)
            {
                return control.BackColor;
            }

            control = control.Parent;
        }

        return Color.FromArgb(42, 42, 42);
    }
}
