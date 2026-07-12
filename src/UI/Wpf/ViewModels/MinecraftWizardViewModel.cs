using System.Collections.ObjectModel;
using ApexTweaker.Minecraft.Models;
using ApexTweaker.UI.Wpf.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApexTweaker.UI.Wpf.ViewModels;

internal enum MinecraftWizardStepState
{
    NotTested,
    Active,
    Complete,
    Attention,
    Critical,
    Reverted,
    Kept,
    Inconclusive
}

internal sealed partial class MinecraftWizardStepViewModel : ObservableObject
{
    public MinecraftWizardStepViewModel(int number, string title, string description)
    {
        Number = number;
        Title = title;
        Description = description;
    }

    public int Number { get; }

    public string Title { get; }

    public string Description { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateLabel))]
    [NotifyPropertyChangedFor(nameof(StateColor))]
    private MinecraftWizardStepState state = MinecraftWizardStepState.NotTested;

    [ObservableProperty]
    private string detail = "Nao testado";

    public string StateLabel => State switch
    {
        MinecraftWizardStepState.Active => "EM ANDAMENTO",
        MinecraftWizardStepState.Complete => "SEGURO",
        MinecraftWizardStepState.Attention => "ATENCAO",
        MinecraftWizardStepState.Critical => "CRITICO",
        MinecraftWizardStepState.Reverted => "REVERTIDO",
        MinecraftWizardStepState.Kept => "MANTIDO",
        MinecraftWizardStepState.Inconclusive => "INCONCLUSIVO",
        _ => "NAO TESTADO"
    };

    public string StateColor => State switch
    {
        MinecraftWizardStepState.Active => "#4DA3FF",
        MinecraftWizardStepState.Complete or MinecraftWizardStepState.Kept => "#47D18C",
        MinecraftWizardStepState.Attention or MinecraftWizardStepState.Inconclusive => "#F5B942",
        MinecraftWizardStepState.Critical => "#FF6B6B",
        MinecraftWizardStepState.Reverted => "#64B5F6",
        _ => "#8892A6"
    };
}

internal sealed record MinecraftVisualStateItem(string Label, string Color, string Meaning);

internal sealed partial class MinecraftWizardViewModel : ObservableObject
{
    private const int BenchmarkDurationSeconds = 60;

    public MinecraftWizardViewModel()
    {
        Steps = new ObservableCollection<MinecraftWizardStepViewModel>
        {
            new(1, "Objetivo", "Entenda os limites reais de 4 GB e as regras de seguranca."),
            new(2, "Instancia", "Selecione Prism, MultiMC, Modrinth ou uma pasta valida."),
            new(3, "Diagnostico", "Leia hardware, Java, pagefile, disco e processos."),
            new(4, "Mods", "Classifique dependencias, duplicados e peso visual."),
            new(5, "Modo", "Escolha primeiro teste, Potato ou uma hipotese isolada."),
            new(6, "Baseline", "Meca o estado atual sem contaminar a rodada."),
            new(7, "Candidato", "Revise o diff e aplique somente com backup."),
            new(8, "Pos-teste", "Repita a mesma cena e os mesmos 60 segundos."),
            new(9, "Resultado", "Compare fatos, dados manuais e confianca."),
            new(10, "Finalizar", "Mantenha, reverta ou repita e exporte a evidencia.")
        };
        VisualStates =
        [
            new("Seguro", "#47D18C", "Alteracao reversivel e de baixo risco"),
            new("Atencao", "#F5B942", "Exige leitura ou confirmacao"),
            new("Critico", "#FF6B6B", "Bloqueia aplicacao"),
            new("Nao testado", "#8892A6", "Sem evidencia suficiente"),
            new("Medido", "#4DA3FF", "Coletado automaticamente"),
            new("Inferido", "#D995FF", "Conclusao derivada de fatos"),
            new("Manual", "#F2A65A", "Informado pelo usuario"),
            new("Revertido", "#64B5F6", "Backup restaurado"),
            new("Mantido", "#47D18C", "Candidato aprovado"),
            new("Inconclusivo", "#F5B942", "Nova rodada obrigatoria")
        ];
        BenchmarkPoints = [];
        Steps[0].State = MinecraftWizardStepState.Active;
        Steps[0].Detail = "Comece aqui";
        BackCommand = new RelayCommand(Back, () => CurrentStepIndex > 0 && !IsBusy);
        NextCommand = new RelayCommand(Next, () => CurrentStepIndex < Steps.Count - 1 && !IsBusy);
        ToggleModeCommand = new RelayCommand(() => IsAdvancedMode = !IsAdvancedMode);
        CancelCommand = new RelayCommand(() => CancelRequested?.Invoke(), () => IsBenchmarkRunning);
    }

    public event Action? CancelRequested;

    public ObservableCollection<MinecraftWizardStepViewModel> Steps { get; }

    public IReadOnlyList<MinecraftVisualStateItem> VisualStates { get; }

    public ObservableCollection<BenchmarkChartPoint> BenchmarkPoints { get; }

    public IRelayCommand BackCommand { get; }

    public IRelayCommand NextCommand { get; }

    public IRelayCommand ToggleModeCommand { get; }

    public IRelayCommand CancelCommand { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentStep))]
    [NotifyPropertyChangedFor(nameof(CurrentStepNumber))]
    [NotifyPropertyChangedFor(nameof(OverallProgress))]
    private int currentStepIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModeLabel))]
    private bool isAdvancedMode;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isBenchmarkRunning;

    [ObservableProperty]
    private double benchmarkProgress;

    [ObservableProperty]
    private string operationStatus = "Pronto para iniciar em modo seguro.";

    [ObservableProperty]
    private string instanceSummary = "Nenhuma instancia validada.";

    [ObservableProperty]
    private string auditSummary = "Auditoria ainda nao executada.";

    [ObservableProperty]
    private string comparisonSummary = "Baseline e candidato ainda nao comparados.";

    public MinecraftWizardStepViewModel CurrentStep => Steps[CurrentStepIndex];

    public int CurrentStepNumber => CurrentStepIndex + 1;

    public double OverallProgress => CurrentStepIndex * 100d / (Steps.Count - 1);

    public string ModeLabel => IsAdvancedMode ? "Modo avancado" : "Modo simples";

    partial void OnCurrentStepIndexChanged(int oldValue, int newValue)
    {
        if (oldValue >= 0 && oldValue < Steps.Count && Steps[oldValue].State == MinecraftWizardStepState.Active)
        {
            Steps[oldValue].State = MinecraftWizardStepState.Complete;
            Steps[oldValue].Detail = "Etapa visitada";
        }

        Steps[newValue].State = MinecraftWizardStepState.Active;
        Steps[newValue].Detail = "Etapa atual";
        RefreshCommands();
    }

    partial void OnIsBusyChanged(bool value) => RefreshCommands();

    partial void OnIsBenchmarkRunningChanged(bool value) => CancelCommand.NotifyCanExecuteChanged();

    public void GoToStep(int index)
    {
        CurrentStepIndex = Math.Clamp(index, 0, Steps.Count - 1);
    }

    public void SetInstanceState(bool valid, string summary)
    {
        InstanceSummary = summary;
        SetStep(1, valid ? MinecraftWizardStepState.Complete : MinecraftWizardStepState.Attention, summary);
    }

    public void SetAudit(MinecraftAuditResult audit)
    {
        AuditSummary = $"{audit.Summary.TotalMods} mods | {audit.Summary.DuplicateModIds} duplicados | " +
                       $"{audit.Summary.PossibleConflicts} alertas | {audit.Summary.PerformanceMods} performance";
        var state = audit.Issues.Any(issue => issue.Severity == AuditSeverity.Error)
            ? MinecraftWizardStepState.Critical
            : audit.Summary.PossibleConflicts > 0
                ? MinecraftWizardStepState.Attention
                : MinecraftWizardStepState.Complete;
        SetStep(2, state, $"RAM {audit.Environment.TotalMemoryGb:0.#} GB; Java {(audit.Environment.Java.Found ? "detectado" : "ausente")}");
        SetStep(3, state, AuditSummary);
    }

    public void SetProfilePlan(MinecraftProfilePlan plan)
    {
        var label = plan.Experiment?.DisplayName ?? plan.Profile.ToString();
        SetStep(4, MinecraftWizardStepState.Complete, $"{label} | {plan.MaximumHeapMb} MB | {plan.MaximumFps} FPS");
        OperationStatus = $"Dry-run pronto: {plan.Changes.Count(change => change.WillWrite)} alteracoes permitidas.";
    }

    public void SetExperiment(MinecraftScientificExperiment experiment)
    {
        switch (experiment.Phase)
        {
            case ScientificExperimentPhase.BaselinePending:
                SetStep(5, MinecraftWizardStepState.Active, "Aguardando medicao baseline");
                break;
            case ScientificExperimentPhase.BaselineRecorded:
                SetStep(5, MinecraftWizardStepState.Complete, "Baseline congelado");
                SetStep(6, MinecraftWizardStepState.Active, "Candidato pronto para confirmacao");
                break;
            case ScientificExperimentPhase.CandidateApplied:
                SetStep(6, MinecraftWizardStepState.Complete, "Candidato aplicado com backup");
                SetStep(7, MinecraftWizardStepState.Active, "Repita o benchmark");
                break;
            case ScientificExperimentPhase.CandidateRecorded:
                SetStep(7, MinecraftWizardStepState.Complete, "Pos-teste registrado");
                SetStep(8, MinecraftWizardStepState.Active, "Comparacao pendente");
                break;
            case ScientificExperimentPhase.Compared:
                ComparisonSummary = $"{experiment.Comparison?.Decision} | confianca {experiment.Comparison?.Confidence}";
                SetStep(8, MinecraftWizardStepState.Complete, ComparisonSummary);
                SetStep(9, MinecraftWizardStepState.Active, "Finalize a decisao");
                break;
            case ScientificExperimentPhase.Kept:
                SetStep(9, MinecraftWizardStepState.Kept, "Candidato mantido");
                break;
            case ScientificExperimentPhase.Reverted:
                SetStep(9, MinecraftWizardStepState.Reverted, "Backup restaurado");
                break;
            case ScientificExperimentPhase.NeedsRetest:
                SetStep(9, MinecraftWizardStepState.Inconclusive, "Nova rodada obrigatoria");
                break;
            case ScientificExperimentPhase.Failed:
                SetStep(9, MinecraftWizardStepState.Critical, "Experimento falhou");
                break;
        }
    }

    public void BeginBenchmark()
    {
        BenchmarkPoints.Clear();
        BenchmarkProgress = 0;
        IsBenchmarkRunning = true;
        OperationStatus = "Benchmark automatico em andamento; FPS continua manual.";
    }

    public void AddBenchmarkSample(MinecraftBenchmarkSample sample)
    {
        BenchmarkPoints.Add(new BenchmarkChartPoint(
            BenchmarkPoints.Count + 1,
            sample.WorkingSetBytes / 1024d / 1024d,
            (double)sample.AvailableMemoryGb * 1024d,
            sample.CpuPercent,
            sample.CommitUsedMb));
        BenchmarkProgress = Math.Min(100, BenchmarkPoints.Count * 100d / BenchmarkDurationSeconds);
        OperationStatus = $"{BenchmarkPoints.Count}s | Java {sample.WorkingSetBytes / 1024d / 1024d:0} MB | " +
                          $"CPU {sample.CpuPercent:0.0}% | commit {sample.CommitUsedMb} MB";
    }

    public void CompleteBenchmark(MinecraftBenchmarkResult? result, bool cancelled = false)
    {
        IsBenchmarkRunning = false;
        BenchmarkProgress = cancelled ? BenchmarkProgress : 100;
        OperationStatus = cancelled
            ? "Benchmark cancelado com seguranca; nenhuma configuracao foi alterada."
            : result is null
                ? "Benchmark indisponivel."
                : $"Benchmark {result.Status}; FPS nao foi capturado automaticamente.";
    }

    public void SetBusyState(bool value)
    {
        IsBusy = value;
    }

    private void Back()
    {
        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
        }
    }

    private void Next()
    {
        if (CurrentStepIndex < Steps.Count - 1)
        {
            CurrentStepIndex++;
        }
    }

    private void SetStep(int index, MinecraftWizardStepState state, string detail)
    {
        Steps[index].State = state;
        Steps[index].Detail = detail;
    }

    private void RefreshCommands()
    {
        BackCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }
}
