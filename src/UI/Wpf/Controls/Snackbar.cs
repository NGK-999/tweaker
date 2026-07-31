using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ApexTweaker.UI.Wpf.Animations;

namespace ApexTweaker.UI.Wpf.Controls;

public enum SnackbarKind
{
    Info,
    Success,
    Warning,
    Error
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
    private Storyboard? activeStoryboard;
    private int showGeneration;

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

        var generation = ++showGeneration;
        CancelActivePresentation();

        messageText.Text = message;
        ApplyKindSurface(kind);

        var fadeIn = new Storyboard { FillBehavior = FillBehavior.HoldEnd };
        UiMotion.ConfigureStoryboard(fadeIn);
        fadeIn.Children.Add(UiMotion.CreateDoubleAnimation(
            this,
            OpacityProperty,
            1D,
            TimeSpan.FromMilliseconds(180),
            UiMotion.EaseOut,
            from: Opacity));
        activeStoryboard = fadeIn;
        fadeIn.Begin();

        hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        hideTimer.Tick += (_, _) =>
        {
            if (generation != showGeneration)
            {
                return;
            }

            hideTimer?.Stop();
            hideTimer = null;

            var fadeOut = new Storyboard { FillBehavior = FillBehavior.HoldEnd };
            UiMotion.ConfigureStoryboard(fadeOut);
            fadeOut.Children.Add(UiMotion.CreateDoubleAnimation(
                this,
                OpacityProperty,
                0D,
                TimeSpan.FromMilliseconds(220),
                UiMotion.EaseIn,
                from: Opacity));
            activeStoryboard = fadeOut;
            fadeOut.Begin();
        };
        hideTimer.Start();
    }

    private void CancelActivePresentation()
    {
        hideTimer?.Stop();
        hideTimer = null;

        if (activeStoryboard is not null)
        {
            activeStoryboard.Stop();
            activeStoryboard.Remove(this);
            activeStoryboard = null;
        }

        BeginAnimation(OpacityProperty, null);
    }

    private void ApplyKindSurface(SnackbarKind kind)
    {
        var (backgroundKey, borderKey) = kind switch
        {
            SnackbarKind.Success => ("SuccessSurfaceBrush", "SuccessBrush"),
            SnackbarKind.Warning => ("WarningSurfaceBrush", "WarningBrush"),
            SnackbarKind.Error => ("ErrorSurfaceBrush", "ErrorBrush"),
            _ => ("InfoSurfaceBrush", "AccentBrush")
        };

        SetResourceReference(BackgroundProperty, backgroundKey);
        SetResourceReference(BorderBrushProperty, borderKey);
    }
}
