using System;
using System.Windows;
using System.Windows.Controls;
using ApexTweaker.Models;
using ApexTweaker.UI.Wpf.ViewModels;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace ApexTweaker.UI.Wpf.Views;

public partial class CatalogView : WpfUserControl
{
    private readonly CatalogViewModel viewModel = new();

    public CatalogView()
    {
        InitializeComponent();
        DataContext = viewModel;
        PresetCombo.ItemsSource = viewModel.Presets;
        PresetCombo.SelectedItem = viewModel.SelectedPreset;
        viewModel.LoadBios();
        BiosList.ItemsSource = viewModel.BiosItems;
        RulesList.ItemsSource = viewModel.Rows;
        viewModel.Analyze();
        StatusText.Text = viewModel.StatusText;
    }

    private void AnalyzeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PresetCombo.SelectedItem is WindowsOptimizationPreset preset)
        {
            viewModel.SelectedPreset = preset;
        }

        viewModel.Analyze();
        StatusText.Text = viewModel.StatusText;
    }

    private void PresetCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetCombo.SelectedItem is WindowsOptimizationPreset preset)
        {
            viewModel.SelectedPreset = preset;
        }
    }
}
