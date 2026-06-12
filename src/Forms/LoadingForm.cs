using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Renomeador.Forms;

internal sealed class LoadingForm : Form
{
    private static readonly Color Bg = Color.FromArgb(10, 15, 24);
    private static readonly Color Panel = Color.FromArgb(17, 24, 39);
    private static readonly Color Border = Color.FromArgb(45, 58, 80);
    private static readonly Color TextMain = Color.FromArgb(243, 244, 246);
    private static readonly Color TextMuted = Color.FromArgb(156, 163, 175);
    private static readonly Color ProgressBack = Color.FromArgb(20, 25, 35);
    private static readonly Color Accent = Color.FromArgb(34, 211, 238);

    private readonly Panel customProgressBar;
    private readonly System.Windows.Forms.Timer marqueeTimer = new() { Interval = 16 };
    private int _marqueeX = -50;
    private readonly int _marqueeWidth = 100;
    private bool marqueeTimerDisposed;

    public LoadingForm()
    {
        Text = AppInfo.Name;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 150);
        BackColor = Bg;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

        var shell = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            Padding = new Padding(22)
        };
        shell.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawRectangle(pen, 0, 0, shell.Width - 1, shell.Height - 1);
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 4F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        var title = new Label
        {
            Text = "Inicializando motor Apex...",
            Dock = DockStyle.Fill,
            ForeColor = TextMain,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.BottomCenter
        };

        customProgressBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ProgressBack,
            Margin = new Padding(0)
        };
        typeof(Panel).InvokeMember(
            "DoubleBuffered",
            BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            target: customProgressBar,
            args: [true]);
        customProgressBar.Paint += CustomProgressBar_Paint;

        var subtitle = new Label
        {
            Text = "Preparando interface e servicos internos",
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.TopCenter,
            Padding = new Padding(0, 8, 0, 0)
        };

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(customProgressBar, 0, 1);
        layout.Controls.Add(subtitle, 0, 2);
        shell.Controls.Add(layout);
        Controls.Add(shell);

        marqueeTimer.Tick += MarqueeTimer_Tick;
        Load += LoadingForm_Load;
        FormClosing += (_, _) => StopAndDisposeMarqueeTimer();
    }

    private async void LoadingForm_Load(object? sender, EventArgs e)
    {
        marqueeTimer.Start();
        await Task.Delay(2500);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void MarqueeTimer_Tick(object? sender, EventArgs e)
    {
        _marqueeX += 4;
        if (_marqueeX > customProgressBar.Width)
        {
            _marqueeX = -_marqueeWidth;
        }

        customProgressBar.Invalidate();
    }

    private void CustomProgressBar_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Accent);
        e.Graphics.FillRectangle(brush, _marqueeX, 0, _marqueeWidth, customProgressBar.Height);
    }

    private void StopAndDisposeMarqueeTimer()
    {
        if (marqueeTimerDisposed)
        {
            return;
        }

        marqueeTimer.Stop();
        marqueeTimer.Dispose();
        marqueeTimerDisposed = true;
    }
}
