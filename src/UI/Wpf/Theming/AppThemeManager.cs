using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ApexTweaker.UI.Wpf.Theming;

public enum AppThemeMode
{
    Dark,
    Light
}

public static class AppThemeManager
{
    private static readonly IReadOnlyDictionary<string, string> DarkPalette =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["WindowBgColor"] = "#17181B",
            ["SidebarBgColor"] = "#141517",
            ["ContentBgColor"] = "#1B1D21",
            ["CardBgColor"] = "#212328",
            ["CardElevatedColor"] = "#2A2D33",
            ["CardBorderColor"] = "#26282E",
            ["AccentColor"] = "#4B9E9E",
            ["AccentPressedColor"] = "#3A8484",
            ["AccentHoverColor"] = "#5CB0B0",
            ["TextPrimaryColor"] = "#EDEEF0",
            ["TextSecondaryColor"] = "#9AA0AC",
            ["TextTertiaryColor"] = "#6C7280",
            ["SeparatorColor"] = "#2A2D33",
            ["DestructiveColor"] = "#E5534B",
            ["DestructiveHoverColor"] = "#EF6B63",
            ["SuccessColor"] = "#4CAF7D",
            ["WarningColor"] = "#D8A657",
            ["ErrorColor"] = "#EF6B63",
            ["ChartBgColor"] = "#17181B",
            ["NavHoverColor"] = "#1E2024",
            ["NavActiveColor"] = "#1A2224",
            ["SecondaryHoverColor"] = "#31343B",
            ["ControlBgColor"] = "#1B1D21",
            ["ControlDisabledColor"] = "#26282E",
            ["SelectionColor"] = "#1F3B3B",
            ["FocusColor"] = "#5CB0B0",
            ["InfoSurfaceColor"] = "#182A2A",
            ["SuccessSurfaceColor"] = "#16261E",
            ["WarningSurfaceColor"] = "#2A2317",
            ["ErrorSurfaceColor"] = "#2C1D1B",
            ["PanelBgColor"] = "#1B1D21",
            ["PanelBorderColor"] = "#26282E",
            ["OnAccentTextColor"] = "#0B1414"
        };

    private static readonly IReadOnlyDictionary<string, string> LightPalette =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["WindowBgColor"] = "#F4F5F7",
            ["SidebarBgColor"] = "#E7E9ED",
            ["ContentBgColor"] = "#FAFBFC",
            ["CardBgColor"] = "#FFFFFF",
            ["CardElevatedColor"] = "#EEF1F4",
            ["CardBorderColor"] = "#D6DAE0",
            ["AccentColor"] = "#2F7A78",
            ["AccentPressedColor"] = "#245F5D",
            ["AccentHoverColor"] = "#388F8C",
            ["TextPrimaryColor"] = "#172033",
            ["TextSecondaryColor"] = "#445064",
            ["TextTertiaryColor"] = "#657286",
            ["SeparatorColor"] = "#D7DEE8",
            ["DestructiveColor"] = "#C9362B",
            ["DestructiveHoverColor"] = "#A42C24",
            ["SuccessColor"] = "#147D4B",
            ["WarningColor"] = "#A65F00",
            ["ErrorColor"] = "#C9362B",
            ["ChartBgColor"] = "#EAF0F6",
            ["NavHoverColor"] = "#DEE2E7",
            ["NavActiveColor"] = "#D7E4E3",
            ["SecondaryHoverColor"] = "#E0E8F1",
            ["ControlBgColor"] = "#FFFFFF",
            ["ControlDisabledColor"] = "#E7EBF0",
            ["SelectionColor"] = "#D6EAE9",
            ["FocusColor"] = "#2F7A78",
            ["InfoSurfaceColor"] = "#E3F0EF",
            ["SuccessSurfaceColor"] = "#E7F6EE",
            ["WarningSurfaceColor"] = "#FFF4DC",
            ["ErrorSurfaceColor"] = "#FDECEB",
            ["PanelBgColor"] = "#F1F5F9",
            ["PanelBorderColor"] = "#D5DEE8",
            ["OnAccentTextColor"] = "#FFFFFF"
        };

    private static readonly IReadOnlyDictionary<string, string> BrushColorKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["WindowBgBrush"] = "WindowBgColor",
            ["SidebarBgBrush"] = "SidebarBgColor",
            ["ContentBgBrush"] = "ContentBgColor",
            ["CardBgBrush"] = "CardBgColor",
            ["CardElevatedBrush"] = "CardElevatedColor",
            ["CardBorderBrush"] = "CardBorderColor",
            ["AccentBrush"] = "AccentColor",
            ["AccentPressedBrush"] = "AccentPressedColor",
            ["AccentHoverBrush"] = "AccentHoverColor",
            ["TextPrimaryBrush"] = "TextPrimaryColor",
            ["TextSecondaryBrush"] = "TextSecondaryColor",
            ["TextTertiaryBrush"] = "TextTertiaryColor",
            ["SeparatorBrush"] = "SeparatorColor",
            ["DestructiveBrush"] = "DestructiveColor",
            ["DestructiveHoverBrush"] = "DestructiveHoverColor",
            ["SuccessBrush"] = "SuccessColor",
            ["WarningBrush"] = "WarningColor",
            ["ErrorBrush"] = "ErrorColor",
            ["ChartBgBrush"] = "ChartBgColor",
            ["NavHoverBrush"] = "NavHoverColor",
            ["NavActiveBrush"] = "NavActiveColor",
            ["SecondaryHoverBrush"] = "SecondaryHoverColor",
            ["ControlBgBrush"] = "ControlBgColor",
            ["ControlDisabledBrush"] = "ControlDisabledColor",
            ["SelectionBrush"] = "SelectionColor",
            ["FocusBrush"] = "FocusColor",
            ["InfoSurfaceBrush"] = "InfoSurfaceColor",
            ["SuccessSurfaceBrush"] = "SuccessSurfaceColor",
            ["WarningSurfaceBrush"] = "WarningSurfaceColor",
            ["ErrorSurfaceBrush"] = "ErrorSurfaceColor",
            ["PanelBgBrush"] = "PanelBgColor",
            ["PanelBorderBrush"] = "PanelBorderColor",
            ["OnAccentTextBrush"] = "OnAccentTextColor"
        };

    public static AppThemeMode Current { get; private set; } = AppThemeMode.Dark;

    public static void Toggle(FrameworkElement root) =>
        Apply(root, Current == AppThemeMode.Dark ? AppThemeMode.Light : AppThemeMode.Dark);

    public static void Apply(FrameworkElement root, AppThemeMode theme)
    {
        ArgumentNullException.ThrowIfNull(root);
        Current = theme;
        var palette = theme == AppThemeMode.Dark ? DarkPalette : LightPalette;
        var visitedResources = new HashSet<ResourceDictionary>(ReferenceEqualityComparer.Instance);
        ApplyElement(root, palette, visitedResources);
    }

    public static string GetHex(string colorKey, AppThemeMode theme)
    {
        var palette = theme == AppThemeMode.Dark ? DarkPalette : LightPalette;
        return palette[colorKey];
    }

    private static void ApplyElement(
        DependencyObject element,
        IReadOnlyDictionary<string, string> palette,
        ISet<ResourceDictionary> visitedResources)
    {
        if (element is FrameworkElement frameworkElement)
        {
            ApplyResources(frameworkElement.Resources, palette, visitedResources);
        }

        var children = VisualTreeHelper.GetChildrenCount(element);
        for (var index = 0; index < children; index++)
        {
            ApplyElement(VisualTreeHelper.GetChild(element, index), palette, visitedResources);
        }
    }

    private static void ApplyResources(
        ResourceDictionary resources,
        IReadOnlyDictionary<string, string> palette,
        ISet<ResourceDictionary> visitedResources)
    {
        if (!visitedResources.Add(resources))
        {
            return;
        }

        foreach (var merged in resources.MergedDictionaries)
        {
            ApplyResources(merged, palette, visitedResources);
        }

        foreach (var (key, value) in palette)
        {
            if (resources.Contains(key))
            {
                resources[key] = ParseColor(value);
            }
        }

        foreach (var (brushKey, colorKey) in BrushColorKeys)
        {
            if (!resources.Contains(brushKey))
            {
                continue;
            }

            var color = ParseColor(palette[colorKey]);
            if (resources[brushKey] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = color;
                continue;
            }

            resources[brushKey] = new SolidColorBrush(color);
        }
    }

    private static Color ParseColor(string value) =>
        (Color)ColorConverter.ConvertFromString(value)!;
}
