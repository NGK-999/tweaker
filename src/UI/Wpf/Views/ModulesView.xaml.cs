using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        return CoreButtonsPanel.Children
            .OfType<WpfButton>()
            .Concat(PeripheralButtonsPanel.Children.OfType<WpfButton>())
            .Concat(GpuButtonsPanel.Children.OfType<WpfButton>())
            .Concat(MarketButtonsPanel.Children.OfType<WpfButton>())
            .ToArray();
    }
}
