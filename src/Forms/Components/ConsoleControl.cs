using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Renomeador.Forms.Components;

internal sealed class ConsoleControl : UserControl
{
    private static readonly Color ConsoleBackColor = Color.FromArgb(20, 20, 22);
    private static readonly Color ConsoleTextColor = Color.FromArgb(224, 224, 224);

    private readonly List<ConsoleRenderLine> renderLines = [];
    private readonly VScrollBar verticalScrollBar;
    private readonly Font terminalFont;
    private int lineHeight;

    public ConsoleControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        Margin = new Padding(0);
        Padding = new Padding(12, 10, 8, 10);
        BackColor = ConsoleBackColor;
        ForeColor = ConsoleTextColor;
        TabStop = false;

        terminalFont = CreateTerminalFont();
        lineHeight = terminalFont.Height + 6;

        verticalScrollBar = new VScrollBar
        {
            Dock = DockStyle.Right,
            SmallChange = 1,
            LargeChange = 1,
            Visible = false
        };

        verticalScrollBar.Scroll += (_, _) => Invalidate();
        Controls.Add(verticalScrollBar);
        verticalScrollBar.BringToFront();
    }

    public bool IsSurfaceReady => !IsDisposed && IsHandleCreated;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            terminalFont.Dispose();
            verticalScrollBar.Dispose();
        }

        base.Dispose(disposing);
    }

    public void SetEntries(IReadOnlyList<(string Text, Color Color)> lines)
    {
        if (IsDisposed)
        {
            return;
        }

        renderLines.Clear();
        foreach (var line in lines)
        {
            AppendNormalizedEntry(line.Text, line.Color.IsEmpty ? ConsoleTextColor : line.Color);
        }

        UpdateScrollBar();
        ScrollToBottom();
        RefreshSurface();
    }

    public void ClearEntries()
    {
        if (IsDisposed)
        {
            return;
        }

        renderLines.Clear();
        verticalScrollBar.Value = 0;
        UpdateScrollBar();
        RefreshSurface();
    }

    public void RefreshSurface()
    {
        if (IsDisposed)
        {
            return;
        }

        Invalidate();
        Update();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (IsDisposed)
        {
            return;
        }

        lineHeight = terminalFont.Height + 6;
        UpdateScrollBar();
        RefreshSurface();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!verticalScrollBar.Visible)
        {
            return;
        }

        var linesPerTick = Math.Max(1, SystemInformation.MouseWheelScrollLines);
        var delta = e.Delta > 0 ? -linesPerTick : linesPerTick;
        var nextValue = Math.Max(0, Math.Min(GetMaxTopLine(), verticalScrollBar.Value + delta));
        if (nextValue == verticalScrollBar.Value)
        {
            return;
        }

        verticalScrollBar.Value = nextValue;
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(ConsoleBackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(ConsoleBackColor);

        var viewport = GetViewportBounds();
        using var backgroundBrush = new SolidBrush(ConsoleBackColor);
        e.Graphics.FillRectangle(backgroundBrush, viewport);

        if (renderLines.Count == 0 || viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        var previousClip = e.Graphics.Clip;
        e.Graphics.SetClip(viewport);

        var drawFlags = TextFormatFlags.Left |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding |
                        TextFormatFlags.NoPrefix |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.PreserveGraphicsClipping;

        var topLine = Math.Max(0, Math.Min(verticalScrollBar.Value, GetMaxTopLine()));
        var visibleLineCapacity = GetVisibleLineCapacity();
        var maxLineIndex = Math.Min(renderLines.Count, topLine + visibleLineCapacity + 1);

        for (var index = topLine; index < maxLineIndex; index++)
        {
            var drawY = viewport.Top + ((index - topLine) * lineHeight);
            var bounds = new Rectangle(viewport.Left, drawY, viewport.Width, lineHeight);
            var line = renderLines[index];
            TextRenderer.DrawText(e.Graphics, line.Text, terminalFont, bounds, line.Color, drawFlags);
        }

        e.Graphics.Clip = previousClip;
    }

    private void AppendNormalizedEntry(string text, Color color)
    {
        var normalized = NormalizeConsoleText(text);
        if (normalized.Length == 0)
        {
            return;
        }

        var logicalLines = normalized.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var logicalLine in logicalLines)
        {
            var trimmed = logicalLine.TrimEnd();
            if (trimmed.Length == 0)
            {
                continue;
            }

            renderLines.Add(new ConsoleRenderLine(trimmed, color));
        }
    }

    private void UpdateScrollBar()
    {
        var visibleLineCapacity = GetVisibleLineCapacity();
        var maxTopLine = GetMaxTopLine(visibleLineCapacity);
        var scrollbarVisible = renderLines.Count > visibleLineCapacity;

        verticalScrollBar.Visible = scrollbarVisible;
        verticalScrollBar.Minimum = 0;
        verticalScrollBar.SmallChange = 1;
        verticalScrollBar.LargeChange = Math.Max(1, visibleLineCapacity);
        verticalScrollBar.Maximum = Math.Max(0, maxTopLine + verticalScrollBar.LargeChange - 1);

        if (verticalScrollBar.Value > maxTopLine)
        {
            verticalScrollBar.Value = maxTopLine;
        }
    }

    private void ScrollToBottom()
    {
        verticalScrollBar.Value = renderLines.Count == 0 ? 0 : GetMaxTopLine();
    }

    private int GetVisibleLineCapacity()
    {
        var viewportHeight = Math.Max(0, GetViewportBounds().Height);
        return Math.Max(1, viewportHeight / Math.Max(1, lineHeight));
    }

    private int GetMaxTopLine()
    {
        return GetMaxTopLine(GetVisibleLineCapacity());
    }

    private int GetMaxTopLine(int visibleLineCapacity)
    {
        return Math.Max(0, renderLines.Count - Math.Max(1, visibleLineCapacity));
    }

    private Rectangle GetViewportBounds()
    {
        var rightInset = verticalScrollBar.Visible ? verticalScrollBar.Width : 0;
        return new Rectangle(
            Padding.Left,
            Padding.Top,
            Math.Max(0, ClientSize.Width - Padding.Horizontal - rightInset),
            Math.Max(0, ClientSize.Height - Padding.Vertical));
    }

    private static string NormalizeConsoleText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length + 16);
        foreach (var ch in text)
        {
            if (ch == '\r' || ch == '\n')
            {
                builder.Append(ch);
                continue;
            }

            if (ch == '\t')
            {
                builder.Append("    ");
                continue;
            }

            if (!char.IsControl(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static Font CreateTerminalFont()
    {
        try
        {
            return new Font("Cascadia Code", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            return new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    private readonly record struct ConsoleRenderLine(string Text, Color Color);
}
