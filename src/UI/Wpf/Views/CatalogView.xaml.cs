using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ApexTweaker.Models;
using ApexTweaker.UI.Wpf.Controls;
using ApexTweaker.UI.Wpf.ViewModels;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace ApexTweaker.UI.Wpf.Views;

public partial class CatalogView : WpfUserControl
{
    private readonly CatalogViewModel viewModel = new();
    private bool initialAnalyzeDone;

    /// <summary>Shell should navigate to Dashboard only — does not start Auto-Optimize.</summary>
    public event Action? GoToAutoOptimizeNavigationRequested;

    /// <summary>Explicit snackbar kind for shell (no substring classification).</summary>
    public event Action<string, SnackbarKind>? FeedbackStatusRequested;

    public CatalogView()
    {
        InitializeComponent();
        DataContext = viewModel;
        PresetCombo.ItemsSource = viewModel.Presets;
        PresetCombo.SelectedItem = viewModel.SelectedPreset;
        viewModel.LoadBios();
        BiosList.ItemsSource = viewModel.BiosItems;
        RulesList.ItemsSource = viewModel.Rows;
        // Analyze on Loaded so MainWindow can subscribe FeedbackStatusRequested first.
        Loaded += CatalogView_OnLoaded;
    }

    private async void CatalogView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (initialAnalyzeDone)
        {
            return;
        }

        initialAnalyzeDone = true;
        await RunAnalyzeAndRefreshUiAsync().ConfigureAwait(true);
    }

    private async void AnalyzeButton_OnClick(object sender, RoutedEventArgs e) =>
        await RunAnalyzeAndRefreshUiAsync().ConfigureAwait(true);

    private async void RetryAnalyze_OnClick(object sender, RoutedEventArgs e)
    {
        RetryEmptyButton.Focus();
        await RunAnalyzeAndRefreshUiAsync().ConfigureAwait(true);
    }

    private void GoToAuto_OnClick(object sender, RoutedEventArgs e)
    {
        FeedbackStatusRequested?.Invoke(
            "Abrindo Dashboard. Use Auto-Optimize la quando quiser aplicar — nada foi iniciado ainda.",
            SnackbarKind.Info);
        GoToAutoOptimizeNavigationRequested?.Invoke();
    }

    private void PresetCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetCombo.SelectedItem is WindowsOptimizationPreset preset)
        {
            viewModel.SelectedPreset = preset;
        }
    }

    private async Task RunAnalyzeAndRefreshUiAsync()
    {
        if (PresetCombo.SelectedItem is WindowsOptimizationPreset preset)
        {
            viewModel.SelectedPreset = preset;
        }

        await viewModel.AnalyzeAsync().ConfigureAwait(true);
        StatusText.Text = viewModel.StatusText;
        ApplyFeedbackVisibility();

        if (viewModel.FeedbackKind == CatalogFeedbackKind.Error)
        {
            FeedbackStatusRequested?.Invoke(viewModel.StatusText, SnackbarKind.Error);
            RetryErrorButton.Focus();
        }
    }

    private void ApplyFeedbackVisibility()
    {
        EmptyPanel.Visibility = viewModel.ShowEmptyPanel ? Visibility.Visible : Visibility.Collapsed;
        ErrorPanel.Visibility = viewModel.ShowErrorPanel ? Visibility.Visible : Visibility.Collapsed;
        PartialPanel.Visibility = viewModel.ShowPartialPanel ? Visibility.Visible : Visibility.Collapsed;
        RulesList.Visibility = viewModel.ShowRulesList ? Visibility.Visible : Visibility.Collapsed;
        RulesHeader.Visibility = viewModel.ShowRulesList ? Visibility.Visible : Visibility.Collapsed;
        GoToAutoButton.Visibility = viewModel.ShowGoToAutoCta ? Visibility.Visible : Visibility.Collapsed;
    }
}
