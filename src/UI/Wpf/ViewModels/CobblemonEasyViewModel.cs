using ApexTweaker.Minecraft.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApexTweaker.UI.Wpf.ViewModels;

internal sealed partial class CobblemonEasyViewModel : ObservableObject
{
    [ObservableProperty]
    private string selectedPath = "Nenhuma instancia selecionada.";

    [ObservableProperty]
    private MinecraftEasyState overallState = MinecraftEasyState.TestRequired;

    [ObservableProperty]
    private string overallStatus = "Comece detectando a instancia";

    [ObservableProperty]
    private string statusMessage = "O ApexTweaker vai procurar o Minecraft e validar os arquivos principais.";

    [ObservableProperty]
    private string nextAction = "Proximo passo: Detectar Instancia";

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
    private bool isTestPanelVisible;

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

    public string StateColor => OverallState switch
    {
        MinecraftEasyState.Ready or MinecraftEasyState.BackupCreated or
            MinecraftEasyState.OptimizationApplied or MinecraftEasyState.Restored => "#47D18C",
        MinecraftEasyState.TooHeavy or MinecraftEasyState.ServerMayReject or MinecraftEasyState.Failed => "#FF756B",
        MinecraftEasyState.Attention or MinecraftEasyState.Inconclusive => "#F5B942",
        _ => "#4DA3FF"
    };

    partial void OnOverallStateChanged(MinecraftEasyState value) => OnPropertyChanged(nameof(StateColor));

    public void ResetForPath(string path, bool resolved)
    {
        SelectedPath = string.IsNullOrWhiteSpace(path) ? "Nenhuma instancia selecionada." : path;
        InstanceReady = resolved;
        AuditReady = false;
        OptimizationApplied = false;
        EssentialMods = PerformanceMods = HeavyMods = DuplicateMods = RiskCount = "--";
        ModDetails = "Clique em Analisar Mods para gerar o resumo.";
        OverallState = resolved ? MinecraftEasyState.Attention : MinecraftEasyState.TestRequired;
        OverallStatus = resolved ? "Instancia encontrada" : "Detecte a instancia";
        StatusMessage = resolved
            ? "A pasta parece valida. Confirme Java, config e logs em Detectar Instancia."
            : "Selecione uma instancia com options.txt e pasta mods.";
        NextAction = "Proximo passo: Detectar Instancia";
        InstanceDetails = resolved ? "Validacao basica concluida." : "Ainda nao verificado.";
    }

    public void SetInstance(MinecraftEasyInstanceStatus result)
    {
        OverallState = result.State;
        OverallStatus = result.Status;
        StatusMessage = result.Message;
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
        NextAction = InstanceReady ? "Proximo passo: Analisar Mods" : "Selecione ou complete a instancia";
    }

    public void SetAudit(MinecraftEasyModSummary summary)
    {
        AuditReady = true;
        OverallState = summary.State;
        OverallStatus = summary.Status;
        StatusMessage = summary.Risks == 0
            ? "Os mods foram analisados sem alterar nenhum JAR."
            : "Pode existir conflito ou peso excessivo. Os mods permanecem intactos.";
        EssentialMods = summary.EssentialMods.ToString();
        PerformanceMods = summary.PerformanceMods.ToString();
        HeavyMods = summary.HeavyVisualMods.ToString();
        DuplicateMods = summary.DuplicateModIds.ToString();
        RiskCount = summary.Risks.ToString();
        ModDetails = BuildSummary(summary);
        NextAction = InstanceReady ? "Proximo passo: Otimizar para PC Fraco" : "Selecione uma instancia completa";
    }

    public void SetOptimizationApplied(string backupId, string javaArguments, bool javaAppliedAutomatically)
    {
        OptimizationApplied = true;
        OverallState = MinecraftEasyState.OptimizationApplied;
        OverallStatus = "Otimizacao aplicada";
        StatusMessage = string.IsNullOrWhiteSpace(backupId)
            ? "A instancia ja estava otimizada; nenhuma escrita foi necessaria."
            : $"Backup criado: {backupId}. Mods nao foram alterados.";
        NextAction = "Proximo passo: Testar Jogo";
        BenchmarkSummary = javaAppliedAutomatically
            ? $"Memoria da instancia configurada automaticamente: {javaArguments}"
            : $"Acao manual: configure no launcher {javaArguments}";
    }

    public void SetServerReadiness(MinecraftEasyServerReadiness readiness)
    {
        OverallState = readiness.State;
        OverallStatus = readiness.Status;
        StatusMessage = readiness.Message;
        ServerDetails = string.Join("\n", readiness.Checklist.Concat(readiness.Warnings).Select(item => $"- {item}"));
        NextAction = readiness.State == MinecraftEasyState.Ready
            ? "Proximo passo: Testar Jogo"
            : "Confirme a lista de mods do servidor antes do teste";
    }

    public void BeginBenchmark()
    {
        IsTestPanelVisible = true;
        BenchmarkProgress = 0;
        OverallState = MinecraftEasyState.TestRequired;
        OverallStatus = "Teste em andamento";
        StatusMessage = "Abra o jogo e entre no servidor enquanto o ApexTweaker coleta RAM, CPU e logs.";
        BenchmarkSummary = "Procurando o processo Java. FPS continua sendo informado por voce.";
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
        OverallStatus = cancelled ? "Inconclusivo" : MapBenchmarkStatus(result?.Status);
        StatusMessage = cancelled
            ? "O teste foi cancelado. Nenhuma configuracao foi alterada."
            : "Responda as perguntas abaixo para completar o diagnostico. FPS nao foi inventado.";
        BenchmarkSummary = result is null
            ? "Nao foi possivel medir o processo Java. Responda apenas o que voce observou."
            : $"Pico Java {result.PeakWorkingSetBytes / 1024d / 1024d:0} MB | " +
              $"menor RAM livre {result.MinimumAvailableMemoryGb:0.00} GB | FPS automatico indisponivel";
        NextAction = "Responda o teste e use Corrigir Problemas ou Restaurar Tudo";
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
            _ => "Nao deu para provar que melhorou. Teste novamente ou restaure."
        };
    }

    public void SetCorrections(MinecraftEasyCorrectionPlan plan)
    {
        OverallState = plan.State;
        OverallStatus = plan.Status;
        StatusMessage = plan.Message;
        CorrectionDetails = string.Join(
            "\n",
            plan.SafeAutomaticSuggestions.Select(item => $"- Opcao segura: {item}")
                .Concat(plan.ManualActions.Select(item => $"- Manual: {item}"))
                .Concat(plan.SuspectedMods.Select(item => $"- Mod suspeito: {item}")));
        NextAction = plan.State == MinecraftEasyState.Ready
            ? "Repita o teste para confirmar"
            : "Aplique somente uma sugestao por teste ou restaure tudo";
    }

    public void SetRestored(string backupId)
    {
        OptimizationApplied = false;
        OverallState = MinecraftEasyState.Restored;
        OverallStatus = "Restaurado";
        StatusMessage = $"O backup {backupId} foi restaurado e os hashes foram conferidos.";
        NextAction = "Analise novamente antes de outra otimizacao";
    }

    public void SetDiagnostic(MinecraftDiagnosticPackageResult package)
    {
        OverallStatus = "Diagnostico exportado";
        StatusMessage = $"Pacote criado com {package.IncludedEntries.Count} arquivo(s). SHA-256 {package.Sha256[..12]}...";
        NextAction = package.ZipPath;
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
            lines.Add($"{label}: {string.Join(", ", values)}");
        }
    }

    private static string MapBenchmarkStatus(BenchmarkStatus? status) => status switch
    {
        BenchmarkStatus.Approved => "Pronto",
        BenchmarkStatus.Unstable => "Precisa de atencao",
        BenchmarkStatus.Failed => "Falhou",
        _ => "Inconclusivo"
    };
}
