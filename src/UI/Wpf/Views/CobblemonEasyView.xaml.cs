using System.Windows;
using ApexTweaker.Minecraft.Models;
using ApexTweaker.UI.Wpf.ViewModels;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace ApexTweaker.UI.Wpf.Views;

public partial class CobblemonEasyView : WpfUserControl
{
    private readonly CobblemonEasyViewModel viewModel = new();

    public CobblemonEasyView()
    {
        InitializeComponent();
        DataContext = viewModel;
        EasyFpsComboBox.ItemsSource = new[] { 24, 30 };
        EasyFpsComboBox.SelectedItem = 24;
        UpdateActions();
    }

    public event Func<string, Task>? DetectRequested;

    public event Func<string, Task>? AnalyzeRequested;

    internal event Func<string, bool, int, Task>? OptimizeRequested;

    public event Func<string, Task>? PrepareServerRequested;

    public event Func<string, Task>? TestRequested;

    internal event Func<string, MinecraftOperationalObservation, Task>? SaveTestRequested;

    public event Func<string, Task>? FixRequested;

    public event Func<string, Task>? RestoreRequested;

    public event Func<string, Task>? ExportRequested;

    public event Action? AdvancedRequested;

    internal CobblemonEasyViewModel ViewModel => viewModel;

    public string SelectedPath => viewModel.SelectedPath;

    public void SetSelectedPath(string path, bool resolved)
    {
        viewModel.ResetForPath(path, resolved);
        UpdateActions();
    }

    internal void SetInstanceStatus(MinecraftEasyInstanceStatus status)
    {
        viewModel.SetInstance(status);
        UpdateActions();
    }

    internal void SetAudit(MinecraftEasyModSummary summary)
    {
        viewModel.SetAudit(summary);
        UpdateActions();
    }

    public void SetOptimizationApplied(string backupId, string javaArguments, bool javaAppliedAutomatically)
    {
        viewModel.SetOptimizationApplied(backupId, javaArguments, javaAppliedAutomatically);
        UpdateActions();
    }

    internal void SetServerReadiness(MinecraftEasyServerReadiness readiness)
    {
        viewModel.SetServerReadiness(readiness);
        UpdateActions();
    }

    public void BeginBenchmark()
    {
        viewModel.BeginBenchmark();
        UpdateActions();
    }

    internal void AddBenchmarkSample(MinecraftBenchmarkSample sample, int sampleCount) =>
        viewModel.AddBenchmarkSample(sample, sampleCount);

    internal void CompleteBenchmark(MinecraftBenchmarkResult? result, bool cancelled)
    {
        viewModel.CompleteBenchmark(result, cancelled);
        UpdateActions();
    }

    internal void SetOperationalResult(OperationalHomologationStatus status)
    {
        viewModel.SetOperationalResult(status);
        UpdateActions();
    }

    internal void SetCorrections(MinecraftEasyCorrectionPlan plan)
    {
        viewModel.SetCorrections(plan);
        UpdateActions();
    }

    public void SetRestored(string backupId)
    {
        viewModel.SetRestored(backupId);
        UpdateActions();
    }

    internal void SetDiagnostic(MinecraftDiagnosticPackageResult package) => viewModel.SetDiagnostic(package);

    public void SetBusy(bool value)
    {
        viewModel.SetBusy(value);
        UpdateActions();
    }

    private async void DetectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DetectRequested is not null)
        {
            await DetectRequested.Invoke(viewModel.SelectedPath);
        }
    }

    private async void AnalyzeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (AnalyzeRequested is not null)
        {
            await AnalyzeRequested.Invoke(viewModel.SelectedPath);
        }
    }

    private async void OptimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (OptimizeRequested is not null)
        {
            await OptimizeRequested.Invoke(viewModel.SelectedPath, viewModel.UseExtremeResolution, viewModel.SelectedFps);
        }
    }

    private async void ServerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PrepareServerRequested is not null)
        {
            await PrepareServerRequested.Invoke(viewModel.SelectedPath);
        }
    }

    private async void TestButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                "Abra o Minecraft, chegue ao menu e tente entrar no servidor.\n\n" +
                "Depois clique OK. O ApexTweaker observara o processo Java por 60 segundos. FPS nao e medido automaticamente.",
                "Testar Cobblemon",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information) != MessageBoxResult.OK)
        {
            return;
        }

        if (TestRequested is not null)
        {
            await TestRequested.Invoke(viewModel.SelectedPath);
        }
    }

    private async void SaveTestButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SaveTestRequested is not null)
        {
            await SaveTestRequested.Invoke(viewModel.SelectedPath, viewModel.BuildObservation());
        }
    }

    private async void FixButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (FixRequested is not null)
        {
            await FixRequested.Invoke(viewModel.SelectedPath);
        }
    }

    private async void RestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RestoreRequested is not null)
        {
            await RestoreRequested.Invoke(viewModel.SelectedPath);
        }
    }

    private async void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ExportRequested is not null)
        {
            await ExportRequested.Invoke(viewModel.SelectedPath);
        }
    }

    private void AdvancedButton_OnClick(object sender, RoutedEventArgs e) => AdvancedRequested?.Invoke();

    private void UpdateActions()
    {
        var available = !viewModel.IsBusy;
        DetectButton.IsEnabled = available;
        AnalyzeButton.IsEnabled = available && viewModel.InstanceReady;
        OptimizeButton.IsEnabled = available && viewModel.InstanceReady && viewModel.AuditReady;
        ServerButton.IsEnabled = available && viewModel.AuditReady;
        TestButton.IsEnabled = available && viewModel.InstanceReady;
        FixButton.IsEnabled = available && viewModel.IsTestPanelVisible;
        RestoreButton.IsEnabled = available && viewModel.InstanceReady;
        ExportButton.IsEnabled = available && viewModel.InstanceReady;
    }
}
