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
    public event Func<Task>? CleanTempRequested;
    public event Func<Task>? TrimSsdRequested;
    public event Func<Task>? RepairSystemRequested;
    public event Func<Task>? StorageSenseOffRequested;

    public UtilitiesView()
    {
        InitializeComponent();
    }

    public void SetBusy(bool busy)
    {
        var enabled = !busy;
        RevertButton.IsEnabled = enabled;
        UninstallButton.IsEnabled = enabled;
        AboutButton.IsEnabled = enabled;
        RiotSupportButton.IsEnabled = enabled;
        CleanTempButton.IsEnabled = enabled;
        TrimButton.IsEnabled = enabled;
        RepairButton.IsEnabled = enabled;
        StorageSenseButton.IsEnabled = enabled;
    }

    private async void CleanTempButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CleanTempRequested is not null)
        {
            await CleanTempRequested.Invoke();
        }
    }

    private async void TrimButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TrimSsdRequested is not null)
        {
            await TrimSsdRequested.Invoke();
        }
    }

    private async void RepairButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RepairSystemRequested is not null)
        {
            await RepairSystemRequested.Invoke();
        }
    }

    private async void StorageSenseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (StorageSenseOffRequested is not null)
        {
            await StorageSenseOffRequested.Invoke();
        }
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

    private void AboutButton_OnClick(object sender, RoutedEventArgs e) => AboutRequested?.Invoke();

    private void RiotSupportButton_OnClick(object sender, RoutedEventArgs e) => RiotSupportRequested?.Invoke();
}
