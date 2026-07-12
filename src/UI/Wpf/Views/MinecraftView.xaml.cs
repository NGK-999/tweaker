using System;
using System.Collections.Generic;
using System.Globalization;
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
    private bool auditAvailable;
    private bool instanceDetected;
    private bool profilePreviewReady;
    private bool quarantinePlanReady;
    private ScientificExperimentPhase? scientificPhase;

    public event Action? BrowseRequested;

    public event Func<string, Task>? AuditRequested;

    internal event Func<string, MinecraftProfileKind, int, Task>? ApplyProfileRequested;

    internal event Func<string, MinecraftProfileKind, int, Task>? PreviewProfileRequested;

    public event Func<string, Task>? RollbackRequested;

    public event Func<IReadOnlyList<string>, Task>? ApplyQuarantineRequested;

    public event Func<string, Task>? RollbackQuarantineRequested;

    public event Func<string, Task>? BenchmarkRequested;

    internal event Func<string, int, Task>? ExportChecklistRequested;

    internal event Func<string, MinecraftOperationalObservation, Task>? SaveHomologationRequested;

    internal event Func<string, int, Task>? ScientificPlanRequested;

    internal event Func<string, int, Task>? ScientificStartRequested;

    internal event Func<string, MinecraftOperationalObservation, Task>? ScientificAdvanceRequested;

    public event Action? OpenReportsRequested;

    public MinecraftView()
    {
        InitializeComponent();
        ProfileComboBox.ItemsSource = MinecraftProfileService.AvailableProfiles
            .OrderBy(profile => profile.Kind)
            .ToArray();
        ProfileComboBox.SelectedItem = MinecraftProfileService.AvailableProfiles
            .First(profile => profile.Kind == MinecraftProfileKind.Extreme4Gb);
        FpsComboBox.ItemsSource = new[] { 30, 45, 60 };
        FpsComboBox.SelectedItem = 45;
        ProfileComboBox.SelectionChanged += (_, _) =>
        {
            profilePreviewReady = false;
            UpdateActionState();
        };
        FpsComboBox.SelectionChanged += (_, _) =>
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
        auditAvailable = false;
        profilePreviewReady = false;
        quarantinePlanReady = false;
        scientificPhase = null;
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
        auditAvailable = true;

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
            $"FPS {plan.MaximumFps}, heap {plan.MaximumHeapMb} MB. Relatorio: {reportPath}";
        OperationalStatusText.Text = plan.JavaMemoryReason;
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

    internal void SetOperationalResult(OperationalHomologationStatus status, string reportPath)
    {
        OperationalStatusText.Text = $"Resultado {status}. Relatorio: {reportPath}";
    }

    public void SetOperationalChecklist(string reportPath)
    {
        OperationalStatusText.Text = $"Checklist exportado: {reportPath}";
    }

    internal void SetScientificPlan(MinecraftScientificOptimizationPlan plan, string reportPath)
    {
        ScientificStatusText.Text =
            $"Gargalo {plan.Diagnosis.Primary} ({plan.Diagnosis.Confidence}). " +
            $"Candidato {plan.SelectedProfile}, {plan.JavaMemory.Arguments}, {plan.MaximumFps} FPS. " +
            $"Bloqueadores: {(plan.HasCriticalBlockers ? "SIM" : "NAO")}. Relatorio: {reportPath}";
    }

    internal void SetScientificExperiment(MinecraftScientificExperiment experiment, string reportPath)
    {
        scientificPhase = experiment.Phase;
        ScientificStatusText.Text =
            $"{experiment.ExperimentId} | fase {experiment.Phase} | " +
            $"decisao {experiment.Comparison?.Decision.ToString() ?? "PENDENTE"}. Relatorio: {reportPath}";
        ScientificAdvanceButton.Content = experiment.Phase switch
        {
            ScientificExperimentPhase.BaselinePending => "Registrar baseline",
            ScientificExperimentPhase.BaselineRecorded => "Aplicar candidato",
            ScientificExperimentPhase.CandidateApplied => "Registrar candidato",
            ScientificExperimentPhase.CandidateRecorded => "Comparar rodadas",
            ScientificExperimentPhase.Compared => "Finalizar decisao",
            ScientificExperimentPhase.NeedsRetest => "Novo teste necessario",
            ScientificExperimentPhase.Kept => "Candidato mantido",
            ScientificExperimentPhase.Reverted => "Rollback concluido",
            _ => "Experimento falhou"
        };
        UpdateActionState();
    }

    public void ClearOperationalObservation()
    {
        GameOpenedCheckBox.IsChecked = false;
        MenuReachedCheckBox.IsChecked = false;
        WorldEnteredCheckBox.IsChecked = false;
        ServerEnteredCheckBox.IsChecked = false;
        Playable720pCheckBox.IsChecked = false;
        SevereDropsCheckBox.IsChecked = false;
        CrashedCheckBox.IsChecked = false;
        OutOfMemoryCheckBox.IsChecked = false;
        MenuSecondsTextBox.Clear();
        JoinSecondsTextBox.Clear();
        AverageFpsTextBox.Clear();
        MinimumFpsTextBox.Clear();
        OperationalNotesTextBox.Clear();
    }

    public void InvalidateScientificExperiment(string reason)
    {
        scientificPhase = null;
        ScientificAdvanceButton.Content = "Avancar etapa";
        ScientificStatusText.Text = reason;
        UpdateActionState();
    }

    private void UpdateActionState()
    {
        BrowseButton.IsEnabled = !busy;
        AuditButton.IsEnabled = !busy;
        PathTextBox.IsEnabled = !busy;
        ProfileComboBox.IsEnabled = !busy && instanceDetected;
        FpsComboBox.IsEnabled = !busy && instanceDetected;
        PreviewProfileButton.IsEnabled = !busy && instanceDetected;
        ApplyProfileButton.IsEnabled = !busy && instanceDetected && profilePreviewReady;
        RollbackButton.IsEnabled = !busy && instanceDetected;
        QuarantineList.IsEnabled = !busy && quarantinePlanReady;
        ApplyQuarantineButton.IsEnabled = !busy && quarantinePlanReady && QuarantineList.SelectedItems.Count > 0;
        RollbackQuarantineButton.IsEnabled = !busy && DirectoryPathAvailable();
        BenchmarkButton.IsEnabled = !busy;
        ScientificPlanButton.IsEnabled = !busy && instanceDetected;
        ScientificStartButton.IsEnabled = !busy && instanceDetected;
        ScientificAdvanceButton.IsEnabled = !busy && scientificPhase is
            ScientificExperimentPhase.BaselinePending or
            ScientificExperimentPhase.BaselineRecorded or
            ScientificExperimentPhase.CandidateApplied or
            ScientificExperimentPhase.CandidateRecorded or
            ScientificExperimentPhase.Compared;
        ExportChecklistButton.IsEnabled = !busy && auditAvailable;
        SaveHomologationButton.IsEnabled = !busy && instanceDetected;
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
        auditAvailable = false;
        profilePreviewReady = false;
        quarantinePlanReady = false;
        scientificPhase = null;
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
            await ApplyProfileRequested.Invoke(SelectedPath, profile.Kind, SelectedFps);
        }
    }

    private async void PreviewProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PreviewProfileRequested is not null && ProfileComboBox.SelectedItem is MinecraftProfileDefinition profile)
        {
            await PreviewProfileRequested.Invoke(SelectedPath, profile.Kind, SelectedFps);
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

    private async void ExportChecklistButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ExportChecklistRequested is not null)
        {
            await ExportChecklistRequested.Invoke(SelectedPath, SelectedFps);
        }
    }

    private async void SaveHomologationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SaveHomologationRequested is null || !TryReadOperationalObservation(out var observation))
        {
            return;
        }

        await SaveHomologationRequested.Invoke(SelectedPath, observation);
    }

    private async void ScientificPlanButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ScientificPlanRequested is not null)
        {
            await ScientificPlanRequested.Invoke(SelectedPath, SelectedFps);
        }
    }

    private async void ScientificStartButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ScientificStartRequested is not null)
        {
            await ScientificStartRequested.Invoke(SelectedPath, SelectedFps);
        }
    }

    private async void ScientificAdvanceButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ScientificAdvanceRequested is null || scientificPhase is null)
        {
            return;
        }

        var observation = EmptyObservation();
        if (scientificPhase is ScientificExperimentPhase.BaselinePending or ScientificExperimentPhase.CandidateApplied &&
            !TryReadOperationalObservation(out observation))
        {
            return;
        }

        await ScientificAdvanceRequested.Invoke(SelectedPath, observation);
    }

    private int SelectedFps => FpsComboBox.SelectedItem is int fps ? fps : 45;

    private bool TryReadOperationalObservation(out MinecraftOperationalObservation observation)
    {
        observation = default!;
        if (!TryReadDecimal(MenuSecondsTextBox.Text, "Tempo ate o menu", out var menuSeconds) ||
            !TryReadDecimal(JoinSecondsTextBox.Text, "Tempo de entrada", out var joinSeconds) ||
            !TryReadDouble(AverageFpsTextBox.Text, "FPS medio", out var averageFps) ||
            !TryReadDouble(MinimumFpsTextBox.Text, "FPS minimo", out var minimumFps))
        {
            return false;
        }

        observation = new MinecraftOperationalObservation(
            GameOpenedCheckBox.IsChecked == true,
            MenuReachedCheckBox.IsChecked == true,
            menuSeconds,
            WorldEnteredCheckBox.IsChecked == true,
            ServerEnteredCheckBox.IsChecked == true,
            joinSeconds,
            Playable720pCheckBox.IsChecked == true,
            averageFps,
            minimumFps,
            SevereDropsCheckBox.IsChecked == true,
            CrashedCheckBox.IsChecked == true,
            OutOfMemoryCheckBox.IsChecked == true,
            OperationalNotesTextBox.Text.Trim());
        return true;
    }

    private static bool TryReadDecimal(string value, string label, out decimal? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim().Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
            parsed >= 0)
        {
            result = parsed;
            return true;
        }

        ShowInvalidNumber(label);
        return false;
    }

    private static bool TryReadDouble(string value, string label, out double? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim().Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
            parsed >= 0)
        {
            result = parsed;
            return true;
        }

        ShowInvalidNumber(label);
        return false;
    }

    private static void ShowInvalidNumber(string label)
    {
        System.Windows.MessageBox.Show(
            $"{label} deve ser um numero positivo ou ficar vazio quando nao foi medido.",
            "Homologacao operacional",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static MinecraftOperationalObservation EmptyObservation()
    {
        return new MinecraftOperationalObservation(
            false, false, null, false, false, null, false, null, null, false, false, false, string.Empty);
    }

    private bool DirectoryPathAvailable()
    {
        return !string.IsNullOrWhiteSpace(SelectedPath) && System.IO.Directory.Exists(SelectedPath);
    }
}
