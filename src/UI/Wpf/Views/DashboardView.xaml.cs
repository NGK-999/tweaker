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
        AutoOptimizeButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 60));
        AutoOptimizeButton.IsEnabled = false;
    }

    public void SetAutoOptimizeIdle(bool alreadyOptimized)
    {
        AutoOptimizeButton.Content = alreadyOptimized
            ? "\u2713 SISTEMA J\u00C1 OTIMIZADO AO M\u00C1XIMO"
            : "\u26A1 OTIMIZAR SISTEMA AO M\u00C1XIMO";

        AutoOptimizeButton.Background = new SolidColorBrush(
            alreadyOptimized
                ? System.Windows.Media.Color.FromRgb(18, 96, 110)
                : System.Windows.Media.Color.FromRgb(0, 180, 216));
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