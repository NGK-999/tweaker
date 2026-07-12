using System.IO;
using System.Globalization;
using ApexTweaker.Minecraft.Models;
using ApexTweaker.Minecraft.Services;

namespace ApexTweaker.Minecraft;

internal static class MinecraftCommandLine
{
    public static bool TryRun(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length == 0 || !args.Any(arg => arg.StartsWith("--minecraft-", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        try
        {
            if (HasFlag(args, "--minecraft-help"))
            {
                WriteUsage();
                return true;
            }

            if (HasFlag(args, "--minecraft-self-test"))
            {
                var selfTest = MinecraftSelfTest.Run();
                foreach (var line in selfTest)
                {
                    Console.WriteLine(line);
                }

                WriteStatusFile(args, "SELF_TEST_OK", string.Join(Environment.NewLine, selfTest));
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-plan"))
            {
                var instance = RequireValue(args, "--instance");
                var service = CreateScientificService(args);
                var result = service.Plan(instance, ParseFps(args), GetValue(args, "--output"));
                Console.WriteLine($"Gargalo: {result.Plan.Diagnosis.Primary} ({result.Plan.Diagnosis.Confidence})");
                Console.WriteLine($"Perfil: {result.Plan.SelectedProfile} | JVM: {result.Plan.JavaMemory.Arguments} | FPS: {result.Plan.MaximumFps}");
                Console.WriteLine(result.Reports.MarkdownPath);
                WriteStatusFile(args, "SCIENTIFIC_PLAN_OK", result.Reports.MarkdownPath);
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-start") ||
                HasFlag(args, "--minecraft-scientific-auto-optimize"))
            {
                var instance = RequireValue(args, "--instance");
                var preset = GetValue(args, "--preset");
                var service = CreateScientificService(args);
                var result = string.IsNullOrWhiteSpace(preset)
                    ? service.StartGuided(instance, ParseFps(args))
                    : service.StartCustom(instance, preset);
                Console.WriteLine($"Experimento: {result.Experiment.ExperimentId}");
                Console.WriteLine($"Fase: {result.Experiment.Phase}");
                Console.WriteLine(result.Reports.MarkdownPath);
                WriteStatusFile(args, "SCIENTIFIC_EXPERIMENT_STARTED", result.Experiment.ExperimentId);
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-record"))
            {
                var experimentId = RequireValue(args, "--experiment");
                var service = CreateScientificService(args);
                var experiment = service.Load(experimentId);
                var measurementKind = ParseMeasurementKind(RequireValue(args, "--phase"));
                var benchmark = CaptureOptionalBenchmark(args, experiment.InstanceRoot);
                var result = service.RecordMeasurement(
                    experimentId,
                    measurementKind,
                    ParseObservation(args),
                    benchmark);
                Console.WriteLine($"{result.Experiment.Phase}: {result.Reports.MarkdownPath}");
                WriteStatusFile(args, "SCIENTIFIC_MEASUREMENT_RECORDED", result.Reports.MarkdownPath);
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-apply"))
            {
                RequireConfirmation(args);
                var result = CreateScientificService(args).ApplyCandidate(
                    RequireValue(args, "--experiment"),
                    userConfirmed: true);
                Console.WriteLine($"{result.Experiment.Phase}: backup={result.Experiment.AppliedProfileBackupId ?? "SEM_ESCRITA"}");
                Console.WriteLine(result.Reports.MarkdownPath);
                WriteStatusFile(args, "SCIENTIFIC_CANDIDATE_APPLIED", result.Reports.MarkdownPath);
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-compare"))
            {
                var result = CreateScientificService(args).Compare(RequireValue(args, "--experiment"));
                Console.WriteLine($"Decisao: {result.Experiment.Comparison?.Decision} | score={result.Experiment.Comparison?.Score} | confianca={result.Experiment.Comparison?.Confidence}");
                Console.WriteLine(result.Reports.MarkdownPath);
                WriteStatusFile(args, "SCIENTIFIC_COMPARISON_OK", result.Reports.MarkdownPath);
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-finalize"))
            {
                RequireConfirmation(args);
                var result = CreateScientificService(args).Finalize(
                    RequireValue(args, "--experiment"),
                    rollbackConfirmed: true);
                Console.WriteLine($"Fase final: {result.Experiment.Phase}");
                Console.WriteLine(result.Reports.MarkdownPath);
                WriteStatusFile(args, "SCIENTIFIC_EXPERIMENT_FINALIZED", result.Reports.MarkdownPath);
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-cancel"))
            {
                RequireConfirmation(args);
                var result = CreateScientificService(args).Cancel(
                    RequireValue(args, "--experiment"),
                    rollbackConfirmed: true);
                Console.WriteLine($"Fase final: {result.Experiment.Phase}");
                Console.WriteLine(result.Reports.MarkdownPath);
                WriteStatusFile(args, "SCIENTIFIC_EXPERIMENT_CANCELLED", result.Reports.MarkdownPath);
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-show"))
            {
                var experiment = CreateScientificService(args).Load(RequireValue(args, "--experiment"));
                Console.WriteLine($"{experiment.ExperimentId} | {experiment.Phase} | {experiment.Comparison?.Decision.ToString() ?? "SEM_DECISAO"}");
                WriteStatusFile(args, "SCIENTIFIC_EXPERIMENT_FOUND", experiment.Phase.ToString());
                return true;
            }

            if (HasFlag(args, "--minecraft-scientific-list"))
            {
                foreach (var experiment in CreateScientificService(args).List())
                {
                    Console.WriteLine($"{experiment.ExperimentId} | {experiment.Phase} | {experiment.UpdatedAtUtc:O}");
                }

                return true;
            }

            if (HasFlag(args, "--minecraft-audit"))
            {
                var modsDirectory = RequireValue(args, "--mods");
                var output = GetValue(args, "--output");
                var target = GetValue(args, "--target") ?? "1.21.1";
                var result = new MinecraftAuditService().Audit(modsDirectory, target, MinecraftLoader.Fabric);
                var paths = new MinecraftReportService().WriteAudit(result, output);
                var quarantine = new MinecraftQuarantineService().BuildPlan(result);
                var quarantinePath = new MinecraftReportService().WriteQuarantinePlan(quarantine, output);
                var survival = new MinecraftSurvivalPlanService().Build(result, quarantine);
                var survivalPath = new MinecraftReportService().WriteSurvivalPlan(survival, output);
                Console.WriteLine(paths.JsonPath);
                Console.WriteLine(paths.MarkdownPath);
                Console.WriteLine(paths.TextPath);
                Console.WriteLine(paths.QuarantineSuggestionsDirectory);
                Console.WriteLine(quarantinePath);
                Console.WriteLine(survivalPath);
                WriteStatusFile(args, "AUDIT_OK", paths.JsonPath);
                return true;
            }

            if (HasFlag(args, "--minecraft-profile-dry-run"))
            {
                var instance = RequireValue(args, "--instance");
                var profile = ParseProfile(GetValue(args, "--profile") ?? "EXTREME_4GB");
                var plan = new MinecraftProfileService().PlanProfile(instance, profile, ParseFps(args));
                var path = new MinecraftReportService().WriteProfilePlan(
                    plan,
                    applied: false,
                    outputDirectory: GetValue(args, "--output"));
                Console.WriteLine($"DRY_RUN: {plan.Changes.Count(change => change.WillWrite)} alteracoes propostas.");
                Console.WriteLine(path);
                WriteStatusFile(args, "PROFILE_DRY_RUN_OK", path);
                return true;
            }

            if (HasFlag(args, "--minecraft-experiment-dry-run"))
            {
                var plan = new MinecraftProfileService().PlanExperiment(
                    RequireValue(args, "--instance"),
                    RequireValue(args, "--preset"));
                var path = new MinecraftReportService().WriteProfilePlan(
                    plan,
                    applied: false,
                    outputDirectory: GetValue(args, "--output"));
                Console.WriteLine($"DRY_RUN: {plan.Experiment?.DisplayName} | {plan.Changes.Count(change => change.WillWrite)} alteracoes.");
                Console.WriteLine(path);
                WriteStatusFile(args, "EXPERIMENT_DRY_RUN_OK", path);
                return true;
            }

            if (HasFlag(args, "--minecraft-apply-profile"))
            {
                RequireConfirmation(args);
                var instance = RequireValue(args, "--instance");
                var profile = ParseProfile(GetValue(args, "--profile") ?? "EXTREME_4GB");
                var result = new MinecraftProfileService().ApplyProfile(instance, profile, ParseFps(args));
                Console.WriteLine($"{result.Profile}: {result.InstanceRoot}");
                Console.WriteLine($"Backup: {result.BackupDirectory}");
                Console.WriteLine($"Relatorio: {result.ReportPath}");
                WriteStatusFile(args, "PROFILE_OK", result.BackupDirectory);
                return true;
            }

            if (HasFlag(args, "--minecraft-rollback"))
            {
                RequireConfirmation(args);
                var instance = RequireValue(args, "--instance");
                var result = new MinecraftProfileService().RollbackLatest(instance);
                Console.WriteLine($"Rollback: {result.BackupId}");
                WriteStatusFile(args, "ROLLBACK_OK", result.BackupId);
                return true;
            }

            if (HasFlag(args, "--minecraft-quarantine-dry-run"))
            {
                var modsDirectory = RequireValue(args, "--mods");
                var audit = new MinecraftAuditService().Audit(modsDirectory);
                var plan = new MinecraftQuarantineService().BuildPlan(audit);
                var path = new MinecraftReportService().WriteQuarantinePlan(plan, GetValue(args, "--output"));
                foreach (var candidate in plan.Candidates)
                {
                    Console.WriteLine(
                        $"{candidate.FileName} | {candidate.SideAssessment} | {candidate.Risk} | " +
                        $"servidor={candidate.ServerEntryImpact} | acao={candidate.OperationalRecommendation}");
                }

                Console.WriteLine(path);
                WriteStatusFile(args, "QUARANTINE_DRY_RUN_OK", path);
                return true;
            }

            if (HasFlag(args, "--minecraft-quarantine-apply"))
            {
                RequireConfirmation(args);
                var modsDirectory = RequireValue(args, "--mods");
                var fileList = RequireValue(args, "--files")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var audit = new MinecraftAuditService().Audit(modsDirectory);
                var plan = new MinecraftQuarantineService().BuildPlan(audit);
                var result = new MinecraftQuarantineService().Apply(
                    plan,
                    fileList,
                    new MinecraftQuarantineConfirmation(
                        UserConfirmed: true,
                        ServerManifestConfirmed: HasFlag(args, "--server-manifest-confirmed")));
                Console.WriteLine($"Quarentena: {result.QuarantineDirectory}");
                Console.WriteLine($"Backup: {result.BackupDirectory}");
                Console.WriteLine($"Manifesto: {result.ManifestPath}");
                WriteStatusFile(args, "QUARANTINE_APPLY_OK", result.ManifestPath);
                return true;
            }

            if (HasFlag(args, "--minecraft-operational-checklist"))
            {
                var modsDirectory = RequireValue(args, "--mods");
                var audit = new MinecraftAuditService().Audit(modsDirectory);
                var quarantine = new MinecraftQuarantineService().BuildPlan(audit);
                MinecraftProfilePlan? profilePlan = null;
                var instance = GetValue(args, "--instance");
                if (!string.IsNullOrWhiteSpace(instance))
                {
                    profilePlan = new MinecraftProfileService().PlanProfile(
                        instance,
                        MinecraftProfileKind.Extreme4Gb,
                        ParseFps(args));
                }

                var checklist = new MinecraftOperationalHomologationService()
                    .BuildChecklist(audit, quarantine, profilePlan);
                var path = new MinecraftReportService()
                    .WriteOperationalChecklist(checklist, GetValue(args, "--output"));
                Console.WriteLine(path);
                WriteStatusFile(args, "OPERATIONAL_CHECKLIST_OK", path);
                return true;
            }

            if (HasFlag(args, "--minecraft-homologation-report"))
            {
                var instance = RequireValue(args, "--instance");
                var observation = ParseObservation(args);

                MinecraftBenchmarkResult? benchmark = null;
                var benchmarkSecondsText = GetValue(args, "--benchmark-seconds");
                if (benchmarkSecondsText is not null)
                {
                    if (!int.TryParse(benchmarkSecondsText, out var benchmarkSeconds) || benchmarkSeconds is < 10 or > 600)
                    {
                        throw new ArgumentException("--benchmark-seconds deve estar entre 10 e 600.");
                    }

                    benchmark = new MinecraftBenchmarkService()
                        .CaptureAsync(
                            TimeSpan.FromSeconds(benchmarkSeconds),
                            selectedPath: instance)
                        .GetAwaiter()
                        .GetResult();
                }

                var result = new MinecraftOperationalHomologationService()
                    .Evaluate(instance, observation, benchmark);
                var path = new MinecraftReportService()
                    .WriteOperationalHomologation(result, GetValue(args, "--output"));
                Console.WriteLine($"{result.Status}: {path}");
                WriteStatusFile(args, "HOMOLOGATION_REPORT_OK", path);
                return true;
            }

            if (HasFlag(args, "--minecraft-quarantine-rollback"))
            {
                RequireConfirmation(args);
                var modsDirectory = RequireValue(args, "--mods");
                var result = new MinecraftQuarantineService().RollbackLatest(modsDirectory);
                Console.WriteLine($"Rollback da quarentena: {result.OperationId}");
                WriteStatusFile(args, "QUARANTINE_ROLLBACK_OK", result.OperationId);
                return true;
            }

            if (HasFlag(args, "--minecraft-discover-instances"))
            {
                foreach (var instance in new MinecraftInstanceService().Discover())
                {
                    Console.WriteLine($"{instance.Launcher} | {instance.DisplayName} | {instance.GameDirectory}");
                }

                return true;
            }

            if (HasFlag(args, "--minecraft-benchmark"))
            {
                var secondsText = GetValue(args, "--seconds") ?? "60";
                if (!int.TryParse(secondsText, out var seconds))
                {
                    throw new ArgumentException("--seconds deve ser um numero inteiro.");
                }

                var waitText = GetValue(args, "--wait-seconds") ?? "0";
                if (!int.TryParse(waitText, out var waitSeconds) || waitSeconds < 0 || waitSeconds > 300)
                {
                    throw new ArgumentException("--wait-seconds deve estar entre 0 e 300.");
                }

                var benchmark = new MinecraftBenchmarkService()
                    .CaptureAsync(
                        TimeSpan.FromSeconds(seconds),
                        selectedPath: GetValue(args, "--instance"),
                        processWait: TimeSpan.FromSeconds(waitSeconds))
                    .GetAwaiter()
                    .GetResult();
                var path = new MinecraftReportService().WriteBenchmark(benchmark, GetValue(args, "--output"));
                Console.WriteLine(path);
                WriteStatusFile(args, "BENCHMARK_OK", path);
                return true;
            }

            throw new ArgumentException("Comando Minecraft desconhecido. Use --minecraft-help.");
        }
        catch (Exception ex)
        {
            exitCode = 1;
            Console.Error.WriteLine(ex.Message);
            WriteStatusFile(args, "ERROR", ex.ToString());
            return true;
        }
    }

    private static void RequireConfirmation(string[] args)
    {
        if (!HasFlag(args, "--yes"))
        {
            throw new InvalidOperationException("Operacao de escrita bloqueada. Repita com --yes apos revisar o caminho e o backup.");
        }
    }

    private static MinecraftProfileKind ParseProfile(string value)
    {
        var normalized = value.Replace("-", string.Empty).Replace("_", string.Empty);
        return normalized.ToUpperInvariant() switch
        {
            "SAFE" => MinecraftProfileKind.Safe,
            "LOWEND" => MinecraftProfileKind.LowEnd,
            "EXTREME4GB" => MinecraftProfileKind.Extreme4Gb,
            "POTATOCOBBLEMON4GB" => MinecraftProfileKind.PotatoCobblemon4Gb,
            "GPULIMITED" => MinecraftProfileKind.GpuLimited,
            "RAMLIMITED" => MinecraftProfileKind.RamLimited,
            "CPULIMITED" => MinecraftProfileKind.CpuLimited,
            "SERVERENTRYCOMPATIBLE" => MinecraftProfileKind.ServerEntryCompatible,
            "COBBLEMONSERVERCLIENT" => MinecraftProfileKind.CobblemonServerClient,
            "BENCHMARK" => MinecraftProfileKind.Benchmark,
            _ => throw new ArgumentException($"Perfil desconhecido: {value}")
        };
    }

    private static int? ParseFps(string[] args)
    {
        var value = GetValue(args, "--fps");
        if (value is null)
        {
            return null;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fps) ||
            fps is not (20 or 24 or 30 or 45 or 60))
        {
            throw new ArgumentException("--fps deve ser 20, 24, 30, 45 ou 60.");
        }

        return fps;
    }

    private static ScientificMeasurementKind ParseMeasurementKind(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "BASELINE" => ScientificMeasurementKind.Baseline,
            "CANDIDATE" or "CANDIDATO" => ScientificMeasurementKind.Candidate,
            _ => throw new ArgumentException("--phase deve ser baseline ou candidate.")
        };
    }

    private static MinecraftScientificExperimentService CreateScientificService(string[] args)
    {
        var root = GetValue(args, "--experiment-root");
        return new MinecraftScientificExperimentService(
            experimentRoot: root,
            profileBackupRoot: root is null ? null : Path.Combine(root, "_profile-backups"),
            profileReportRoot: root is null ? null : Path.Combine(root, "_profile-reports"));
    }

    private static MinecraftOperationalObservation ParseObservation(string[] args)
    {
        return new MinecraftOperationalObservation(
            GameOpened: HasFlag(args, "--game-opened"),
            MenuReached: HasFlag(args, "--menu-reached"),
            MenuLoadSeconds: ParseOptionalDecimal(args, "--menu-seconds"),
            WorldEntered: HasFlag(args, "--world-entered"),
            ServerEntered: HasFlag(args, "--server-entered"),
            JoinLoadSeconds: ParseOptionalDecimal(args, "--join-seconds"),
            PlayableAt720p: HasFlag(args, "--playable-720p"),
            AverageFps: ParseOptionalDouble(args, "--average-fps"),
            MinimumFps: ParseOptionalDouble(args, "--minimum-fps"),
            SevereDrops: HasFlag(args, "--severe-drops"),
            Crashed: HasFlag(args, "--crashed"),
            OutOfMemory: HasFlag(args, "--out-of-memory"),
            Notes: GetValue(args, "--notes") ?? string.Empty);
    }

    private static MinecraftBenchmarkResult? CaptureOptionalBenchmark(string[] args, string instanceRoot)
    {
        var durationText = GetValue(args, "--benchmark-seconds");
        if (durationText is null)
        {
            return null;
        }

        if (!int.TryParse(durationText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) ||
            seconds is < 10 or > 600)
        {
            throw new ArgumentException("--benchmark-seconds deve estar entre 10 e 600.");
        }

        var waitText = GetValue(args, "--wait-seconds") ?? "0";
        if (!int.TryParse(waitText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var waitSeconds) ||
            waitSeconds is < 0 or > 300)
        {
            throw new ArgumentException("--wait-seconds deve estar entre 0 e 300.");
        }

        return new MinecraftBenchmarkService()
            .CaptureAsync(
                TimeSpan.FromSeconds(seconds),
                selectedPath: instanceRoot,
                processWait: TimeSpan.FromSeconds(waitSeconds))
            .GetAwaiter()
            .GetResult();
    }

    private static decimal? ParseOptionalDecimal(string[] args, string key)
    {
        var value = GetValue(args, key);
        if (value is null)
        {
            return null;
        }

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) || result < 0)
        {
            throw new ArgumentException($"{key} deve ser um numero positivo usando ponto decimal.");
        }

        return result;
    }

    private static double? ParseOptionalDouble(string[] args, string key)
    {
        var value = GetValue(args, key);
        if (value is null)
        {
            return null;
        }

        if (!double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) || result < 0)
        {
            throw new ArgumentException($"{key} deve ser um numero positivo usando ponto decimal.");
        }

        return result;
    }

    private static string RequireValue(string[] args, string key)
    {
        return GetValue(args, key) ?? throw new ArgumentException($"Parametro obrigatorio ausente: {key}");
    }

    private static string? GetValue(string[] args, string key)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], key, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static bool HasFlag(string[] args, string flag)
    {
        return args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
    }

    private static void WriteStatusFile(string[] args, string status, string detail)
    {
        var statusPath = GetValue(args, "--status-file");
        if (string.IsNullOrWhiteSpace(statusPath))
        {
            return;
        }

        var fullPath = Path.GetFullPath(statusPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, $"{status}{Environment.NewLine}{detail}{Environment.NewLine}");
    }

    private static void WriteUsage()
    {
        Console.WriteLine("ApexTweaker Minecraft commands:");
        Console.WriteLine("  scientific commands accept [--experiment-root <path>] for a custom writable store");
        Console.WriteLine("  --minecraft-scientific-plan --instance <path> [--fps 20|24|30|45|60] [--output <path>]");
        Console.WriteLine("  --minecraft-scientific-auto-optimize --instance <path> [--fps 20|24|30|45|60]");
        Console.WriteLine("  --minecraft-scientific-start --instance <path> [--fps ...] [--preset resolution-854x480]");
        Console.WriteLine("  --minecraft-scientific-record --experiment <id> --phase baseline|candidate [observacoes] [--benchmark-seconds 60]");
        Console.WriteLine("  --minecraft-scientific-apply --experiment <id> --yes");
        Console.WriteLine("  --minecraft-scientific-compare --experiment <id>");
        Console.WriteLine("  --minecraft-scientific-finalize --experiment <id> --yes");
        Console.WriteLine("  --minecraft-scientific-cancel --experiment <id> --yes");
        Console.WriteLine("  --minecraft-scientific-show --experiment <id>");
        Console.WriteLine("  --minecraft-scientific-list");
        Console.WriteLine("  --minecraft-audit --mods <path> [--output <path>] [--target 1.21.1]");
        Console.WriteLine("  --minecraft-profile-dry-run --instance <path> --profile POTATO_COBBLEMON_4GB [--fps ...] [--output <path>]");
        Console.WriteLine("  --minecraft-experiment-dry-run --instance <path> --preset heap-1792 [--output <path>]");
        Console.WriteLine("  --minecraft-apply-profile --instance <path> --profile POTATO_COBBLEMON_4GB [--fps ...] --yes");
        Console.WriteLine("  --minecraft-rollback --instance <path> --yes");
        Console.WriteLine("  --minecraft-quarantine-dry-run --mods <path> [--output <path>]");
        Console.WriteLine("  --minecraft-quarantine-apply --mods <path> --files \"a.jar;b.jar\" --yes [--server-manifest-confirmed]");
        Console.WriteLine("  --minecraft-quarantine-rollback --mods <path> --yes");
        Console.WriteLine("  --minecraft-discover-instances");
        Console.WriteLine("  --minecraft-benchmark [--instance <path>] [--seconds 60] [--wait-seconds 30] [--output <path>]");
        Console.WriteLine("  --minecraft-operational-checklist --mods <path> [--instance <path>] [--fps 30|45|60] [--output <path>]");
        Console.WriteLine("  --minecraft-homologation-report --instance <path> [observacoes] [--benchmark-seconds 60] [--output <path>]");
        Console.WriteLine("    observacoes: --game-opened --menu-reached --menu-seconds N --world-entered --server-entered");
        Console.WriteLine("                 --join-seconds N --playable-720p --average-fps N --minimum-fps N");
        Console.WriteLine("                 --severe-drops --crashed --out-of-memory --notes \"texto\"");
        Console.WriteLine("  --minecraft-self-test");
    }
}
