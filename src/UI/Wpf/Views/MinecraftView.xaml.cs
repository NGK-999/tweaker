using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ApexTweaker.Minecraft.Models;
using ApexTweaker.Minecraft.Services;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace ApexTweaker.UI.Wpf.Views;

public partial class MinecraftView : WpfUserControl
{
    private bool busy;
    private bool instanceDetected;

    public event Action? BrowseRequested;

    public event Func<string, Task>? AuditRequested;

    internal event Func<string, MinecraftProfileKind, Task>? ApplyProfileRequested;

    public event Func<string, Task>? RollbackRequested;

    public event Func<Task>? BenchmarkRequested;

    public event Action? OpenReportsRequested;

    public MinecraftView()
    {
        InitializeComponent();
        ProfileComboBox.ItemsSource = MinecraftProfileService.AvailableProfiles
            .OrderBy(profile => profile.Kind)
            .ToArray();
        ProfileComboBox.SelectedItem = MinecraftProfileService.AvailableProfiles
            .First(profile => profile.Kind == MinecraftProfileKind.Extreme4Gb);
        UpdateActionState();
    }

    public string SelectedPath => PathTextBox.Text.Trim();

    public void SetSelectedPath(string path)
    {
        PathTextBox.Text = path;
        instanceDetected = MinecraftProfileService.TryResolveInstanceRoot(path, out _);
        InstanceStateText.Text = instanceDetected
            ? "Instancia valida detectada. Perfis e rollback estao disponiveis."
            : "Pasta aceita para auditoria. Perfil bloqueado ate selecionar uma instancia com options.txt e subpasta mods.";
        UpdateActionState();
    }

    internal void SetAuditResult(MinecraftAuditResult result, MinecraftReportPaths reports)
    {
        TotalModsText.Text = result.Summary.TotalMods.ToString();
        PerformanceModsText.Text = result.Summary.PerformanceMods.ToString();
        DuplicateModsText.Text = result.Summary.DuplicateModIds.ToString();
        IssuesText.Text = result.Summary.PossibleConflicts.ToString();
        instanceDetected = result.InstanceRootDetected;

        EnvironmentText.Text =
            $"{result.Environment.Processor}\n" +
            $"RAM {result.Environment.TotalMemoryGb:0.##} GB / livre {result.Environment.AvailableMemoryGb:0.##} GB | " +
            $"Pagefile {result.Environment.PageFileAllocatedMb} MB\n" +
            $"Java {(result.Environment.Java.Found ? result.Environment.Java.Version : "nao detectado")} | " +
            $"GPU {string.Join(", ", result.Environment.Gpus.DefaultIfEmpty("indisponivel"))}";

        IssueList.ItemsSource = result.Issues
            .Take(12)
            .Select(issue => $"[{issue.Severity}] {issue.Code}: {issue.Message}")
            .ToArray();
        JavaArgumentsTextBox.Text = result.Environment.RecommendedJavaArguments;
        OperationText.Text =
            $"Relatorios gerados em {System.IO.Path.GetDirectoryName(reports.JsonPath)}. " +
            $"Sugestoes: {reports.QuarantineSuggestionsDirectory}. Nenhum JAR foi alterado.";
        InstanceStateText.Text = result.InstanceRootDetected
            ? $"Instancia valida: {result.InstanceRoot}"
            : "A pasta contem mods, mas nao e uma instancia completa. Auditoria concluida; aplicacao de perfil permanece bloqueada.";
        UpdateActionState();
    }

    public void SetBusy(bool value)
    {
        busy = value;
        UpdateActionState();
    }

    public void SetOperationText(string message)
    {
        OperationText.Text = message;
    }

    public void SetJavaArguments(string arguments)
    {
        JavaArgumentsTextBox.Text = arguments;
    }

    private void UpdateActionState()
    {
        BrowseButton.IsEnabled = !busy;
        AuditButton.IsEnabled = !busy;
        PathTextBox.IsEnabled = !busy;
        ProfileComboBox.IsEnabled = !busy && instanceDetected;
        ApplyProfileButton.IsEnabled = !busy && instanceDetected;
        RollbackButton.IsEnabled = !busy && instanceDetected;
        BenchmarkButton.IsEnabled = !busy;
        OpenReportsButton.IsEnabled = !busy;
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        BrowseRequested?.Invoke();
    }

    private async void AuditButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (AuditRequested is not null)
        {
            await AuditRequested.Invoke(SelectedPath);
        }
    }

    private async void ApplyProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ApplyProfileRequested is not null && ProfileComboBox.SelectedItem is MinecraftProfileDefinition profile)
        {
            await ApplyProfileRequested.Invoke(SelectedPath, profile.Kind);
        }
    }

    private async void RollbackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RollbackRequested is not null)
        {
            await RollbackRequested.Invoke(SelectedPath);
        }
    }

    private async void BenchmarkButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (BenchmarkRequested is not null)
        {
            await BenchmarkRequested.Invoke();
        }
    }

    private void OpenReportsButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenReportsRequested?.Invoke();
    }
}
