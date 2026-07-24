using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ApexTweaker.UI.Wpf.Controls;

public enum RiskLevel
{
    Safe,
    Advanced,
    Restart,
    Dangerous
}

/// <summary>
/// Typed risk badge (glyph + text, not color-only) reused across Modules/Performance rows.
/// </summary>
public sealed class RiskBadge : Border
{
    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
        nameof(Level),
        typeof(RiskLevel),
        typeof(RiskBadge),
        new PropertyMetadata(RiskLevel.Safe, OnLevelChanged));

    private readonly TextBlock glyphText = new() { FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock labelText = new()
    {
        FontSize = 10,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(4, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center
    };

    public RiskLevel Level
    {
        get => (RiskLevel)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public RiskBadge()
    {
        CornerRadius = new CornerRadius(6);
        Padding = new Thickness(8, 3, 8, 3);
        BorderThickness = new Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { glyphText, labelText } };
        Refresh();
    }

    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RiskBadge)d).Refresh();

    private void Refresh()
    {
        var (glyph, label, surfaceKey, borderKey) = Level switch
        {
            RiskLevel.Dangerous => ("!", "PERIGO", "ErrorSurfaceBrush", "ErrorBrush"),
            RiskLevel.Advanced => ("!", "AVANCADO", "WarningSurfaceBrush", "WarningBrush"),
            RiskLevel.Restart => ("R", "REINICIO", "InfoSurfaceBrush", "AccentBrush"),
            _ => ("OK", "SEGURO", "SuccessSurfaceBrush", "SuccessBrush")
        };

        glyphText.Text = glyph;
        labelText.Text = label;
        SetResourceReference(BackgroundProperty, surfaceKey);
        SetResourceReference(BorderBrushProperty, borderKey);
        glyphText.SetResourceReference(TextBlock.ForegroundProperty, borderKey);
        labelText.SetResourceReference(TextBlock.ForegroundProperty, borderKey);
    }
}
