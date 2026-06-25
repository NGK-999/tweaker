using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace ApexTweaker.UI.Wpf.Views;

public partial class UtilitiesView : WpfUserControl
{
    public event Func<Task>? RevertRequested;

    public event Func<Task>? UninstallRequested;

    public event Action? AboutRequested;

    public event Action? RiotSupportRequested;

    public UtilitiesView()
    {
        InitializeComponent();
    }

    public void SetBusy(bool busy)
    {
        RevertButton.IsEnabled = !busy;
        UninstallButton.IsEnabled = !busy;
        AboutButton.IsEnabled = !busy;
        RiotSupportButton.IsEnabled = !busy;
    }

    private async void RevertButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RevertRequested is not null)
        {
            await RevertRequested.Invoke();
        }
    }

    private async void UninstallButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (UninstallRequested is not null)
        {
            await UninstallRequested.Invoke();
        }
    }

    private void AboutButton_OnClick(object sender, RoutedEventArgs e)
    {
        AboutRequested?.Invoke();
    }

    private void RiotSupportButton_OnClick(object sender, RoutedEventArgs e)
    {
        RiotSupportRequested?.Invoke();
    }
}

