using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Renomeador.Forms.Components;

internal sealed class SidebarNavButton : Button
{
    private const int AnimationIntervalMs = 16;
    private const double HoverDurationMs = 170D;
    private const double SelectionDurationMs = 260D;
    private const float AnimationEpsilon = 0.003F;

    private readonly System.Windows.Forms.Timer animationTimer;
    private readonly Stopwatch animationStopwatch;
    private readonly Font iconFont;

    private bool isHovering;
    private bool isSelected;
    private float hoverProgress;
    private float selectionProgress;
    private float hoverStart;
    private float hoverTarget;
    private float selectionStart;
    private float selectionTarget;

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

        InactiveTextColor = Color.FromArgb(200, 200, 200);
        HoverTextColor = Color.White;
        SelectedTextColor = Color.White;
        InactiveIconColor = Color.FromArgb(210, 210, 210);
        HoverIconColor = Color.White;
        SelectedFillColor = ColorTranslator.FromHtml("#383838");
        HoverFillColor = Color.FromArgb(44, 44, 44);
        IndicatorColor = Color.FromArgb(0, 180, 216);

        iconFont = CreateIconFont();
        animationStopwatch = new Stopwatch();
        animationTimer = new System.Windows.Forms.Timer { Interval = AnimationIntervalMs };
        animationTimer.Tick += AnimationTimer_Tick;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string IconGlyph { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color InactiveTextColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverTextColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SelectedTextColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color InactiveIconColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverIconColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SelectedFillColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverFillColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color IndicatorColor { get; set; }

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
            StartAnimation();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            animationTimer.Dispose();
            animationStopwatch.Stop();
            iconFont.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        isHovering = true;
        StartAnimation();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        isHovering = false;
        StartAnimation();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.Clear(ResolveBackgroundColor(Parent));

        var bounds = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var hoverFillAlpha = (int)Math.Round(HoverFillColor.A * hoverProgress * (1F - (selectionProgress * 0.30F)));
        var selectedFillAlpha = (int)Math.Round(Math.Max(120, (int)SelectedFillColor.A) * selectionProgress);

        if (hoverFillAlpha > 0)
        {
            using var hoverPath = CreateRoundedPath(bounds, Radius);
            using var hoverBrush = new SolidBrush(Color.FromArgb(Math.Min(255, hoverFillAlpha), HoverFillColor));
            e.Graphics.FillPath(hoverBrush, hoverPath);
        }

        if (selectedFillAlpha > 0)
        {
            using var selectedPath = CreateRoundedPath(bounds, Radius);
            using var selectedBrush = new SolidBrush(Color.FromArgb(Math.Min(255, selectedFillAlpha), SelectedFillColor));
            e.Graphics.FillPath(selectedBrush, selectedPath);
        }

        if (selectionProgress > AnimationEpsilon)
        {
            var indicatorHeight = Math.Max(10, (int)Math.Round((Height - 16) * (0.45F + (selectionProgress * 0.55F))));
            var indicatorWidth = Math.Max(2, (int)Math.Round(2D + (2D * selectionProgress)));
            var indicatorY = (Height - indicatorHeight) / 2;
            var indicatorRect = new Rectangle(8, indicatorY, indicatorWidth, indicatorHeight);
            using var indicatorPath = CreateRoundedPath(indicatorRect, 2);
            using var indicatorBrush = new SolidBrush(Color.FromArgb((int)Math.Round(220 * selectionProgress), IndicatorColor));
            e.Graphics.FillPath(indicatorBrush, indicatorPath);
        }

        var emphasis = Math.Max(selectionProgress, hoverProgress * 0.90F);
        var textColor = LerpColor(InactiveTextColor, IsSelected ? SelectedTextColor : HoverTextColor, emphasis);
        var iconColor = LerpColor(InactiveIconColor, HoverIconColor, emphasis);
        var contentOffset = (int)Math.Round((hoverProgress * 1.5D) + (selectionProgress * 3D));

        var iconRect = new Rectangle(12 + contentOffset, 0, 18, Height);
        TextRenderer.DrawText(
            e.Graphics,
            IconGlyph,
            iconFont,
            iconRect,
            iconColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var textRect = new Rectangle(38 + contentOffset, 0, Width - 50 - contentOffset, Height);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textRect,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        var elapsedMs = animationStopwatch.Elapsed.TotalMilliseconds;
        var hoverProgressRatio = Math.Clamp(elapsedMs / HoverDurationMs, 0D, 1D);
        var selectionProgressRatio = Math.Clamp(elapsedMs / SelectionDurationMs, 0D, 1D);

        hoverProgress = Lerp(hoverStart, hoverTarget, EaseOutCubic(hoverProgressRatio));
        selectionProgress = Lerp(selectionStart, selectionTarget, EaseOutQuart(selectionProgressRatio));

        if (Math.Abs(hoverProgress - hoverTarget) <= AnimationEpsilon &&
            Math.Abs(selectionProgress - selectionTarget) <= AnimationEpsilon)
        {
            hoverProgress = hoverTarget;
            selectionProgress = selectionTarget;
            animationTimer.Stop();
            animationStopwatch.Stop();
        }

        Invalidate();
    }

    private void StartAnimation()
    {
        if (IsDisposed)
        {
            return;
        }

        hoverStart = hoverProgress;
        selectionStart = selectionProgress;
        hoverTarget = isHovering && !isSelected ? 1F : 0F;
        selectionTarget = isSelected ? 1F : 0F;

        animationStopwatch.Restart();
        if (!animationTimer.Enabled)
        {
            animationTimer.Start();
        }

        Invalidate();
    }

    private static float Lerp(float start, float end, double progress)
    {
        return (float)(start + ((end - start) * progress));
    }

    private static double EaseOutCubic(double progress)
    {
        return 1D - Math.Pow(1D - progress, 3D);
    }

    private static double EaseOutQuart(double progress)
    {
        return 1D - Math.Pow(1D - progress, 4D);
    }

    private static Color LerpColor(Color from, Color to, float progress)
    {
        var clamped = Math.Clamp(progress, 0F, 1F);
        var a = (int)Math.Round(from.A + ((to.A - from.A) * clamped));
        var r = (int)Math.Round(from.R + ((to.R - from.R) * clamped));
        var g = (int)Math.Round(from.G + ((to.G - from.G) * clamped));
        var b = (int)Math.Round(from.B + ((to.B - from.B) * clamped));
        return Color.FromArgb(a, r, g, b);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
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

    private static Font CreateIconFont()
    {
        try
        {
            return new Font("Segoe Fluent Icons", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            return new Font("Segoe MDL2 Assets", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        }
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

        return Color.FromArgb(37, 37, 37);
    }
}
