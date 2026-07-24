using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ApexTweaker.UI.Wpf.Controls;

public enum SnackbarKind
{
    Info,
    Success,
    Warning
}

/// <summary>
/// Reusable corporate toast: "Aplicado.", "Ja aplicado (SKIP).", "Reinicio necessario." etc.
/// One instance lives in the shell (MainWindow) and every page routes status through it.
/// </summary>
public sealed class Snackbar : Border
{
    private readonly TextBlock messageText = new()
    {
        FontSize = 13,
        FontWeight = FontWeights.SemiBold,
        TextWrapping = TextWrapping.Wrap
    };

    private DispatcherTimer? hideTimer;

    public Snackbar()
    {
        CornerRadius = new CornerRadius(10);
        Padding = new Thickness(16, 12, 16, 12);
        BorderThickness = new Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(0, 0, 0, 24);
        MaxWidth = 460;
        Opacity = 0;
        IsHitTestVisible = false;
        SetResourceReference(BackgroundProperty, "CardElevatedBrush");
        SetResourceReference(BorderBrushProperty, "CardBorderBrush");
        messageText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        Child = messageText;
    }

    public void Show(string message, SnackbarKind kind = SnackbarKind.Info, double seconds = 3.2)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        messageText.Text = message;
        var borderKey = kind switch
        {
            SnackbarKind.Success => "SuccessBrush",
            SnackbarKind.Warning => "WarningBrush",
            _ => "AccentBrush"
        };
        SetResourceReference(BorderBrushProperty, borderKey);

        hideTimer?.Stop();
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(180)));

        hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        hideTimer.Tick += (_, _) =>
        {
            hideTimer?.Stop();
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(220)));
        };
        hideTimer.Start();
    }
}
