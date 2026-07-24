using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace ApexTweaker.UI.Wpf.Views;

public partial class ModulesView : WpfUserControl
{
    public event Func<string, Task>? ModuleRequested;

    public ModulesView()
    {
        InitializeComponent();
    }

    public void SetBusy(bool busy)
    {
        foreach (var button in EnumerateModuleButtons())
        {
            button.IsEnabled = !busy;
        }
    }

    private async void ModuleButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton button ||
            button.Tag is not string moduleKey ||
            ModuleRequested is null)
        {
            return;
        }

        await ModuleRequested.Invoke(moduleKey);
    }

    private WpfButton[] EnumerateModuleButtons()
    {
        return FindButtons(CoreButtonsPanel)
            .Concat(FindButtons(PeripheralButtonsPanel))
            .Concat(FindButtons(GpuButtonsPanel))
            .Concat(FindButtons(GamesButtonsPanel))
            .Concat(FindButtons(MarketButtonsPanel))
            .ToArray();
    }

    private static IEnumerable<WpfButton> FindButtons(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is WpfButton button)
            {
                yield return button;
            }

            foreach (var nested in FindButtons(child))
            {
                yield return nested;
            }
        }
    }
}

