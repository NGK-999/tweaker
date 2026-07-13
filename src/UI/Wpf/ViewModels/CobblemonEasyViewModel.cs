using ApexTweaker.Minecraft.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApexTweaker.UI.Wpf.ViewModels;

internal enum EasyStepState
{
    NotStarted,
    Ready,
    Running,
    Completed,
    Attention,
    Failed,
    Blocked
}

internal sealed partial class EasyStepViewModel : ObservableObject
{
    [ObservableProperty]
    private EasyStepState state;

    [ObservableProperty]
    private bool isCurrent;

    public EasyStepViewModel(EasyStepState state, bool isCurrent = false)
    {
        this.state = state;
        this.isCurrent = isCurrent;
    }

    public string StateLabel => State switch
    {
        EasyStepState.NotStarted => "N\u00E3o iniciado",
        EasyStepState.Ready => "Pronto para executar",
        EasyStepState.Running => "Executando",
        EasyStepState.Completed => "Conclu\u00EDdo",
        EasyStepState.Attention => "Aten\u00E7\u00E3o",
        EasyStepState.Failed => "Falhou",
        _ => "Bloqueado"
    };

    public string Color => State switch
    {
        EasyStepState.Completed => "#67D89B",
        EasyStepState.Attention => "#F5B942",
        EasyStepState.Failed => "#FF756B",
        EasyStepState.Running or EasyStepState.Ready => "#65B1FF",
        _ => "#718096"
    };

    public string Background => IsCurrent
        ? State switch
        {
            EasyStepState.Attention => "#302817",
            EasyStepState.Failed => "#351D22",
            EasyStepState.Completed => "#153126",
            _ => "#142C49"
        }
        : "#111D2D";

    public string BorderBrush => IsCurrent ? Color : "#2B3D58";

    public void Set(EasyStepState value, bool current = false)
    {
        State = value;
        IsCurrent = current;
    }

    partial void OnStateChanged(EasyStepState value) => NotifyVisuals();

    partial void OnIsCurrentChanged(bool value) => NotifyVisuals();

    private void NotifyVisuals()
    {
        OnPropertyChanged(nameof(StateLabel));
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(BorderBrush));
    }
}

internal sealed partial class CobblemonEasyViewModel : ObservableObject
{
    [ObservableProperty]
    private string selectedPath = "Nenhuma instancia selecionada.";

    [ObservableProperty]
    private MinecraftEasyState overallState = MinecraftEasyState.TestRequired;

    [ObservableProperty]
    private string overallStatus = "Aguardando detec\u00E7\u00E3o";

    [ObservableProperty]
    private string statusMessage = "Aguardando detec\u00E7\u00E3o da inst\u00E2ncia Minecraft.";

    [ObservableProperty]
    private string nextAction = "Detectar Instancia Agora";

    [ObservableProperty]
    private string instanceDetails = "Ainda nao verificado.";

    [ObservableProperty]
    private string essentialMods = "--";

    [ObservableProperty]
    private string performanceMods = "--";

    [ObservableProperty]
    private string heavyMods = "--";

    [ObservableProperty]
    private string duplicateMods = "--";

    [ObservableProperty]
    private string riskCount = "--";

    [ObservableProperty]
    private string modDetails = "Clique em Analisar Mods para gerar o resumo.";

    [ObservableProperty]
    private string serverDetails = "A compatibilidade com o servidor ainda nao foi verificada.";

    [ObservableProperty]
    private string correctionDetails = "Execute o teste antes de procurar problemas.";

    [ObservableProperty]
    private string benchmarkSummary = "Teste ainda nao iniciado.";

    [ObservableProperty]
    private double benchmarkProgress;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool instanceReady;

    [ObservableProperty]
    private bool auditReady;

    [ObservableProperty]
    private bool optimizationApplied;

    [ObservableProperty]
    private bool hasBackup;

    [ObservableProperty]
    private bool isTestPanelVisible;

    [ObservableProperty]
    private bool isPrimaryDetectCtaVisible = true;

    [ObservableProperty]
    private bool isStatusBadgeVisible;

    [ObservableProperty]
    private bool useExtremeResolution;

    [ObservableProperty]
    private int selectedFps = 24;

    [ObservableProperty]
    private bool gameOpened;

    [ObservableProperty]
    private bool menuReached;

    [ObservableProperty]
    private bool serverEntered;

    [ObservableProperty]
    private bool severeStutter;

    [ObservableProperty]
    private bool closedAlone;

    [ObservableProperty]
    private bool modError;

    [ObservableProperty]
    private string approximateFps = string.Empty;

    [ObservableProperty]
    private string userNotes = string.Empty;

    public EasyStepViewModel DetectStep { get; } = new(EasyStepState.Ready, true);

    public EasyStepViewModel AnalyzeStep { get; } = new(EasyStepState.Blocked);

    public EasyStepViewModel OptimizeStep { get; } = new(EasyStepState.Blocked);

    public EasyStepViewModel ServerStep { get; } = new(EasyStepState.Blocked);

    public EasyStepViewModel TestStep { get; } = new(EasyStepState.Blocked);

    public EasyStepViewModel FixStep { get; } = new(EasyStepState.Blocked);

    public bool CanExport => AuditReady || IsTestPanelVisible;

    public string StateColor => OverallState switch
    {
        MinecraftEasyState.Ready or MinecraftEasyState.BackupCreated or
            MinecraftEasyState.OptimizationApplied or MinecraftEasyState.Restored => "#47D18C",
        MinecraftEasyState.TooHeavy or MinecraftEasyState.ServerMayReject or MinecraftEasyState.Failed => "#FF756B",
        MinecraftEasyState.Attention or MinecraftEasyState.Inconclusive => "#F5B942",
        _ => "#4DA3FF"
    };

    partial void OnOverallStateChanged(MinecraftEasyState value) => OnPropertyChanged(nameof(StateColor));

    partial void OnAuditReadyChanged(bool value) => OnPropertyChanged(nameof(CanExport));

    partial void OnIsTestPanelVisibleChanged(bool value) => OnPropertyChanged(nameof(CanExport));

    public void ResetForPath(string path, bool resolved)
    {
        SelectedPath = string.IsNullOrWhiteSpace(path) ? "Nenhuma instancia selecionada." : path;
        InstanceReady = false;
        AuditReady = false;
        OptimizationApplied = false;
        HasBackup = false;
        IsTestPanelVisible = false;
        IsPrimaryDetectCtaVisible = true;
        IsStatusBadgeVisible = false;
        EssentialMods = PerformanceMods = HeavyMods = DuplicateMods = RiskCount = "--";
        ModDetails = "Clique em Analisar Mods para gerar o resumo.";
        OverallState = MinecraftEasyState.TestRequired;
        OverallStatus = "Aguardando detec\u00E7\u00E3o";
        StatusMessage = "Aguardando detec\u00E7\u00E3o da inst\u00E2ncia Minecraft.";
        NextAction = "Detectar Instancia Agora";
        InstanceDetails = resolved
            ? "Pasta selecionada. Clique em Detectar Instancia Agora para validar."
            : "Ainda nao verificado.";
        ResetAnswers();
        SetCurrentStep(DetectStep, EasyStepState.Ready);
    }

    public void BeginDetection() => BeginStep(
        DetectStep,
        "Detectando instancia",
        "Procurando Minecraft, Java e arquivos principais.");

    public void SetInstance(MinecraftEasyInstanceStatus result)
    {
        OverallState = result.State;
        InstanceReady = result.Instance is not null && result.OptionsFound && result.ModsFound;
        if (result.Instance is not null)
        {
            SelectedPath = result.Instance.GameDirectory;
        }

        InstanceDetails = string.Join(
            "  |  ",
            Flag("Java 21 x64", result.JavaFound),
            Flag("options.txt", result.OptionsFound),
            Flag("mods", result.ModsFound),
            Flag("config", result.ConfigFound),
            Flag("logs", result.LogsFound));
        IsPrimaryDetectCtaVisible = !InstanceReady;
        IsStatusBadgeVisible = InstanceReady;
        if (InstanceReady)
        {
            OverallStatus = "Inst\u00E2ncia detectada";
            StatusMessage = "Inst\u00E2ncia detectada. Pr\u00F3ximo passo: Analisar Mods.";
            NextAction = "Analisar Mods";
            DetectStep.Set(EasyStepState.Completed);
            SetCurrentStep(AnalyzeStep, EasyStepState.Ready);
        }
        else
        {
            OverallStatus = result.Status;
            StatusMessage = HumanizeMessage(result.Message);
            NextAction = "Corrija os itens indicados e detecte novamente";
            SetCurrentStep(DetectStep, EasyStepState.Attention);
        }
    }

    public void BeginAnalysis() => BeginStep(
        AnalyzeStep,
        "Analisando mods",
        "Conferindo mods essenciais, desempenho, peso visual e duplicatas.");

    public void SetAudit(MinecraftEasyModSummary summary)
    {
        AuditReady = true;
        OverallState = summary.State;
        OverallStatus = summary.Risks == 0 ? "Mods analisados" : "Mods analisados com alertas";
        StatusMessage = "Mods analisados. Pr\u00F3ximo passo: Otimizar para PC Fraco.";
        EssentialMods = summary.EssentialMods.ToString();
        PerformanceMods = summary.PerformanceMods.ToString();
        HeavyMods = summary.HeavyVisualMods.ToString();
        DuplicateMods = summary.DuplicateModIds.ToString();
        RiskCount = summary.Risks.ToString();
        ModDetails = BuildSummary(summary);
        NextAction = InstanceReady ? "Otimizar para PC Fraco" : "Selecione uma instancia completa";
        AnalyzeStep.Set(summary.Risks == 0 ? EasyStepState.Completed : EasyStepState.Attention);
        ServerStep.Set(EasyStepState.Ready);
        SetCurrentStep(OptimizeStep, EasyStepState.Ready);
    }

    public void BeginOptimization() => BeginStep(
        OptimizeStep,
        "Aplicando otimizacao",
        "Criando backup antes de ajustar o Minecraft.");

    public void SetOptimizationApplied(string backupId, string javaArguments, bool javaAppliedAutomatically)
    {
        OptimizationApplied = true;
        HasBackup = !string.IsNullOrWhiteSpace(backupId);
        OverallState = MinecraftEasyState.OptimizationApplied;
        OverallStatus = "Otimizacao aplicada";
        StatusMessage = string.IsNullOrWhiteSpace(backupId)
            ? "As configura\u00E7\u00F5es j\u00E1 estavam aplicadas. Pr\u00F3ximo passo: Testar Jogo."
            : "Otimiza\u00E7\u00E3o aplicada com backup. Pr\u00F3ximo passo: Testar Jogo.";
        NextAction = "Testar Jogo";
        BenchmarkSummary = javaAppliedAutomatically
            ? $"Memoria da instancia configurada automaticamente: {javaArguments}"
            : $"Acao manual: configure no launcher {javaArguments}";
        OptimizeStep.Set(EasyStepState.Completed);
        SetCurrentStep(TestStep, EasyStepState.Ready);
    }

    public void BeginServerPreparation() => BeginStep(
        ServerStep,
        "Conferindo servidor",
        "Verificando requisitos sem mover ou remover mods.");

    public void SetServerReadiness(MinecraftEasyServerReadiness readiness)
    {
        OverallState = readiness.State;
        OverallStatus = readiness.Status;
        StatusMessage = HumanizeMessage(readiness.Message);
        ServerDetails = string.Join("\n", readiness.Checklist.Concat(readiness.Warnings).Select(item => $"- {HumanizeMessage(item)}"));
        NextAction = readiness.State == MinecraftEasyState.Ready
            ? "Proximo passo: Testar Jogo"
            : "Confirme a lista de mods do servidor antes do teste";
        ServerStep.Set(readiness.State switch
        {
            MinecraftEasyState.Ready => EasyStepState.Completed,
            MinecraftEasyState.ServerMayReject or MinecraftEasyState.Failed => EasyStepState.Failed,
            _ => EasyStepState.Attention
        });
        SetCurrentStep(OptimizationApplied ? TestStep : OptimizeStep, EasyStepState.Ready);
    }

    public void BeginBenchmark()
    {
        IsTestPanelVisible = true;
        BenchmarkProgress = 0;
        OverallState = MinecraftEasyState.TestRequired;
        OverallStatus = "Teste em andamento";
        StatusMessage = "Abra o jogo e entre no servidor enquanto o ApexTweaker coleta RAM, CPU e logs.";
        BenchmarkSummary = "Procurando o processo Java. FPS continua sendo informado por voce.";
        SetCurrentStep(TestStep, EasyStepState.Running);
    }

    public void AddBenchmarkSample(MinecraftBenchmarkSample sample, int sampleCount)
    {
        BenchmarkProgress = Math.Min(100, sampleCount * 100d / 60d);
        BenchmarkSummary = $"{sampleCount}s de 60s | Java {sample.WorkingSetBytes / 1024d / 1024d:0} MB | " +
                          $"RAM livre {sample.AvailableMemoryGb:0.00} GB";
    }

    public void CompleteBenchmark(MinecraftBenchmarkResult? result, bool cancelled)
    {
        BenchmarkProgress = cancelled ? BenchmarkProgress : 100;
        OverallState = cancelled
            ? MinecraftEasyState.Inconclusive
            : result?.Status switch
            {
                BenchmarkStatus.Approved => MinecraftEasyState.Ready,
                BenchmarkStatus.Unstable => MinecraftEasyState.Attention,
                BenchmarkStatus.Failed => MinecraftEasyState.Failed,
                _ => MinecraftEasyState.Inconclusive
            };
        OverallStatus = cancelled ? "Teste interrompido" : "Teste conclu\u00EDdo";
        StatusMessage = cancelled
            ? "O teste foi cancelado. Nenhuma configuracao foi alterada."
            : "Teste conclu\u00EDdo. Escolha Corrigir Problemas, Restaurar Tudo ou Exportar Diagn\u00F3stico.";
        BenchmarkSummary = result is null
            ? "Nao foi possivel medir o processo Java. Responda apenas o que voce observou."
            : $"Pico Java {result.PeakWorkingSetBytes / 1024d / 1024d:0} MB | " +
              $"menor RAM livre {result.MinimumAvailableMemoryGb:0.00} GB | FPS automatico indisponivel";
        NextAction = cancelled
            ? "Teste novamente quando estiver pronto"
            : "Corrigir, restaurar ou exportar diagnostico";
        TestStep.Set(cancelled
            ? EasyStepState.Attention
            : result?.Status switch
            {
                BenchmarkStatus.Approved => EasyStepState.Completed,
                BenchmarkStatus.Failed => EasyStepState.Failed,
                _ => EasyStepState.Attention
            });
        SetCurrentStep(FixStep, EasyStepState.Ready);
    }

    public void SetOperationalResult(OperationalHomologationStatus status)
    {
        OverallState = status switch
        {
            OperationalHomologationStatus.Approved => MinecraftEasyState.Ready,
            OperationalHomologationStatus.Unstable => MinecraftEasyState.Attention,
            OperationalHomologationStatus.Failed => MinecraftEasyState.Failed,
            _ => MinecraftEasyState.Inconclusive
        };
        OverallStatus = status switch
        {
            OperationalHomologationStatus.Approved => "Pronto",
            OperationalHomologationStatus.Unstable => "Precisa de atencao",
            OperationalHomologationStatus.Failed => "Falhou",
            _ => "Inconclusivo"
        };
        StatusMessage = status switch
        {
            OperationalHomologationStatus.Approved => "O jogo abriu e entrou no destino informado. Repita o teste para confirmar.",
            OperationalHomologationStatus.Unstable => "O jogo abriu, mas houve quedas ou desempenho insuficiente.",
            OperationalHomologationStatus.Failed => "O jogo nao completou o teste. Use Corrigir Problemas ou restaure o backup.",
            _ => "N\u00E3o deu para provar que melhorou. Recomendo testar novamente ou restaurar."
        };
        TestStep.Set(status switch
        {
            OperationalHomologationStatus.Approved => EasyStepState.Completed,
            OperationalHomologationStatus.Failed => EasyStepState.Failed,
            _ => EasyStepState.Attention
        });
        SetCurrentStep(FixStep, EasyStepState.Ready);
    }

    public void BeginCorrection() => BeginStep(
        FixStep,
        "Analisando o teste",
        "Procurando o proximo teste seguro sem apagar arquivos.");

    public void SetCorrections(MinecraftEasyCorrectionPlan plan)
    {
        OverallState = plan.State;
        OverallStatus = plan.State == MinecraftEasyState.Ready ? "Nenhuma correcao obrigatoria" : "Proximo teste recomendado";
        StatusMessage = HumanizeMessage(plan.Message);
        CorrectionDetails = string.Join(
            "\n",
            plan.SafeAutomaticSuggestions.Select(item => $"- Opcao segura: {HumanizeMessage(item)}")
                .Concat(plan.ManualActions.Select(item => $"- Acao manual: {HumanizeMessage(item)}"))
                .Concat(plan.SuspectedMods.Select(item => $"- Mod para verificar: {item}")));
        NextAction = plan.State == MinecraftEasyState.Ready
            ? "Repita o teste para confirmar"
            : "Aplique somente uma sugestao por teste ou restaure tudo";
        FixStep.Set(plan.State switch
        {
            MinecraftEasyState.Ready => EasyStepState.Completed,
            MinecraftEasyState.Failed => EasyStepState.Failed,
            _ => EasyStepState.Attention
        }, current: true);
    }

    public void SetRestored(string backupId)
    {
        OptimizationApplied = false;
        HasBackup = false;
        OverallState = MinecraftEasyState.Restored;
        OverallStatus = "Restaurado";
        StatusMessage = "As configuracoes anteriores foram restauradas com seguranca.";
        NextAction = "Analise novamente antes de outra otimizacao";
        OptimizeStep.Set(EasyStepState.NotStarted);
        TestStep.Set(EasyStepState.Blocked);
        FixStep.Set(EasyStepState.Blocked);
        SetCurrentStep(AnalyzeStep, EasyStepState.Ready);
    }

    public void SetDiagnostic(MinecraftDiagnosticPackageResult package)
    {
        OverallStatus = "Diagnostico exportado";
        StatusMessage = $"Diagn\u00F3stico criado com {package.IncludedEntries.Count} arquivo(s). " +
                        "O pacote est\u00E1 pronto para compartilhar com quem vai analisar o problema.";
        NextAction = "Diagn\u00F3stico pronto";
    }

    public MinecraftOperationalObservation BuildObservation()
    {
        var fps = double.TryParse(
            ApproximateFps.Replace(',', '.'),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsedFps)
            ? parsedFps
            : (double?)null;
        var notes = string.Join(
            " ",
            new[]
            {
                UserNotes.Trim(),
                ModError ? "Usuario informou erro de mod." : string.Empty
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return new MinecraftOperationalObservation(
            GameOpened,
            MenuReached,
            null,
            false,
            ServerEntered,
            null,
            GameOpened && ServerEntered && !SevereStutter && !ClosedAlone,
            fps,
            null,
            SevereStutter,
            ClosedAlone,
            false,
            notes);
    }

    public void SetBusy(bool value)
    {
        IsBusy = value;
    }

    public void EndPendingAction(EasyStepViewModel step)
    {
        if (step.State != EasyStepState.Running)
        {
            return;
        }

        SetCurrentStep(step, EasyStepState.Ready);
        OverallState = MinecraftEasyState.Attention;
        OverallStatus = "Acao nao concluida";
        StatusMessage = "A acao nao foi concluida. Tente novamente quando estiver pronto.";
    }

    private static string Flag(string label, bool available) => $"{label}: {(available ? "OK" : "falta")}";

    private static string BuildSummary(MinecraftEasyModSummary summary)
    {
        var lines = new List<string>();
        AddLine(lines, "Essenciais", summary.EssentialNames);
        AddLine(lines, "Performance", summary.PerformanceNames);
        AddLine(lines, "Visuais pesados", summary.HeavyVisualNames);
        AddLine(lines, "Duplicatas", summary.DuplicateNames);
        AddLine(lines, "Riscos", summary.RiskMessages);
        return lines.Count == 0 ? "Nenhum detalhe adicional." : string.Join("\n", lines);
    }

    private static void AddLine(ICollection<string> lines, string label, IReadOnlyList<string> values)
    {
        if (values.Count > 0)
        {
            lines.Add($"{label}: {string.Join(", ", values.Select(HumanizeMessage))}");
        }
    }

    private void BeginStep(EasyStepViewModel step, string status, string message)
    {
        SetCurrentStep(step, EasyStepState.Running);
        OverallState = MinecraftEasyState.TestRequired;
        OverallStatus = status;
        StatusMessage = message;
    }

    private void SetCurrentStep(EasyStepViewModel step, EasyStepState state)
    {
        foreach (var item in AllSteps())
        {
            item.IsCurrent = false;
        }

        step.Set(state, current: true);
    }

    private IEnumerable<EasyStepViewModel> AllSteps()
    {
        yield return DetectStep;
        yield return AnalyzeStep;
        yield return OptimizeStep;
        yield return ServerStep;
        yield return TestStep;
        yield return FixStep;
    }

    private void ResetAnswers()
    {
        GameOpened = false;
        MenuReached = false;
        ServerEntered = false;
        SevereStutter = false;
        ClosedAlone = false;
        ModError = false;
        ApproximateFps = string.Empty;
        UserNotes = string.Empty;
        BenchmarkProgress = 0;
        BenchmarkSummary = "Teste ainda nao iniciado.";
        ServerDetails = "A compatibilidade com o servidor ainda nao foi verificada.";
        CorrectionDetails = "Execute o teste antes de procurar problemas.";
    }

    private static string HumanizeMessage(string value) => value
        .Replace("Candidato inconclusivo", "N\u00E3o deu para provar que melhorou. Recomendo testar novamente ou restaurar.", StringComparison.OrdinalIgnoreCase)
        .Replace("INSUFFICIENT_DATA", "N\u00E3o deu para provar que melhorou. Recomendo testar novamente ou restaurar.", StringComparison.OrdinalIgnoreCase)
        .Replace("SERVER_MOD_MISMATCH", "O servidor recusou por mod ausente ou vers\u00E3o diferente.", StringComparison.OrdinalIgnoreCase)
        .Replace("PAGEFILE_PRESSURE", "O Windows est\u00E1 usando muita mem\u00F3ria virtual. Isso pode causar travamentos.", StringComparison.OrdinalIgnoreCase);

}
