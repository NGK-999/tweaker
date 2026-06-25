using System.Drawing;
using System.Windows.Forms;
using Renomeador.Forms;

namespace Renomeador.Forms.Components;

internal sealed class TelemetryPage : UserControl
{
    private static readonly Color TextMain = Color.FromArgb(255, 255, 255);
    private static readonly Color Accent = Color.FromArgb(0, 180, 216);
    private static readonly Color GlassCardFill = ColorTranslator.FromHtml("#2A2A2A");
    private static readonly Color GlassCardBorder = ColorTranslator.FromHtml("#3A3A3C");

    private readonly Control abTestButton;
    private readonly Control performanceChart;
    private readonly Control consoleFrame;

    private readonly GamerCard abTestCard;
    private readonly GamerCard chartCard;
    private readonly GamerCard consoleCard;

    private readonly Panel abTestHost;
    private readonly Panel chartHost;
    private readonly Panel consoleHost;

    public TelemetryPage(Control abTestButton, Control performanceChart, Control consoleFrame)
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Padding = new Padding(0);

        this.abTestButton = abTestButton;
        this.performanceChart = performanceChart;
        this.consoleFrame = consoleFrame;

        abTestHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        chartHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0) };
        consoleHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0) };

        abTestCard = CreateCard("Teste A/B de Estabilidade", abTestHost);
        chartCard = CreateCard("Gráfico em tempo real", chartHost);
        consoleCard = CreateCard("Console", consoleHost);

        Controls.Add(abTestCard);
        Controls.Add(chartCard);
        Controls.Add(consoleCard);

        EnsureHostedControls();
        LayoutCards();
    }

    public void RestoreVisualState()
    {
        EnsureHostedControls();

        abTestCard.Visible = true;
        chartCard.Visible = true;
        consoleCard.Visible = true;
        abTestButton.Visible = true;
        performanceChart.Visible = true;
        consoleFrame.Visible = true;

        abTestCard.BringToFront();
        chartCard.BringToFront();
        consoleCard.BringToFront();

        PerformLayout();
        LayoutCards();
        Invalidate();
        Update();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        LayoutCards();
    }

    private void EnsureHostedControls()
    {
        if (!ReferenceEquals(abTestButton.Parent, abTestHost))
        {
            abTestButton.Parent?.Controls.Remove(abTestButton);
            abTestHost.Controls.Add(abTestButton);
        }

        if (!ReferenceEquals(performanceChart.Parent, chartHost))
        {
            performanceChart.Parent?.Controls.Remove(performanceChart);
            chartHost.Controls.Add(performanceChart);
        }

        if (!ReferenceEquals(consoleFrame.Parent, consoleHost))
        {
            consoleFrame.Parent?.Controls.Remove(consoleFrame);
            consoleHost.Controls.Add(consoleFrame);
        }

        abTestButton.Dock = DockStyle.Top;
        performanceChart.Dock = DockStyle.Fill;
        consoleFrame.Dock = DockStyle.Fill;
    }

    private void LayoutCards()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        const int gap = 14;
        const int topCardHeight = 118;
        const int minChartHeight = 220;
        const int minConsoleHeight = 190;

        var width = Width;
        var consoleHeight = Math.Max(minConsoleHeight, (int)(Height * 0.30));
        var chartHeight = Height - topCardHeight - consoleHeight - (gap * 2);
        if (chartHeight < minChartHeight)
        {
            chartHeight = minChartHeight;
            consoleHeight = Math.Max(minConsoleHeight, Height - topCardHeight - chartHeight - (gap * 2));
        }

        var abBounds = new Rectangle(0, 0, width, topCardHeight);
        var chartBounds = new Rectangle(0, topCardHeight + gap, width, Math.Max(0, chartHeight));
        var consoleY = chartBounds.Bottom + gap;
        var consoleBounds = new Rectangle(0, consoleY, width, Math.Max(0, Height - consoleY));

        abTestCard.Bounds = abBounds;
        chartCard.Bounds = chartBounds;
        consoleCard.Bounds = consoleBounds;
    }

    private static GamerCard CreateCard(string title, Control content)
    {
        var card = new GamerCard
        {
            Dock = DockStyle.None,
            FillColor = GlassCardFill,
            BorderColor = GlassCardBorder
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        layout.ColumnStyles.Clear();
        layout.RowStyles.Clear();
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(CreateHeaderLabel(title), 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Label CreateHeaderLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text = text,
            ForeColor = Accent,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
    }
}
