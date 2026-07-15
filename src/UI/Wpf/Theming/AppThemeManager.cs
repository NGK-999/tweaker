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
            ["WindowBgColor"] = "#1C1C1E",
            ["SidebarBgColor"] = "#252528",
            ["ContentBgColor"] = "#1C1C1E",
            ["CardBgColor"] = "#2C2C2E",
            ["CardElevatedColor"] = "#3A3A3C",
            ["CardBorderColor"] = "#48484A",
            ["AccentColor"] = "#0A84FF",
            ["AccentPressedColor"] = "#007BEA",
            ["AccentHoverColor"] = "#1A94FF",
            ["TextPrimaryColor"] = "#F5F5F7",
            ["TextSecondaryColor"] = "#A7A7AD",
            ["TextTertiaryColor"] = "#94949B",
            ["SeparatorColor"] = "#38383A",
            ["DestructiveColor"] = "#FF5A52",
            ["DestructiveHoverColor"] = "#FF756B",
            ["SuccessColor"] = "#30D158",
            ["WarningColor"] = "#F5B942",
            ["ErrorColor"] = "#FF756B",
            ["ChartBgColor"] = "#1A1A1C",
            ["NavHoverColor"] = "#3A3A3C",
            ["NavActiveColor"] = "#48484A",
            ["SecondaryHoverColor"] = "#454547",
            ["ControlBgColor"] = "#202024",
            ["ControlDisabledColor"] = "#323238",
            ["SelectionColor"] = "#184A73",
            ["FocusColor"] = "#65B1FF",
            ["InfoSurfaceColor"] = "#13283D",
            ["SuccessSurfaceColor"] = "#153126",
            ["WarningSurfaceColor"] = "#302817",
            ["ErrorSurfaceColor"] = "#351D22",
            ["PanelBgColor"] = "#101B2B",
            ["PanelBorderColor"] = "#2B3D58",
            ["OnAccentTextColor"] = "#08111E"
        };

    private static readonly IReadOnlyDictionary<string, string> LightPalette =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["WindowBgColor"] = "#F5F7FA",
            ["SidebarBgColor"] = "#E9EEF5",
            ["ContentBgColor"] = "#F5F7FA",
            ["CardBgColor"] = "#FFFFFF",
            ["CardElevatedColor"] = "#EEF3F8",
            ["CardBorderColor"] = "#CBD5E1",
            ["AccentColor"] = "#0067C0",
            ["AccentPressedColor"] = "#004F94",
            ["AccentHoverColor"] = "#005FAF",
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
            ["NavHoverColor"] = "#DDE6F0",
            ["NavActiveColor"] = "#C9DCF2",
            ["SecondaryHoverColor"] = "#E0E8F1",
            ["ControlBgColor"] = "#FFFFFF",
            ["ControlDisabledColor"] = "#E7EBF0",
            ["SelectionColor"] = "#D4E9FF",
            ["FocusColor"] = "#0067C0",
            ["InfoSurfaceColor"] = "#E6F2FF",
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
