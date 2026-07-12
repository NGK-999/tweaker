using System.IO;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftScientificExperimentService
{
    private readonly MinecraftScientificExperimentStore store;
    private readonly MinecraftProfileService profileService;
    private readonly MinecraftScientificAutoOptimizeService autoOptimizeService;
    private readonly MinecraftScientificMetricsService metricsService = new();
    private readonly MinecraftScientificComparisonService comparisonService = new();
    private readonly MinecraftBottleneckDiagnosticService diagnosticService = new();
    private readonly MinecraftInstanceEvidenceService evidenceService = new();
    private readonly MinecraftScientificReportService reportService = new();

    public MinecraftScientificExperimentService(
        string? experimentRoot = null,
        string? profileBackupRoot = null,
        string? profileReportRoot = null)
    {
        store = new MinecraftScientificExperimentStore(experimentRoot);
        profileService = new MinecraftProfileService(profileBackupRoot, profileReportRoot);
        autoOptimizeService = new MinecraftScientificAutoOptimizeService(profileService);
    }

    public string ExperimentRoot => store.Root;

    public (MinecraftScientificOptimizationPlan Plan, MinecraftScientificReportPaths Reports) Plan(
        string selectedPath,
        int? maximumFps = null,
        string? outputDirectory = null)
    {
        var plan = autoOptimizeService.BuildPlan(selectedPath, maximumFps);
        return (plan, reportService.WritePlan(plan, outputDirectory));
    }

    public (MinecraftScientificOptimizationPlan Plan, MinecraftScientificReportPaths Reports) PlanCustom(
        string selectedPath,
        string experimentId,
        string? outputDirectory = null)
    {
        var plan = autoOptimizeService.BuildCustomPlan(selectedPath, experimentId);
        return (plan, reportService.WritePlan(plan, outputDirectory));
    }

    public MinecraftScientificOperationResult StartGuided(
        string selectedPath,
        int? maximumFps = null)
    {
        var plan = autoOptimizeService.BuildPlan(selectedPath, maximumFps);
        return StartWithPlan(plan);
    }

    public MinecraftScientificOperationResult StartCustom(
        string selectedPath,
        string experimentId)
    {
        var plan = autoOptimizeService.BuildCustomPlan(selectedPath, experimentId);
        var definition = plan.ProfilePlan.Experiment
            ?? throw new InvalidOperationException("Experimento customizado sem definicao persistida.");
        var hypothesis = new ScientificHypothesis(
            ScientificHypothesisKind.Custom,
            $"{definition.DisplayName}: {definition.ExpectedEffect}",
            ExpectedMetrics(definition.Variable),
            definition.Description,
            definition.Variable is MinecraftExperimentVariable.ResourcePacks or MinecraftExperimentVariable.JavaHeap
                ? ScientificActionRisk.Medium
                : ScientificActionRisk.Low,
            ManualChangeRequired: false);
        return StartWithPlan(plan, hypothesis);
    }

    internal MinecraftScientificOperationResult StartWithPlan(
        MinecraftScientificOptimizationPlan plan,
        ScientificHypothesis? hypothesis = null)
    {
        var experimentId = store.CreateId();
        var now = DateTimeOffset.UtcNow;
        var experiment = new MinecraftScientificExperiment(
            experimentId,
            now,
            now,
            plan.InstanceRoot,
            ScientificExperimentPhase.BaselinePending,
            hypothesis ?? BuildHypothesis(plan),
            plan,
            AppliedProfileBackupId: null,
            Baseline: null,
            Candidate: null,
            Comparison: null,
            DiagnosisAfter: null,
            [Audit(now, "Experimento criado em dry-run; nenhum arquivo foi alterado.")]);
        return Persist(
            experiment,
            [
                "Execute o jogo no estado atual e registre o baseline.",
                "Nao aplique o candidato antes do baseline.",
                $"Relatorios e manifesto: {store.GetExperimentDirectory(experimentId)}"
            ]);
    }

    public MinecraftScientificExperiment Load(string experimentId) => store.Load(experimentId);

    public IReadOnlyList<MinecraftScientificExperiment> List() => store.List();

    public MinecraftScientificOperationResult RecordMeasurement(
        string experimentId,
        ScientificMeasurementKind kind,
        MinecraftOperationalObservation observation,
        MinecraftBenchmarkResult? benchmark)
    {
        var experiment = store.Load(experimentId);
        ValidateBenchmarkInstance(experiment, benchmark);
        if (kind == ScientificMeasurementKind.Baseline &&
            experiment.Phase != ScientificExperimentPhase.BaselinePending)
        {
            throw new InvalidOperationException($"Baseline nao pode ser registrado na fase {experiment.Phase}.");
        }

        if (kind == ScientificMeasurementKind.Candidate &&
            experiment.Phase != ScientificExperimentPhase.CandidateApplied)
        {
            throw new InvalidOperationException($"Candidato nao pode ser registrado na fase {experiment.Phase}.");
        }

        var metrics = metricsService.Build(observation, benchmark);
        if (metrics.Outcome == ScientificBenchmarkOutcome.NotTested)
        {
            throw new InvalidOperationException("Medicao vazia: execute o jogo/benchmark ou registre observacoes reais.");
        }

        var now = DateTimeOffset.UtcNow;
        var measurement = new MinecraftExperimentMeasurement(
            $"measure-{now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}",
            kind,
            now,
            observation,
            benchmark,
            metrics,
            evidenceService.Capture(experiment.InstanceRoot),
            observation.Notes);
        MinecraftScientificExperiment updated;
        if (kind == ScientificMeasurementKind.Baseline)
        {
            var baselinePlan = experiment.OptimizationPlan.ProfilePlan.Experiment is { } customExperiment
                ? autoOptimizeService.BuildCustomPlan(experiment.InstanceRoot, customExperiment.Id)
                : autoOptimizeService.BuildPlan(
                    experiment.InstanceRoot,
                    experiment.OptimizationPlan.MaximumFps);
            updated = experiment with
            {
                UpdatedAtUtc = now,
                Phase = ScientificExperimentPhase.BaselineRecorded,
                OptimizationPlan = baselinePlan,
                Hypothesis = experiment.Hypothesis.Kind == ScientificHypothesisKind.Custom
                    ? experiment.Hypothesis
                    : BuildHypothesis(baselinePlan),
                Baseline = measurement,
                AuditTrail = Append(
                    experiment.AuditTrail,
                    Audit(now, $"Baseline registrado e plano congelado: {metrics.Outcome}."))
            };
        }
        else
        {
            EnsureModSetUnchanged(experiment.Baseline!, measurement.InstanceEvidence, "candidato");
            updated = experiment with
            {
                UpdatedAtUtc = now,
                Phase = ScientificExperimentPhase.CandidateRecorded,
                Candidate = measurement,
                AuditTrail = Append(experiment.AuditTrail, Audit(now, $"Candidato registrado: {metrics.Outcome}."))
            };
        }

        return Persist(updated, [$"Medicao {kind} persistida com hashes e evidencias."]);
    }

    public MinecraftScientificOperationResult ApplyCandidate(
        string experimentId,
        bool userConfirmed)
    {
        if (!userConfirmed)
        {
            throw new InvalidOperationException("Apply cientifico bloqueado: falta confirmacao explicita.");
        }

        var experiment = store.Load(experimentId);
        if (experiment.Phase != ScientificExperimentPhase.BaselineRecorded || experiment.Baseline is null)
        {
            throw new InvalidOperationException("Registre o baseline antes de aplicar o candidato.");
        }

        if (experiment.OptimizationPlan.HasCriticalBlockers)
        {
            throw new InvalidOperationException("Apply bloqueado por conflito estrutural de mods. Resolva a auditoria e inicie outro experimento.");
        }

        var beforeApply = evidenceService.Capture(experiment.InstanceRoot);
        EnsureModSetUnchanged(experiment.Baseline, beforeApply, "pre-apply");
        EnsureConfigSetUnchanged(experiment.Baseline, beforeApply, "pre-apply");
        var frozenPlan = experiment.OptimizationPlan.ProfilePlan;
        var result = profileService.ApplyVerifiedProfile(frozenPlan);
        var now = DateTimeOffset.UtcNow;
        var updated = experiment with
        {
            UpdatedAtUtc = now,
            Phase = ScientificExperimentPhase.CandidateApplied,
            AppliedProfileBackupId = string.IsNullOrWhiteSpace(result.BackupId) ? null : result.BackupId,
            AuditTrail = Append(
                experiment.AuditTrail,
                Audit(now, string.IsNullOrWhiteSpace(result.BackupId)
                    ? "Candidato ja estava aplicado; nenhuma escrita foi necessaria."
                    : $"Candidato aplicado com backup exato {result.BackupId}."))
        };
        return Persist(
            updated,
            [
                "Feche qualquer tela de configuracao, abra o jogo e repita a mesma cena.",
                "Execute um novo benchmark antes de registrar o candidato."
            ]);
    }

    public MinecraftScientificOperationResult Compare(string experimentId)
    {
        var experiment = store.Load(experimentId);
        if (experiment.Phase != ScientificExperimentPhase.CandidateRecorded ||
            experiment.Baseline is null ||
            experiment.Candidate is null)
        {
            throw new InvalidOperationException("Comparacao exige baseline e candidato registrados.");
        }

        var comparison = comparisonService.Compare(experiment.Baseline, experiment.Candidate);
        var expectedWrites = experiment.OptimizationPlan.ProfilePlan.Changes.Count(change => change.WillWrite);
        var changedConfigs = GetChangedHashPaths(
            experiment.Baseline.InstanceEvidence.ConfigHashes,
            experiment.Candidate.InstanceEvidence.ConfigHashes);
        var managedConfigs = GetManagedConfigPaths(experiment);
        var changedManagedConfigs = changedConfigs.Count(managedConfigs.Contains);
        var unexpectedConfigChanges = changedConfigs
            .Where(path => !managedConfigs.Contains(path))
            .ToArray();
        if (expectedWrites == 0 && !experiment.Hypothesis.ManualChangeRequired)
        {
            comparison = comparison with
            {
                Decision = ScientificDecision.InsufficientData,
                Confidence = ScientificConfidence.Low,
                Rationale = Append(
                    comparison.Rationale,
                    "O plano recalculado nao escreveu configuracoes; nao existe variavel independente confirmada.")
            };
        }
        else if (expectedWrites > 0 && changedManagedConfigs == 0)
        {
            comparison = comparison with
            {
                Decision = ScientificDecision.Retest,
                Confidence = ScientificConfidence.Low,
                Rationale = Append(
                    comparison.Rationale,
                    "O plano previa escrita, mas nenhum hash de config mudou entre baseline e candidato.")
            };
        }

        if (unexpectedConfigChanges.Length > 0)
        {
            comparison = comparison with
            {
                Decision = comparison.Decision == ScientificDecision.Revert
                    ? ScientificDecision.Revert
                    : ScientificDecision.Retest,
                Confidence = ScientificConfidence.Low,
                Rationale = Append(
                    comparison.Rationale,
                    $"Mudancas fora da hipotese contaminaram a rodada: {string.Join(", ", unexpectedConfigChanges)}.")
            };
        }

        var diagnosisAfter = diagnosticService.Diagnose(
            experiment.OptimizationPlan.Audit,
            experiment.Candidate.Metrics,
            experiment.OptimizationPlan.ProfilePlan);
        var now = DateTimeOffset.UtcNow;
        var updated = experiment with
        {
            UpdatedAtUtc = now,
            Phase = ScientificExperimentPhase.Compared,
            Comparison = comparison,
            DiagnosisAfter = diagnosisAfter,
            AuditTrail = Append(
                experiment.AuditTrail,
                Audit(now, $"Comparacao concluida: {comparison.Decision}, score={comparison.Score}, confianca={comparison.Confidence}."))
        };
        return Persist(updated, [$"Decisao cientifica: {comparison.Decision}."]);
    }

    public MinecraftScientificOperationResult Finalize(
        string experimentId,
        bool rollbackConfirmed)
    {
        var experiment = store.Load(experimentId);
        if (experiment.Phase != ScientificExperimentPhase.Compared || experiment.Comparison is null)
        {
            throw new InvalidOperationException("Finalize somente depois da comparacao.");
        }

        var now = DateTimeOffset.UtcNow;
        var phase = experiment.Comparison.Decision switch
        {
            ScientificDecision.Keep => ScientificExperimentPhase.Kept,
            ScientificDecision.Revert => ScientificExperimentPhase.Reverted,
            _ => ScientificExperimentPhase.NeedsRetest
        };
        var auditTrail = experiment.AuditTrail;
        var messages = new List<string>();
        if (experiment.Comparison.Decision == ScientificDecision.Revert &&
            string.IsNullOrWhiteSpace(experiment.AppliedProfileBackupId))
        {
            phase = ScientificExperimentPhase.Failed;
            auditTrail = Append(auditTrail, Audit(now, "Rollback solicitado, mas o experimento nao possui backup aplicado."));
            var failed = experiment with
            {
                UpdatedAtUtc = now,
                Phase = phase,
                AuditTrail = auditTrail
            };
            Persist(failed, ["Rollback indisponivel; nenhuma exclusao foi executada."]);
            throw new InvalidOperationException("O candidato nao possui backup de perfil para rollback automatico.");
        }

        var shouldRollback = experiment.Comparison.Decision != ScientificDecision.Keep &&
                             !string.IsNullOrWhiteSpace(experiment.AppliedProfileBackupId);
        if (shouldRollback)
        {
            if (!rollbackConfirmed)
            {
                throw new InvalidOperationException(
                    "Somente KEEP pode permanecer aplicado; confirme o rollback explicito do candidato inconclusivo ou regressivo.");
            }

            var backupId = experiment.AppliedProfileBackupId
                ?? throw new InvalidOperationException("Backup do candidato desapareceu antes do rollback.");
            var rollback = profileService.RollbackBackup(experiment.InstanceRoot, backupId);
            var restoredEvidence = evidenceService.Capture(experiment.InstanceRoot);
            EnsureManagedConfigsRestored(experiment, restoredEvidence);
            auditTrail = Append(
                auditTrail,
                Audit(
                    now,
                    $"Rollback direcionado {rollback.BackupId} concluido para {experiment.Comparison.Decision}; hashes gerenciados conferidos."));
            messages.Add($"Rollback aplicado: {rollback.BackupId}.");
        }
        else if (experiment.Comparison.Decision == ScientificDecision.Keep)
        {
            auditTrail = Append(auditTrail, Audit(now, "Candidato mantido por decisao comparativa; nenhum arquivo adicional foi escrito."));
            messages.Add("Candidato mantido. Preserve esta rodada como evidencia.");
        }
        else
        {
            auditTrail = Append(
                auditTrail,
                Audit(now, "Resultado inconclusivo sem escrita gerenciada; nova rodada obrigatoria."));
            messages.Add("Resultado inconclusivo sem backup aplicado. Repita o experimento antes de promover a mudanca.");
        }

        var updated = experiment with
        {
            UpdatedAtUtc = now,
            Phase = phase,
            AuditTrail = auditTrail
        };
        return Persist(updated, messages);
    }

    public MinecraftScientificOperationResult Cancel(
        string experimentId,
        bool rollbackConfirmed)
    {
        var experiment = store.Load(experimentId);
        if (experiment.Phase is ScientificExperimentPhase.Kept or ScientificExperimentPhase.Reverted)
        {
            throw new InvalidOperationException($"Experimento ja finalizado como {experiment.Phase}.");
        }

        var now = DateTimeOffset.UtcNow;
        var auditTrail = experiment.AuditTrail;
        var messages = new List<string>();
        var phase = ScientificExperimentPhase.NeedsRetest;
        if (!string.IsNullOrWhiteSpace(experiment.AppliedProfileBackupId))
        {
            if (!rollbackConfirmed)
            {
                throw new InvalidOperationException("Confirme o rollback para cancelar um candidato ja aplicado.");
            }

            var rollback = profileService.RollbackBackup(
                experiment.InstanceRoot,
                experiment.AppliedProfileBackupId);
            if (experiment.Baseline is not null)
            {
                EnsureManagedConfigsRestored(experiment, evidenceService.Capture(experiment.InstanceRoot));
            }

            phase = ScientificExperimentPhase.Reverted;
            auditTrail = Append(
                auditTrail,
                Audit(now, $"Experimento cancelado; rollback exato {rollback.BackupId} concluido e conferido."));
            messages.Add($"Cancelamento seguro: backup {rollback.BackupId} restaurado.");
        }
        else
        {
            auditTrail = Append(
                auditTrail,
                Audit(now, "Experimento cancelado antes do apply; nenhum arquivo precisou de rollback."));
            messages.Add("Experimento cancelado sem escrita gerenciada.");
        }

        return Persist(
            experiment with
            {
                UpdatedAtUtc = now,
                Phase = phase,
                AuditTrail = auditTrail
            },
            messages);
    }

    private MinecraftScientificOperationResult Persist(
        MinecraftScientificExperiment experiment,
        IReadOnlyList<string> messages)
    {
        store.Save(experiment);
        var reports = reportService.WriteExperiment(
            experiment,
            store.GetExperimentDirectory(experiment.ExperimentId));
        return new MinecraftScientificOperationResult(experiment, reports, messages);
    }

    private static ScientificHypothesis BuildHypothesis(MinecraftScientificOptimizationPlan plan)
    {
        if (plan.ProfilePlan.Experiment is { } experiment)
        {
            return new ScientificHypothesis(
                ScientificHypothesisKind.Custom,
                $"{experiment.DisplayName}: {experiment.ExpectedEffect}",
                ExpectedMetrics(experiment.Variable),
                experiment.Description,
                experiment.Variable is MinecraftExperimentVariable.ResourcePacks or MinecraftExperimentVariable.JavaHeap
                    ? ScientificActionRisk.Medium
                    : ScientificActionRisk.Low,
                ManualChangeRequired: false);
        }

        return plan.SelectedProfile switch
        {
            MinecraftProfileKind.RamLimited => new ScientificHypothesis(
                ScientificHypothesisKind.RamPressureReduction,
                "Reduzir heap, distancia e FPS diminui pressao de RAM/pagefile sem impedir entrada no servidor.",
                ["Pico RAM Java", "Menor RAM livre", "Delta pagefile", "FPS minimo", "Entrada no servidor"],
                $"Aplicar RAM_LIMITED com {plan.JavaMemory.Arguments} e {plan.MaximumFps} FPS.",
                ScientificActionRisk.Low,
                ManualChangeRequired: false),
            MinecraftProfileKind.CpuLimited => new ScientificHypothesis(
                ScientificHypothesisKind.CpuLoadReduction,
                "Limitar simulacao, entidades e FPS reduz CPU media/pico e stutter.",
                ["CPU media", "CPU pico", "FPS minimo", "Quedas severas"],
                $"Aplicar CPU_LIMITED com {plan.MaximumFps} FPS.",
                ScientificActionRisk.Low,
                ManualChangeRequired: false),
            MinecraftProfileKind.GpuLimited => new ScientificHypothesis(
                ScientificHypothesisKind.GpuLoadReduction,
                "Reduzir resolucao, efeitos e distancia melhora FPS minimo na GPU integrada.",
                ["FPS medio", "FPS minimo", "Quedas severas", "Jogavel em 720p"],
                $"Aplicar GPU_LIMITED com {plan.MaximumFps} FPS e shaders desligados.",
                ScientificActionRisk.Low,
                ManualChangeRequired: false),
            MinecraftProfileKind.ServerEntryCompatible => new ScientificHypothesis(
                ScientificHypothesisKind.ConservativeProfile,
                "Configuracoes client-side conservadoras reduzem carga sem alterar o conjunto exigido pelo servidor.",
                ["Entrada no servidor", "Tempo de entrada", "Crash", "FPS minimo"],
                "Aplicar SERVER_ENTRY_COMPATIBLE sem movimentar mods.",
                ScientificActionRisk.Low,
                ManualChangeRequired: false),
            _ => new ScientificHypothesis(
                ScientificHypothesisKind.ConservativeProfile,
                "O perfil selecionado melhora estabilidade sem regressao critica.",
                ["Entrada no servidor", "FPS minimo", "RAM", "CPU", "Crash"],
                $"Aplicar {plan.SelectedProfile} e comparar na mesma cena.",
                ScientificActionRisk.Low,
                ManualChangeRequired: false)
        };
    }

    private static IReadOnlyList<string> ExpectedMetrics(MinecraftExperimentVariable variable)
    {
        return variable switch
        {
            MinecraftExperimentVariable.JavaHeap =>
                ["Pico RAM Java", "Menor RAM livre", "Delta pagefile", "OOM", "Stutter manual"],
            MinecraftExperimentVariable.FpsCap or MinecraftExperimentVariable.RenderDistance or
                MinecraftExperimentVariable.SimulationDistance =>
                ["CPU media", "CPU pico", "FPS minimo manual", "Quedas severas", "Entrada no servidor"],
            MinecraftExperimentVariable.Resolution or MinecraftExperimentVariable.WindowMode or
                MinecraftExperimentVariable.VisualQuality or MinecraftExperimentVariable.EntityDistance =>
                ["FPS medio manual", "FPS minimo manual", "CPU media", "RAM Java", "Artefatos visuais"],
            MinecraftExperimentVariable.ResourcePacks =>
                ["Tempo ate menu", "Pico RAM Java", "Menor RAM livre", "Entrada no servidor"],
            _ => ["RAM", "CPU", "FPS manual", "Crash"]
        };
    }

    private static void ValidateBenchmarkInstance(
        MinecraftScientificExperiment experiment,
        MinecraftBenchmarkResult? benchmark)
    {
        if (benchmark?.InstanceRoot is null)
        {
            return;
        }

        if (!string.Equals(
                Path.GetFullPath(benchmark.InstanceRoot),
                Path.GetFullPath(experiment.InstanceRoot),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("O benchmark pertence a outra instancia.");
        }
    }

    private static void EnsureModSetUnchanged(
        MinecraftExperimentMeasurement baseline,
        MinecraftInstanceEvidence current,
        string phase)
    {
        if (!DictionariesEqual(baseline.InstanceEvidence.ModHashes, current.ModHashes))
        {
            throw new InvalidOperationException(
                $"Conjunto/hash de mods mudou antes da fase {phase}. Inicie um experimento separado para essa hipotese.");
        }
    }

    private static void EnsureConfigSetUnchanged(
        MinecraftExperimentMeasurement baseline,
        MinecraftInstanceEvidence current,
        string phase)
    {
        if (!DictionariesEqual(baseline.InstanceEvidence.ConfigHashes, current.ConfigHashes))
        {
            throw new InvalidOperationException(
                $"Configs mudaram depois do baseline e antes da fase {phase}. Reinicie o experimento para preservar a variavel independente.");
        }
    }

    private static void EnsureManagedConfigsRestored(
        MinecraftScientificExperiment experiment,
        MinecraftInstanceEvidence restored)
    {
        var baseline = experiment.Baseline
            ?? throw new InvalidOperationException("Baseline ausente durante verificacao de rollback.");
        var managed = GetManagedConfigPaths(experiment);
        foreach (var relativePath in managed)
        {
            baseline.InstanceEvidence.ConfigHashes.TryGetValue(relativePath, out var expected);
            restored.ConfigHashes.TryGetValue(relativePath, out var actual);
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Rollback nao reproduziu o hash baseline de {relativePath}.");
            }
        }
    }

    private static bool DictionariesEqual(
        IReadOnlyDictionary<string, string> left,
        IReadOnlyDictionary<string, string> right)
    {
        return left.Count == right.Count && left.All(item =>
            right.TryGetValue(item.Key, out var value) &&
            string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> GetManagedConfigPaths(MinecraftScientificExperiment experiment)
    {
        return experiment.OptimizationPlan.ProfilePlan.Changes
            .Where(change => change.WillWrite)
            .Select(change => Path.GetRelativePath(experiment.InstanceRoot, change.FilePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetChangedHashPaths(
        IReadOnlyDictionary<string, string> baseline,
        IReadOnlyDictionary<string, string> candidate)
    {
        return baseline.Keys
            .Union(candidate.Keys, StringComparer.OrdinalIgnoreCase)
            .Where(path =>
            {
                var hasBaseline = baseline.TryGetValue(path, out var before);
                var hasCandidate = candidate.TryGetValue(path, out var after);
                return !hasBaseline || !hasCandidate ||
                       !string.Equals(before, after, StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Audit(DateTimeOffset timestamp, string message) => $"{timestamp:O} | {message}";

    private static IReadOnlyList<string> Append(IReadOnlyList<string> source, string value) => [.. source, value];
}
