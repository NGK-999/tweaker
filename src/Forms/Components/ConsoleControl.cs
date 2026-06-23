using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Renomeador.Forms.Components;

internal sealed class ConsoleControl : UserControl
{
    private static readonly Color ConsoleBackColor = Color.FromArgb(20, 20, 22);
    private static readonly Color ConsoleTextColor = Color.FromArgb(224, 224, 224);
    private const int WmSetRedraw = 0x000B;

    private readonly Panel consoleHost;
    private readonly RichTextBox terminalBox;

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

        terminalBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            ReadOnly = true,
            WordWrap = false,
            Multiline = true,
            ScrollBars = RichTextBoxScrollBars.Both,
            BackColor = ConsoleBackColor,
            ForeColor = ConsoleTextColor,
            Font = CreateTerminalFont(),
            BorderStyle = BorderStyle.None,
            ShortcutsEnabled = true,
            DetectUrls = false,
            HideSelection = false
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

        BeginUpdate();
        try
        {
            terminalBox.Clear();

            foreach (var line in lines)
            {
                var normalizedText = NormalizeConsoleText(line.Text);
                if (normalizedText.Length == 0)
                {
                    continue;
                }

                terminalBox.SelectionStart = terminalBox.TextLength;
                terminalBox.SelectionLength = 0;
                terminalBox.SelectionColor = line.Color.IsEmpty ? ConsoleTextColor : line.Color;
                terminalBox.AppendText(normalizedText);
            }

            terminalBox.SelectionColor = ConsoleTextColor;
            terminalBox.SelectionStart = terminalBox.TextLength;
            terminalBox.SelectionLength = 0;
            terminalBox.ScrollToCaret();
        }
        finally
        {
            EndUpdate();
        }
    }

    public void ClearEntries()
    {
        if (IsDisposed || !terminalBox.IsHandleCreated)
        {
            return;
        }

        BeginUpdate();
        try
        {
            terminalBox.Clear();
        }
        finally
        {
            EndUpdate();
        }
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

    private void BeginUpdate()
    {
        if (!terminalBox.IsHandleCreated)
        {
            return;
        }

        terminalBox.SuspendLayout();
        _ = SendMessage(terminalBox.Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
    }

    private void EndUpdate()
    {
        if (!terminalBox.IsHandleCreated)
        {
            return;
        }

        _ = SendMessage(terminalBox.Handle, WmSetRedraw, new IntPtr(1), IntPtr.Zero);
        terminalBox.ResumeLayout();
        terminalBox.Invalidate();
        terminalBox.Update();
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

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
