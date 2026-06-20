using System;
using System.Drawing;
using System.Windows.Forms;

namespace Renomeador.Forms.Components;

internal sealed class DashboardPage : UserControl
{
    private readonly HeroBannerCard heroBannerCard;
    private readonly SurfaceSectionCard autoTuningCard;
    private readonly SurfaceSectionCard recoveryCard;
    private readonly Control autoOptimizeButton;
    private readonly Control restorePointButton;
    private readonly Label summaryLabel;

    public DashboardPage(Control autoOptimizeButton, Control restorePointButton)
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.Transparent;
        Padding = new Padding(0);

        this.autoOptimizeButton = autoOptimizeButton;
        this.restorePointButton = restorePointButton;

        heroBannerCard = new HeroBannerCard
        {
            Dock = DockStyle.None,
            TitleText = "Ajuste o sistema para consistência, não para placebo",
            SubtitleText = "ApexTweaker centraliza auto-tuning, rollback e telemetria para atacar 1% low, stutter e ruído de background com trilha reversível."
        };

        autoTuningCard = new SurfaceSectionCard
        {
            TitleText = "Auto-Tuning",
            Dock = DockStyle.None
        };

        recoveryCard = new SurfaceSectionCard
        {
            TitleText = "Recuperação e Estado",
            Dock = DockStyle.None
        };

        summaryLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 56,
            ForeColor = Color.FromArgb(205, 205, 205),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            Text = "Aplique o auto-tuning para revisar energia, scheduler, GPU e rede. Use telemetria para validar impacto real antes de manter tweaks agressivos.",
            TextAlign = ContentAlignment.TopLeft
        };

        ConfigureActionButton(this.autoOptimizeButton, true);
        ConfigureActionButton(this.restorePointButton, false);

        autoTuningCard.ContentHost.Controls.Add(this.autoOptimizeButton);
        recoveryCard.ContentHost.Controls.Add(summaryLabel);
        recoveryCard.ContentHost.Controls.Add(this.restorePointButton);

        Controls.Add(heroBannerCard);
        Controls.Add(autoTuningCard);
        Controls.Add(recoveryCard);
        Resize += (_, _) => LayoutCards();
        LayoutCards();
    }

    private void LayoutCards()
    {
        var sidePadding = 8;
        var gap = 16;
        var heroHeight = 188;
        var lowerTop = heroHeight + gap;
        var lowerHeight = Math.Max(180, Height - lowerTop);
        var availableWidth = Math.Max(0, Width - (sidePadding * 2) - gap);
        var cardWidth = availableWidth / 2;

        heroBannerCard.Bounds = new Rectangle(sidePadding, 0, Math.Max(0, Width - (sidePadding * 2)), heroHeight);
        autoTuningCard.Bounds = new Rectangle(sidePadding, lowerTop, cardWidth, lowerHeight);
        recoveryCard.Bounds = new Rectangle(sidePadding + cardWidth + gap, lowerTop, cardWidth, lowerHeight);

        autoOptimizeButton.Location = new Point(0, 8);
        autoOptimizeButton.Size = new Size(Math.Max(180, autoTuningCard.ContentHost.Width), 48);

        summaryLabel.Width = recoveryCard.ContentHost.Width;
        summaryLabel.Location = new Point(0, 0);

        restorePointButton.Location = new Point(0, 72);
        restorePointButton.Size = new Size(Math.Max(180, recoveryCard.ContentHost.Width), 38);
    }

    private static void ConfigureActionButton(Control control, bool primary)
    {
        control.Dock = DockStyle.None;
        control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        control.Margin = new Padding(0);

        if (control is Button button)
        {
            button.TextAlign = ContentAlignment.MiddleCenter;
        }
    }
}
