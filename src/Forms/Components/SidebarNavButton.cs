using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Renomeador.Forms.Components;

internal sealed class SidebarNavButton : Button
{
    private bool isHovering;
    private bool isSelected;

    public SidebarNavButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        BackColor = Color.Transparent;
        ForeColor = Color.FromArgb(200, 200, 200);
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Cursor = Cursors.Hand;
        TabStop = false;
        Radius = 10;
        IconGlyph = string.Empty;
        IconColor = Color.FromArgb(210, 210, 210);
        SelectedFillColor = ColorTranslator.FromHtml("#383838");
        HoverFillColor = Color.FromArgb(44, 44, 44);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string IconGlyph { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color IconColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SelectedFillColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverFillColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value)
            {
                return;
            }

            isSelected = value;
            Invalidate();
        }
    }

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

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using var clearBrush = new SolidBrush(Parent?.BackColor ?? SystemColors.ControlDarkDark);
        e.Graphics.FillRectangle(clearBrush, ClientRectangle);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        var fillColor = IsSelected
            ? SelectedFillColor
            : isHovering
                ? HoverFillColor
                : Color.Transparent;

        if (fillColor.A > 0)
        {
            using var path = CreateRoundedPath(bounds, Radius);
            using var fillBrush = new SolidBrush(fillColor);
            e.Graphics.FillPath(fillBrush, path);
        }

        var iconRect = new Rectangle(12, 0, 18, Height);
        using var iconFont = new Font("Segoe Fluent Icons", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        TextRenderer.DrawText(
            e.Graphics,
            IconGlyph,
            iconFont,
            iconRect,
            IsSelected || isHovering ? Color.White : IconColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var textRect = new Rectangle(38, 0, Width - 50, Height);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textRect,
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1, radius * 2);
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
