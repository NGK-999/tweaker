using System;
using System.Collections.Generic;
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
    private bool profilePreviewReady;
    private bool quarantinePlanReady;

    public event Action? BrowseRequested;

    public event Func<string, Task>? AuditRequested;

    internal event Func<string, MinecraftProfileKind, Task>? ApplyProfileRequested;

    internal event Func<string, MinecraftProfileKind, Task>? PreviewProfileRequested;

    public event Func<string, Task>? RollbackRequested;

    public event Func<IReadOnlyList<string>, Task>? ApplyQuarantineRequested;

    public event Func<string, Task>? RollbackQuarantineRequested;

    public event Func<string, Task>? BenchmarkRequested;

    public event Action? OpenReportsRequested;

    public MinecraftView()
    {
        InitializeComponent();
        ProfileComboBox.ItemsSource = MinecraftProfileService.AvailableProfiles
            .OrderBy(profile => profile.Kind)
            .ToArray();
        ProfileComboBox.SelectedItem = MinecraftProfileService.AvailableProfiles
            .First(profile => profile.Kind == MinecraftProfileKind.Extreme4Gb);
        ProfileComboBox.SelectionChanged += (_, _) =>
        {
            profilePreviewReady = false;
            UpdateActionState();
        };
        UpdateActionState();
    }

    public string SelectedPath => PathTextBox.Text.Trim();

    public void SetSelectedPath(string path)
    {
        PathTextBox.Text = path;
        instanceDetected = MinecraftProfileService.TryResolveInstanceRoot(path, out _);
        profilePreviewReady = false;
        quarantinePlanReady = false;
        QuarantineList.ItemsSource = null;
        InstanceStateText.Text = instanceDetected
            ? "Instancia valida detectada. Perfis e rollback estao disponiveis."
            : "Pasta aceita para auditoria. Perfil bloqueado ate selecionar uma instancia com options.txt e subpasta mods.";
        UpdateActionState();
    }

    internal void SetAuditResult(
        MinecraftAuditResult result,
        MinecraftReportPaths reports,
        MinecraftQuarantinePlan quarantine,
        MinecraftSurvivalPlan survival)
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
        QuarantineList.ItemsSource = quarantine.Candidates;
        quarantinePlanReady = quarantine.Candidates.Count > 0;
        SurvivalVerdictText.Text = survival.Verdict;
        SurvivalDetailsText.Text =
            $"JVM: {survival.JavaArguments}\n" +
            $"Candidatos: {survival.QuarantineCandidates.Count} | Riscos: {survival.Risks.Count}\n" +
            string.Join(" | ", survival.GraphicsSettings.Take(3));
        OperationText.Text =
            $"Relatorios gerados em {System.IO.Path.GetDirectoryName(reports.JsonPath)}. " +
            $"Sugestoes: {reports.QuarantineSuggestionsDirectory}. Nenhum JAR foi alterado.";
        InstanceStateText.Text = result.InstanceRootDetected
            ? $"Instancia valida: {result.InstanceRoot}"
            : "A pasta contem mods, mas nao e uma instancia completa. Auditoria concluida; aplicacao de perfil permanece bloqueada.";
        UpdateActionState();
    }

    internal void SetProfilePlan(MinecraftProfilePlan plan, string reportPath)
    {
        profilePreviewReady = true;
        JavaArgumentsTextBox.Text = plan.JavaArguments;
        var changed = plan.Changes.Where(change => change.WillWrite).ToArray();
        OperationText.Text =
            $"DRY-RUN: {changed.Length} alteracoes em {changed.Select(change => change.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count()} arquivo(s). " +
            $"Relatorio: {reportPath}";
        UpdateActionState();
    }

    public void MarkProfileApplied()
    {
        profilePreviewReady = false;
        UpdateActionState();
    }

    public void ClearQuarantineSelection()
    {
        QuarantineList.UnselectAll();
        UpdateActionState();
    }

    public void InvalidateQuarantinePlan()
    {
        quarantinePlanReady = false;
        QuarantineList.ItemsSource = null;
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
        PreviewProfileButton.IsEnabled = !busy && instanceDetected;
        ApplyProfileButton.IsEnabled = !busy && instanceDetected && profilePreviewReady;
        RollbackButton.IsEnabled = !busy && instanceDetected;
        QuarantineList.IsEnabled = !busy && quarantinePlanReady;
        ApplyQuarantineButton.IsEnabled = !busy && quarantinePlanReady && QuarantineList.SelectedItems.Count > 0;
        RollbackQuarantineButton.IsEnabled = !busy && DirectoryPathAvailable();
        BenchmarkButton.IsEnabled = !busy;
        OpenReportsButton.IsEnabled = !busy;
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        BrowseRequested?.Invoke();
    }

    private void PathTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        instanceDetected = MinecraftProfileService.TryResolveInstanceRoot(SelectedPath, out _);
        profilePreviewReady = false;
        quarantinePlanReady = false;
        if (QuarantineList is not null)
        {
            QuarantineList.ItemsSource = null;
        }

        UpdateActionState();
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

    private async void PreviewProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PreviewProfileRequested is not null && ProfileComboBox.SelectedItem is MinecraftProfileDefinition profile)
        {
            await PreviewProfileRequested.Invoke(SelectedPath, profile.Kind);
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
            await BenchmarkRequested.Invoke(SelectedPath);
        }
    }

    private async void ApplyQuarantineButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ApplyQuarantineRequested is not null)
        {
            var selected = QuarantineList.SelectedItems
                .Cast<MinecraftQuarantineCandidate>()
                .Select(candidate => candidate.FileName)
                .ToArray();
            await ApplyQuarantineRequested.Invoke(selected);
        }
    }

    private async void RollbackQuarantineButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RollbackQuarantineRequested is not null)
        {
            await RollbackQuarantineRequested.Invoke(SelectedPath);
        }
    }

    private void QuarantineList_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateActionState();
    }

    private void OpenReportsButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenReportsRequested?.Invoke();
    }

    private bool DirectoryPathAvailable()
    {
        return !string.IsNullOrWhiteSpace(SelectedPath) && System.IO.Directory.Exists(SelectedPath);
    }
}
