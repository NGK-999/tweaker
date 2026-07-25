using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfUserControl = System.Windows.Controls.UserControl;
using System.Windows.Media;

namespace ApexTweaker.UI.Wpf.Views;

public partial class DashboardView : WpfUserControl
{
    public event Func<Task>? AutoOptimizeRequested;

    public event Func<Task>? CreateRestorePointRequested;

    public DashboardView()
    {
        InitializeComponent();
    }

    public void SetSummary(string text)
    {
        SummaryText.Text = text;
    }

    public void SetBusy(bool busy)
    {
        AutoOptimizeButton.IsEnabled = !busy;
        RestorePointButton.IsEnabled = !busy;
    }

    public void SetAutoOptimizeBusy()
    {
        AutoOptimizeButton.Content = "Aplicando otimiza\u00E7\u00F5es...";
        AutoOptimizeButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 72, 74));
        AutoOptimizeButton.IsEnabled = false;
    }

    public void SetAutoOptimizeIdle(bool alreadyOptimized, bool needsUpgrade = false)
    {
        if (needsUpgrade)
        {
            AutoOptimizeButton.Content = "Atualizar otimizacoes (gaps)";
            AutoOptimizeButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
        }
        else if (alreadyOptimized)
        {
            AutoOptimizeButton.Content = "Sistema otimizado — reaplicar?";
            AutoOptimizeButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 74, 110));
        }
        else
        {
            AutoOptimizeButton.Content = "Otimizar sistema";
            AutoOptimizeButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 132, 255));
        }

        AutoOptimizeButton.IsEnabled = true;
    }

    private async void AutoOptimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (AutoOptimizeRequested is not null)
        {
            await AutoOptimizeRequested.Invoke();
        }
    }

    private async void RestorePointButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CreateRestorePointRequested is not null)
        {
            await CreateRestorePointRequested.Invoke();
        }
    }
}