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

    private readonly Panel consoleHost;
    private readonly TextBox terminalBox;
    private readonly StringBuilder textBuffer = new(16 * 1024);

    public ConsoleControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        Dock = DockStyle.Fill;
        Margin = new Padding(0);
        Padding = new Padding(0);
        BackColor = ConsoleBackColor;

        consoleHost = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(12),
            BackColor = ConsoleBackColor
        };

        terminalBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            ReadOnly = true,
            WordWrap = false,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            BackColor = ConsoleBackColor,
            ForeColor = ConsoleTextColor,
            Font = CreateTerminalFont(),
            BorderStyle = BorderStyle.None,
            ShortcutsEnabled = true
        };

        consoleHost.Controls.Add(terminalBox);
        Controls.Add(consoleHost);
    }

    public bool IsSurfaceReady => !IsDisposed && terminalBox.IsHandleCreated;

    public void SetEntries(IReadOnlyList<(string Text, Color Color)> lines)
    {
        if (IsDisposed || !terminalBox.IsHandleCreated)
        {
            return;
        }

        try
        {
            textBuffer.Clear();
            textBuffer.EnsureCapacity(Math.Max(256, lines.Count * 64));

            foreach (var line in lines)
            {
                textBuffer.Append(NormalizeConsoleText(line.Text));
            }

            terminalBox.SuspendLayout();
            terminalBox.Text = textBuffer.ToString();
            terminalBox.SelectionStart = terminalBox.TextLength;
            terminalBox.SelectionLength = 0;
            terminalBox.ScrollToCaret();
        }
        finally
        {
            terminalBox.ResumeLayout();
        }

        RefreshSurface();
    }

    public void ClearEntries()
    {
        if (IsDisposed || !terminalBox.IsHandleCreated)
        {
            return;
        }

        try
        {
            terminalBox.SuspendLayout();
            terminalBox.Clear();
        }
        finally
        {
            terminalBox.ResumeLayout();
        }

        RefreshSurface();
    }

    public void RefreshSurface()
    {
        if (IsDisposed)
        {
            return;
        }

        BackColor = ConsoleBackColor;
        consoleHost.BackColor = ConsoleBackColor;
        terminalBox.BackColor = ConsoleBackColor;

        if (terminalBox.IsHandleCreated)
        {
            terminalBox.Invalidate();
            terminalBox.Update();
        }

        if (consoleHost.IsHandleCreated)
        {
            consoleHost.Invalidate();
            consoleHost.Update();
        }

        if (IsHandleCreated)
        {
            Invalidate();
            Update();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(ConsoleBackColor);
    }

    private static string NormalizeConsoleText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal);
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

}
