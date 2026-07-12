using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApexTweaker.Minecraft.Models;
using ApexTweaker.Minecraft.Services;
using ApexTweaker.UI.Wpf.Views;

namespace ApexTweaker.Minecraft;

internal static class MinecraftSelfTest
{
    public static IReadOnlyList<string> Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "ApexTweaker-SelfTest-" + Guid.NewGuid().ToString("N"));
        var messages = new List<string>();

        try
        {
            var modsDirectory = Path.Combine(root, "audit", "mods");
            Directory.CreateDirectory(modsDirectory);
            CreateFabricJar(Path.Combine(modsDirectory, "sample-1.0.jar"), "sample", "1.0.0", new Dictionary<string, string>());
            CreateFabricJar(Path.Combine(modsDirectory, "sample-2.0.jar"), "sample", "2.0.0", new Dictionary<string, string>());
            CreateFabricJar(
                Path.Combine(modsDirectory, "broken.jar"),
                "broken",
                "1.0.0",
                new Dictionary<string, string> { ["missing-library"] = ">=1.0" });

            var audit = new MinecraftAuditService().Audit(modsDirectory);
            Assert(audit.Summary.DuplicateModIds == 1, "O scanner nao detectou o ID duplicado.");
            Assert(audit.Summary.MissingDependencies == 1, "O scanner nao detectou a dependencia ausente.");
            Assert(audit.Mods.Where(mod => mod.Id == "sample").All(mod => mod.ClassificationTags.Contains(ModClassification.Duplicado)),
                "A auditoria nao adicionou a tag DUPLICADO a todas as versoes do mesmo ID.");
            messages.Add("PASS: scanner detecta duplicidade e dependencia ausente.");

            var reportDirectory = Path.Combine(root, "reports");
            var report = new MinecraftReportService().WriteAudit(audit, reportDirectory);
            Assert(File.Exists(report.JsonPath) && File.Exists(report.MarkdownPath) && File.Exists(report.TextPath),
                "Os tres formatos de relatorio nao foram gerados.");
            Assert(File.Exists(Path.Combine(report.QuarantineSuggestionsDirectory, "quarantine-plan.json")),
                "O plano de quarentena nao foi gerado.");
            messages.Add("PASS: relatorios JSON, Markdown e TXT.");

            var quarantineService = new MinecraftQuarantineService(Path.Combine(root, "quarantine-backups"));
            var quarantinePlan = quarantineService.BuildPlan(audit);
            var olderDuplicate = quarantinePlan.Candidates.Single(candidate => candidate.FileName == "sample-1.0.jar");
            Assert(File.Exists(olderDuplicate.FullPath), "O dry-run de quarentena alterou a origem.");
            var quarantineReport = new MinecraftReportService().WriteQuarantinePlan(quarantinePlan, reportDirectory);
            Assert(File.Exists(quarantineReport), "O relatorio dry-run da quarentena nao foi gerado.");

            AssertThrows<InvalidOperationException>(
                () => quarantineService.Apply(
                    quarantinePlan,
                    [olderDuplicate.FileName],
                    new MinecraftQuarantineConfirmation(false, false)),
                "A quarentena aceitou uma operacao sem confirmacao explicita.");
            AssertThrows<InvalidOperationException>(
                () => quarantineService.Apply(
                    quarantinePlan,
                    [olderDuplicate.FileName],
                    new MinecraftQuarantineConfirmation(true, false)),
                "A quarentena aceitou mod comum sem confirmar o manifesto do servidor.");
            var quarantined = quarantineService.Apply(
                quarantinePlan,
                [olderDuplicate.FileName],
                new MinecraftQuarantineConfirmation(true, true));
            Assert(!File.Exists(olderDuplicate.FullPath), "O JAR selecionado permaneceu na origem depois do apply.");
            Assert(File.Exists(Path.Combine(quarantined.QuarantineDirectory, olderDuplicate.FileName)),
                "O JAR nao chegou a quarentena.");
            Assert(File.Exists(Path.Combine(quarantined.BackupDirectory, olderDuplicate.FileName)),
                "O backup do JAR nao foi criado.");
            _ = quarantineService.RollbackLatest(modsDirectory);
            Assert(File.Exists(olderDuplicate.FullPath), "Rollback da quarentena nao restaurou o JAR.");
            Assert(!File.Exists(Path.Combine(quarantined.QuarantineDirectory, olderDuplicate.FileName)),
                "Rollback da quarentena deixou uma copia movida ativa.");
            messages.Add("PASS: quarentena exige selecao, cria backup SHA-256 e restaura o JAR.");

            var managedRoot = Path.Combine(root, "prism-instance");
            var instanceRoot = Path.Combine(managedRoot, ".minecraft");
            Directory.CreateDirectory(Path.Combine(instanceRoot, "mods"));
            CreateFabricJar(
                Path.Combine(instanceRoot, "mods", "cobblemon-test.jar"),
                "cobblemon",
                "test",
                new Dictionary<string, string>());
            CreateFabricJar(
                Path.Combine(instanceRoot, "mods", "sodium-test.jar"),
                "sodium",
                "0.6.13",
                new Dictionary<string, string>());
            CreateFabricJar(
                Path.Combine(instanceRoot, "mods", "lithium-test.jar"),
                "lithium",
                "0.15.4",
                new Dictionary<string, string>());
            Directory.CreateDirectory(Path.Combine(instanceRoot, "config"));
            var optionsPath = Path.Combine(instanceRoot, "options.txt");
            const string originalOptions = "renderDistance:12\nsimulationDistance:12\nmaxFps:120\ncustomOption:keep\n";
            File.WriteAllText(optionsPath, originalOptions, new UTF8Encoding(false));

            var sodiumPath = Path.Combine(instanceRoot, "config", "sodium-options.json");
            const string originalSodium = "{\"performance\":{\"useEntityCulling\":false,\"useFogOcclusion\":false,\"useBlockFaceCulling\":false,\"animateOnlyVisibleTextures\":false,\"unknown\":17},\"advanced\":{\"enableMemoryTracing\":true}}";
            File.WriteAllText(sodiumPath, originalSodium, new UTF8Encoding(false));
            var immediatelyFastPath = Path.Combine(instanceRoot, "config", "immediatelyfast.json");
            const string originalImmediatelyFast = "{\"font_atlas_resizing\":false,\"map_atlas_generation\":false,\"hud_batching\":false,\"fast_text_lookup\":false,\"fast_buffer_upload\":false,\"experimental_screen_batching\":false}";
            File.WriteAllText(immediatelyFastPath, originalImmediatelyFast, new UTF8Encoding(false));
            var entityCullingPath = Path.Combine(instanceRoot, "config", "entityculling.json");
            const string originalEntityCulling = "{\"debugMode\":true,\"skipEntityCulling\":true,\"skipBlockEntityCulling\":true,\"tickCulling\":false,\"unknown\":\"keep\"}";
            File.WriteAllText(entityCullingPath, originalEntityCulling, new UTF8Encoding(false));
            var irisPath = Path.Combine(instanceRoot, "config", "iris.properties");
            const string originalIris = "enableShaders=true\nunknown=keep\n";
            File.WriteAllText(irisPath, originalIris, new UTF8Encoding(false));
            var unmanagedConfigPath = Path.Combine(instanceRoot, "config", "unmanaged.properties");
            const string originalUnmanagedConfig = "mode=baseline\n";
            File.WriteAllText(unmanagedConfigPath, originalUnmanagedConfig, new UTF8Encoding(false));
            var instanceConfigPath = Path.Combine(managedRoot, "instance.cfg");
            const string originalInstanceConfig = "name=Test Instance\nOverrideMemory=false\nMinMemAlloc=256\nMaxMemAlloc=4096\niconKey=cobblemon\n";
            File.WriteAllText(instanceConfigPath, originalInstanceConfig, new UTF8Encoding(false));

            var profileService = new MinecraftProfileService(
                Path.Combine(root, "backups"),
                reportDirectory);
            var safeMemory = MinecraftEnvironmentService.RecommendJavaMemory(4m, 2.5m);
            var balancedMemory = MinecraftEnvironmentService.RecommendJavaMemory(4m, 3m);
            var aggressiveMemory = MinecraftEnvironmentService.RecommendJavaMemory(4m, 3.5m);
            Assert(safeMemory.MaximumHeapMb == 2048, "O patamar seguro de memoria nao retornou Xmx2048M.");
            Assert(balancedMemory.MaximumHeapMb == 2304, "O patamar balanceado nao retornou Xmx2304M.");
            Assert(aggressiveMemory.MaximumHeapMb == 2560, "O patamar agressivo nao retornou Xmx2560M.");
            messages.Add("PASS: memoria de 4 GB escolhe somente Xmx2048M, Xmx2304M ou Xmx2560M pela RAM livre.");

            var dryRun = profileService.PlanProfile(managedRoot, MinecraftProfileKind.Extreme4Gb, 30);
            Assert(dryRun.Instance.Launcher == MinecraftLauncherKind.PrismLauncher,
                "A instancia Prism nao foi reconhecida.");
            Assert(dryRun.MaximumFps == 30, "O dry-run nao preservou o limite de 30 FPS selecionado.");
            Assert(dryRun.Changes.Any(change => change.Setting == "performance.useEntityCulling" && change.WillWrite),
                "O dry-run nao planejou a config existente do Sodium.");
            Assert(dryRun.Changes.Any(change => change.Setting == "MaxMemAlloc" && change.WillWrite),
                "O dry-run nao planejou a memoria do Prism.");
            Assert(File.ReadAllText(optionsPath).Replace("\r\n", "\n") == originalOptions,
                "O dry-run alterou options.txt.");
            Assert(File.ReadAllText(sodiumPath) == originalSodium, "O dry-run alterou Sodium.");
            Assert(File.ReadAllText(instanceConfigPath).Replace("\r\n", "\n") == originalInstanceConfig,
                "O dry-run alterou instance.cfg.");
            Assert(profileService.PlanProfile(managedRoot, MinecraftProfileKind.Extreme4Gb, 45).MaximumFps == 45,
                "O perfil nao aceitou 45 FPS.");
            Assert(profileService.PlanProfile(managedRoot, MinecraftProfileKind.Extreme4Gb, 60).MaximumFps == 60,
                "O perfil nao aceitou 60 FPS.");
            Assert(MinecraftProfileService.AvailableProfiles.Any(profile => profile.Kind == MinecraftProfileKind.RamLimited) &&
                   MinecraftProfileService.AvailableProfiles.Any(profile => profile.Kind == MinecraftProfileKind.CpuLimited) &&
                   MinecraftProfileService.AvailableProfiles.Any(profile => profile.Kind == MinecraftProfileKind.GpuLimited) &&
                   MinecraftProfileService.AvailableProfiles.Any(profile => profile.Kind == MinecraftProfileKind.ServerEntryCompatible),
                "Os perfis cientificos por gargalo nao foram registrados.");
            AssertThrows<ArgumentOutOfRangeException>(
                () => profileService.PlanProfile(managedRoot, MinecraftProfileKind.Extreme4Gb, 50),
                "O perfil aceitou um limite fora de 30/45/60 FPS.");
            messages.Add("PASS: dry-run planeja options, JSONs, Iris, FPS e Prism sem escrever.");

            var applied = profileService.ApplyProfile(managedRoot, MinecraftProfileKind.Extreme4Gb, 30);
            var changedOptions = File.ReadAllText(optionsPath);
            Assert(changedOptions.Contains("renderDistance:4", StringComparison.Ordinal), "Render distance nao foi aplicada.");
            Assert(changedOptions.Contains("simulationDistance:4", StringComparison.Ordinal), "Simulation distance nao foi aplicada.");
            Assert(changedOptions.Contains("maxFps:30", StringComparison.Ordinal), "O limite selecionado de 30 FPS nao foi aplicado.");
            Assert(changedOptions.Contains("customOption:keep", StringComparison.Ordinal), "Opcao desconhecida foi perdida.");
            Assert(Directory.Exists(applied.BackupDirectory), "Backup nao foi criado.");
            using (var sodium = JsonDocument.Parse(File.ReadAllText(sodiumPath)))
            {
                var performance = sodium.RootElement.GetProperty("performance");
                Assert(performance.GetProperty("useEntityCulling").GetBoolean(), "Entity culling do Sodium nao foi ativado.");
                Assert(performance.GetProperty("unknown").GetInt32() == 17, "Chave desconhecida do Sodium foi perdida.");
            }

            using (var immediatelyFast = JsonDocument.Parse(File.ReadAllText(immediatelyFastPath)))
            {
                Assert(!immediatelyFast.RootElement.GetProperty("hud_batching").GetBoolean(),
                    "HUD batching do ImmediatelyFast nao foi preservado para compatibilidade.");
                Assert(!immediatelyFast.RootElement.GetProperty("experimental_screen_batching").GetBoolean(),
                    "Opcao experimental do ImmediatelyFast foi alterada.");
            }

            Assert(File.ReadAllText(instanceConfigPath).Contains("OverrideMemory=true", StringComparison.Ordinal),
                "Override de memoria do Prism nao foi ativado.");
            Assert(File.ReadAllText(irisPath).Contains("enableShaders=false", StringComparison.Ordinal),
                "Iris nao teve shaders desativados.");
            Assert(File.ReadAllText(irisPath).Contains("unknown=keep", StringComparison.Ordinal),
                "Iris perdeu uma propriedade desconhecida.");
            Assert(File.Exists(applied.ReportPath), "Relatorio antes/depois do apply nao foi gerado.");
            messages.Add("PASS: EXTREME_4GB altera configs reais, preserva desconhecidas e cria backup.");

            _ = profileService.RollbackBackup(managedRoot, applied.BackupId);
            var rolledBack = File.ReadAllText(optionsPath).Replace("\r\n", "\n");
            Assert(rolledBack == originalOptions, "Rollback nao restaurou o options.txt original.");
            Assert(File.ReadAllText(sodiumPath) == originalSodium, "Rollback nao restaurou Sodium byte a byte.");
            Assert(File.ReadAllText(immediatelyFastPath) == originalImmediatelyFast,
                "Rollback nao restaurou ImmediatelyFast byte a byte.");
            Assert(File.ReadAllText(entityCullingPath) == originalEntityCulling,
                "Rollback nao restaurou EntityCulling byte a byte.");
            Assert(File.ReadAllText(irisPath).Replace("\r\n", "\n") == originalIris,
                "Rollback nao restaurou iris.properties byte a byte.");
            Assert(File.ReadAllText(instanceConfigPath).Replace("\r\n", "\n") == originalInstanceConfig,
                "Rollback nao restaurou instance.cfg.");
            Assert(!File.Exists(Path.Combine(instanceRoot, "apextweaker-java-args.txt")),
                "Rollback nao removeu o arquivo criado pelo proprio ApexTweaker.");
            messages.Add("PASS: rollback restaura options, JSONs e launcher e remove somente o arquivo gerado.");

            var legacyDirectory = Path.Combine(profileService.BackupRoot, "legacy-v21");
            Directory.CreateDirectory(legacyDirectory);
            var legacyBackupPath = Path.Combine(legacyDirectory, "options.txt.bak");
            File.Copy(optionsPath, legacyBackupPath);
            var legacyHash = ComputeSha256(optionsPath);
            File.Delete(optionsPath);
            var legacyManifest = new
            {
                backupId = "legacy-v21",
                createdAtUtc = DateTimeOffset.UtcNow.AddMinutes(1),
                instanceRoot,
                profile = "Extreme4Gb",
                files = new[]
                {
                    new
                    {
                        targetPath = optionsPath,
                        backupPath = legacyBackupPath,
                        existedBefore = true,
                        sha256Before = legacyHash,
                        sha256After = (string?)null
                    }
                },
                rolledBackAtUtc = (DateTimeOffset?)null
            };
            File.WriteAllText(
                Path.Combine(legacyDirectory, "manifest.json"),
                JsonSerializer.Serialize(legacyManifest),
                new UTF8Encoding(false));
            _ = profileService.RollbackLatest(managedRoot);
            Assert(File.ReadAllText(optionsPath).Replace("\r\n", "\n") == originalOptions,
                "Rollback nao aceitou o manifesto legado da v2.1.0.");
            messages.Add("PASS: rollback v2.1.0 restaura ate um options.txt removido.");

            var instanceAudit = new MinecraftAuditService().Audit(managedRoot);
            var ramAudit = instanceAudit with
            {
                Environment = instanceAudit.Environment with
                {
                    TotalMemoryGb = 4m,
                    AvailableMemoryGb = 2.5m,
                    PageFileAllocatedMb = 4096,
                    PageFileInUseMb = 256
                }
            };
            var ramDiagnosis = new MinecraftBottleneckDiagnosticService().Diagnose(ramAudit);
            Assert(ramDiagnosis.Primary == MinecraftBottleneckKind.RamLimited,
                "O diagnostico nao classificou o hardware sintetico de 4 GB como RAM_LIMITED.");
            var contractAssessments = new MinecraftModConfigContractCatalog().Assess(instanceAudit, Path.Combine(instanceRoot, "config"));
            Assert(contractAssessments.Single(contract => contract.ModId == "sodium").Status == ModConfigAutomationStatus.Supported,
                "O contrato do Sodium instalado nao foi marcado como suportado.");
            Assert(contractAssessments.Single(contract => contract.ModId == "lithium").Status == ModConfigAutomationStatus.DefaultsRecommended,
                "Lithium deveria preservar os defaults estaveis.");
            messages.Add("PASS: diagnostico de gargalo e contratos de configs distinguem fato, inferencia e manual.");

            var scientificRoot = Path.Combine(root, "scientific-experiments");
            var scientificBackupRoot = Path.Combine(root, "scientific-backups");
            var scientificReports = Path.Combine(root, "scientific-profile-reports");
            var scientificService = new MinecraftScientificExperimentService(
                scientificRoot,
                scientificBackupRoot,
                scientificReports);
            var baselineObservation = new MinecraftOperationalObservation(
                true, true, 70m, true, true, 110m, true, 30d, 16d, false, false, false,
                "Baseline sintetico controlado.");
            var baselineBenchmark = CreateSyntheticBenchmark(
                instanceAudit.Environment,
                instanceRoot,
                averageCpu: 65d,
                peakWorkingSetMb: 1700,
                minimumAvailableGb: 0.70m,
                pageFileDeltaMb: 300);

            var driftStarted = scientificService.StartGuided(managedRoot, 30);
            _ = scientificService.RecordMeasurement(
                driftStarted.Experiment.ExperimentId,
                ScientificMeasurementKind.Baseline,
                baselineObservation,
                baselineBenchmark);
            File.WriteAllText(unmanagedConfigPath, "mode=external-drift\n", new UTF8Encoding(false));
            AssertThrows<InvalidOperationException>(
                () => scientificService.ApplyCandidate(driftStarted.Experiment.ExperimentId, userConfirmed: true),
                "Apply cientifico aceitou drift de config posterior ao baseline.");
            File.WriteAllText(unmanagedConfigPath, originalUnmanagedConfig, new UTF8Encoding(false));

            var started = scientificService.StartGuided(managedRoot, 30);
            Assert(started.Experiment.Phase == ScientificExperimentPhase.BaselinePending,
                "Experimento cientifico nao iniciou aguardando baseline.");
            var baselineRecorded = scientificService.RecordMeasurement(
                started.Experiment.ExperimentId,
                ScientificMeasurementKind.Baseline,
                baselineObservation,
                baselineBenchmark);
            Assert(baselineRecorded.Experiment.Phase == ScientificExperimentPhase.BaselineRecorded,
                "Baseline cientifico nao foi persistido.");
            var candidateApplied = scientificService.ApplyCandidate(started.Experiment.ExperimentId, userConfirmed: true);
            Assert(candidateApplied.Experiment.Phase == ScientificExperimentPhase.CandidateApplied,
                "Candidato cientifico nao foi aplicado.");
            var candidateObservation = baselineObservation with
            {
                MenuLoadSeconds = 58m,
                JoinLoadSeconds = 88m,
                AverageFps = 38d,
                MinimumFps = 22d,
                Notes = "Candidato sintetico na mesma cena."
            };
            var candidateBenchmark = CreateSyntheticBenchmark(
                instanceAudit.Environment,
                instanceRoot,
                averageCpu: 50d,
                peakWorkingSetMb: 1450,
                minimumAvailableGb: 0.95m,
                pageFileDeltaMb: 80);
            var candidateRecorded = scientificService.RecordMeasurement(
                started.Experiment.ExperimentId,
                ScientificMeasurementKind.Candidate,
                candidateObservation,
                candidateBenchmark);
            Assert(candidateRecorded.Experiment.Phase == ScientificExperimentPhase.CandidateRecorded,
                "Candidato cientifico nao foi persistido.");
            var compared = scientificService.Compare(started.Experiment.ExperimentId);
            Assert(compared.Experiment.Comparison?.Decision == ScientificDecision.Keep,
                "O comparador nao manteve um candidato com melhora consistente.");
            var finalized = scientificService.Finalize(started.Experiment.ExperimentId, rollbackConfirmed: false);
            Assert(finalized.Experiment.Phase == ScientificExperimentPhase.Kept,
                "Experimento aprovado nao terminou em KEPT.");
            Assert(File.Exists(finalized.Reports.JsonPath) &&
                   File.Exists(finalized.Reports.MarkdownPath) &&
                   File.Exists(finalized.Reports.TextPath),
                "Relatorios cientificos JSON/Markdown/TXT nao foram gerados.");
            var store = new MinecraftScientificExperimentStore(scientificRoot);
            Assert(store.Load(started.Experiment.ExperimentId).Phase == ScientificExperimentPhase.Kept,
                "Store nao persistiu o estado final do experimento.");
            AssertThrows<ArgumentException>(() => store.Load("../experimento-invalido"),
                "Store aceitou path traversal no ID do experimento.");

            Assert(finalized.Experiment.AppliedProfileBackupId is not null,
                "Experimento KEEP nao registrou o backup aplicado para auditoria.");
            _ = new MinecraftProfileService(scientificBackupRoot, scientificReports)
                .RollbackBackup(managedRoot, finalized.Experiment.AppliedProfileBackupId!);
            Assert(File.ReadAllText(optionsPath).Replace("\r\n", "\n") == originalOptions,
                "Preparacao do teste REVERT nao restaurou o baseline original.");

            var revertStarted = scientificService.StartGuided(managedRoot, 30);
            _ = scientificService.RecordMeasurement(
                revertStarted.Experiment.ExperimentId,
                ScientificMeasurementKind.Baseline,
                baselineObservation,
                baselineBenchmark);
            _ = scientificService.ApplyCandidate(revertStarted.Experiment.ExperimentId, userConfirmed: true);
            var failingObservation = candidateObservation with
            {
                Crashed = true,
                OutOfMemory = true,
                ServerEntered = false,
                Notes = "Regressao sintetica para validar rollback."
            };
            var failingBenchmark = candidateBenchmark with
            {
                Status = BenchmarkStatus.Failed,
                CrashEvidence = true,
                OutOfMemoryEvidence = true
            };
            _ = scientificService.RecordMeasurement(
                revertStarted.Experiment.ExperimentId,
                ScientificMeasurementKind.Candidate,
                failingObservation,
                failingBenchmark);
            var revertCompared = scientificService.Compare(revertStarted.Experiment.ExperimentId);
            Assert(revertCompared.Experiment.Comparison?.Decision == ScientificDecision.Revert,
                "Experimento com crash/OOM nao decidiu REVERT.");
            var reverted = scientificService.Finalize(
                revertStarted.Experiment.ExperimentId,
                rollbackConfirmed: true);
            Assert(reverted.Experiment.Phase == ScientificExperimentPhase.Reverted,
                "Experimento degradado nao terminou em REVERTED.");
            Assert(File.ReadAllText(optionsPath).Replace("\r\n", "\n") == originalOptions,
                "Rollback cientifico nao restaurou options.txt ao baseline.");

            var contaminatedStarted = scientificService.StartGuided(managedRoot, 30);
            _ = scientificService.RecordMeasurement(
                contaminatedStarted.Experiment.ExperimentId,
                ScientificMeasurementKind.Baseline,
                baselineObservation,
                baselineBenchmark);
            var contaminatedApplied = scientificService.ApplyCandidate(
                contaminatedStarted.Experiment.ExperimentId,
                userConfirmed: true);
            File.WriteAllText(unmanagedConfigPath, "mode=contaminated\n", new UTF8Encoding(false));
            _ = scientificService.RecordMeasurement(
                contaminatedStarted.Experiment.ExperimentId,
                ScientificMeasurementKind.Candidate,
                candidateObservation,
                candidateBenchmark);
            var contaminatedCompared = scientificService.Compare(contaminatedStarted.Experiment.ExperimentId);
            var contaminatedComparison = contaminatedCompared.Experiment.Comparison
                ?? throw new InvalidOperationException("Comparacao contaminada nao foi gerada.");
            Assert(contaminatedComparison.Decision == ScientificDecision.Retest &&
                   contaminatedComparison.Confidence == ScientificConfidence.Low,
                "Mudanca fora da hipotese nao rebaixou a decisao para RETEST com baixa confianca.");
            Assert(contaminatedComparison.Rationale.Any(item =>
                    item.Contains("fora da hipotese", StringComparison.OrdinalIgnoreCase)),
                "Comparacao contaminada nao explicou o arquivo externo.");
            var contaminatedFinalized = scientificService.Finalize(
                contaminatedStarted.Experiment.ExperimentId,
                rollbackConfirmed: true);
            Assert(contaminatedFinalized.Experiment.Phase == ScientificExperimentPhase.NeedsRetest,
                "Experimento contaminado nao terminou em NEEDS_RETEST.");
            Assert(contaminatedApplied.Experiment.AppliedProfileBackupId is not null,
                "Experimento contaminado nao registrou backup para limpeza do teste.");
            File.WriteAllText(unmanagedConfigPath, originalUnmanagedConfig, new UTF8Encoding(false));
            Assert(File.ReadAllText(optionsPath).Replace("\r\n", "\n") == originalOptions,
                "NEEDS_RETEST nao restaurou options.txt pelo backup gerenciado.");

            var metricsService = new MinecraftScientificMetricsService();
            var comparisonService = new MinecraftScientificComparisonService();
            var stableMetrics = metricsService.Build(baselineObservation, baselineBenchmark);
            var failedMetrics = metricsService.Build(
                candidateObservation with { Crashed = true, OutOfMemory = true },
                candidateBenchmark with { CrashEvidence = true, OutOfMemoryEvidence = true, Status = BenchmarkStatus.Failed });
            var emptyEvidence = new MinecraftInstanceEvidence(
                DateTimeOffset.UtcNow,
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                []);
            var criticalComparison = comparisonService.Compare(
                new MinecraftExperimentMeasurement(
                    "measure-baseline-test", ScientificMeasurementKind.Baseline, DateTimeOffset.UtcNow,
                    baselineObservation, baselineBenchmark, stableMetrics, emptyEvidence, string.Empty),
                new MinecraftExperimentMeasurement(
                    "measure-candidate-test", ScientificMeasurementKind.Candidate, DateTimeOffset.UtcNow,
                    candidateObservation with { Crashed = true, OutOfMemory = true }, candidateBenchmark,
                    failedMetrics, emptyEvidence, string.Empty));
            Assert(criticalComparison.Decision == ScientificDecision.Revert && criticalComparison.CriticalRegression,
                "Crash/OOM novo nao gerou decisao REVERT critica.");
            messages.Add("PASS: motor cientifico bloqueia drift, detecta contaminacao e executa KEEP/REVERT auditaveis.");

            var operationalService = new MinecraftOperationalHomologationService();
            var checklist = operationalService.BuildChecklist(audit, quarantinePlan, dryRun);
            var checklistPath = new MinecraftReportService().WriteOperationalChecklist(checklist, reportDirectory);
            Assert(File.Exists(checklistPath) &&
                   File.Exists(Path.ChangeExtension(checklistPath, ".json")) &&
                   File.Exists(Path.ChangeExtension(checklistPath, ".txt")),
                "Checklist operacional nao gerou JSON, Markdown e TXT.");
            var approvedObservation = new MinecraftOperationalObservation(
                true,
                true,
                45m,
                true,
                true,
                70m,
                true,
                32d,
                18d,
                false,
                false,
                false,
                "Rodada sintetica do self-test.");
            var approved = operationalService.Evaluate(managedRoot, approvedObservation);
            Assert(approved.Status == OperationalHomologationStatus.Approved,
                "A homologacao aprovada nao foi classificada corretamente.");
            var operationalPath = new MinecraftReportService().WriteOperationalHomologation(approved, reportDirectory);
            Assert(File.Exists(operationalPath) && File.Exists(Path.ChangeExtension(operationalPath, ".json")),
                "Relatorio operacional nao foi gerado.");
            var failed = operationalService.Evaluate(
                managedRoot,
                approvedObservation with { Crashed = true, OutOfMemory = true });
            Assert(failed.Status == OperationalHomologationStatus.Failed,
                "Crash/OOM nao reprovou a homologacao.");
            var unstable = operationalService.Evaluate(
                managedRoot,
                approvedObservation with { AverageFps = 24d, SevereDrops = true });
            Assert(unstable.Status == OperationalHomologationStatus.Unstable,
                "FPS baixo e quedas severas nao marcaram a homologacao como instavel.");
            var notTested = operationalService.Evaluate(
                managedRoot,
                new MinecraftOperationalObservation(
                    false, false, null, false, false, null, false, null, null, false, false, false, string.Empty));
            Assert(notTested.Status == OperationalHomologationStatus.NotTested,
                "Observacao vazia nao foi marcada como nao testada.");
            messages.Add("PASS: checklist e homologacao distinguem APROVADO, INSTAVEL, FALHOU e NAO_TESTADO.");

            Directory.CreateDirectory(Path.Combine(instanceRoot, "logs"));
            Directory.CreateDirectory(Path.Combine(instanceRoot, "crash-reports"));
            File.WriteAllText(
                Path.Combine(instanceRoot, "logs", "latest.log"),
                "[main/ERROR] java.lang.OutOfMemoryError: Java heap space\n",
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(instanceRoot, "crash-reports", "crash-test.txt"),
                "---- Minecraft Crash Report ----\nSynthetic self-test evidence\n",
                new UTF8Encoding(false));
            var benchmark = new MinecraftBenchmarkService(() => null)
                .CaptureAsync(TimeSpan.FromSeconds(5), selectedPath: managedRoot)
                .GetAwaiter()
                .GetResult();
            Assert(benchmark.Status == BenchmarkStatus.NotTested, "Ausencia de processo deveria resultar em NAO_TESTADO.");
            Assert(benchmark.OutOfMemoryEvidence && benchmark.CrashEvidence,
                "O benchmark nao leu as evidencias de log/crash.");
            Assert(benchmark.ActiveMods.Count > 0, "O benchmark nao registrou a lista de mods.");
            var benchmarkPath = new MinecraftReportService().WriteBenchmark(benchmark, reportDirectory);
            Assert(File.Exists(benchmarkPath), "Relatorio de benchmark nao foi gerado.");
            messages.Add("PASS: benchmark NAO_TESTADO registra ambiente, configs, logs e crash reports.");

            var survival = new MinecraftSurvivalPlanService().Build(audit, quarantinePlan);
            var survivalPath = new MinecraftReportService().WriteSurvivalPlan(survival, reportDirectory);
            Assert(File.Exists(survivalPath) && survival.GraphicsSettings.Count > 0,
                "Plano de Sobrevivencia 4 GB nao foi gerado.");
            messages.Add("PASS: Plano de Sobrevivencia 4 GB gera veredito e acoes manuais.");

            var view = new MinecraftView();
            view.SetSelectedPath(modsDirectory);
            Assert(view.SelectedPath == modsDirectory, "A view Cobblemon nao carregou o caminho selecionado.");
            messages.Add("PASS: XAML da pagina Cobblemon carrega em thread STA.");

            messages.Add("SELF_TEST_OK");
            return messages;
        }
        finally
        {
            var tempRoot = Path.GetFullPath(Path.GetTempPath());
            var fullRoot = Path.GetFullPath(root);
            if (fullRoot.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullRoot))
            {
                Directory.Delete(fullRoot, recursive: true);
            }
        }
    }

    private static void CreateFabricJar(
        string path,
        string id,
        string version,
        IReadOnlyDictionary<string, string> dependencies)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);
        var entry = archive.CreateEntry("fabric.mod.json", CompressionLevel.NoCompression);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        var dependencyJson = JsonSerializer.Serialize(dependencies);

        writer.Write($"{{\"schemaVersion\":1,\"id\":\"{id}\",\"name\":\"{id}\",\"version\":\"{version}\",\"environment\":\"*\",\"depends\":{dependencyJson}}}");
    }

    private static MinecraftBenchmarkResult CreateSyntheticBenchmark(
        MinecraftEnvironmentSnapshot environment,
        string instanceRoot,
        double averageCpu,
        int peakWorkingSetMb,
        decimal minimumAvailableGb,
        long pageFileDeltaMb)
    {
        var started = DateTimeOffset.UtcNow;
        var before = environment with { PageFileInUseMb = 200 };
        var after = environment with
        {
            AvailableMemoryGb = minimumAvailableGb + 0.10m,
            PageFileInUseMb = 200 + pageFileDeltaMb
        };
        var samples = Enumerable.Range(0, 20)
            .Select(index => new MinecraftBenchmarkSample(
                started.AddSeconds(index + 1),
                (peakWorkingSetMb - (index % 3) * 10L) * 1024L * 1024L,
                (peakWorkingSetMb + 100L) * 1024L * 1024L,
                minimumAvailableGb + (index % 2) * 0.05m,
                averageCpu,
                index * 4L * 1024L * 1024L,
                index * 1L * 1024L * 1024L))
            .ToArray();
        return new MinecraftBenchmarkResult(
            started,
            TimeSpan.FromSeconds(20),
            instanceRoot,
            before,
            after,
            "javaw",
            4242,
            BenchmarkStatus.Approved,
            peakWorkingSetMb * 1024L * 1024L,
            (peakWorkingSetMb + 100L) * 1024L * 1024L,
            minimumAvailableGb,
            FpsMeasured: false,
            samples,
            [],
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            null,
            null,
            null,
            null,
            OutOfMemoryEvidence: false,
            CrashEvidence: false,
            ["Synthetic self-test benchmark"]);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
