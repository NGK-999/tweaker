using System.IO;
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
                foreach (var line in MinecraftSelfTest.Run())
                {
                    Console.WriteLine(line);
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
                var plan = new MinecraftProfileService().PlanProfile(instance, profile);
                var path = new MinecraftReportService().WriteProfilePlan(
                    plan,
                    applied: false,
                    outputDirectory: GetValue(args, "--output"));
                Console.WriteLine($"DRY_RUN: {plan.Changes.Count(change => change.WillWrite)} alteracoes propostas.");
                Console.WriteLine(path);
                WriteStatusFile(args, "PROFILE_DRY_RUN_OK", path);
                return true;
            }

            if (HasFlag(args, "--minecraft-apply-profile"))
            {
                RequireConfirmation(args);
                var instance = RequireValue(args, "--instance");
                var profile = ParseProfile(GetValue(args, "--profile") ?? "EXTREME_4GB");
                var result = new MinecraftProfileService().ApplyProfile(instance, profile);
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
                        $"{candidate.FileName} | {candidate.Risk} | recomendado={candidate.RecommendedForExtreme} | {candidate.Reason}");
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
                var result = new MinecraftQuarantineService().Apply(plan, fileList);
                Console.WriteLine($"Quarentena: {result.QuarantineDirectory}");
                Console.WriteLine($"Backup: {result.BackupDirectory}");
                Console.WriteLine($"Manifesto: {result.ManifestPath}");
                WriteStatusFile(args, "QUARANTINE_APPLY_OK", result.ManifestPath);
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
            "COBBLEMONSERVERCLIENT" => MinecraftProfileKind.CobblemonServerClient,
            "BENCHMARK" => MinecraftProfileKind.Benchmark,
            _ => throw new ArgumentException($"Perfil desconhecido: {value}")
        };
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
        Console.WriteLine("  --minecraft-audit --mods <path> [--output <path>] [--target 1.21.1]");
        Console.WriteLine("  --minecraft-profile-dry-run --instance <path> --profile EXTREME_4GB [--output <path>]");
        Console.WriteLine("  --minecraft-apply-profile --instance <path> --profile EXTREME_4GB --yes");
        Console.WriteLine("  --minecraft-rollback --instance <path> --yes");
        Console.WriteLine("  --minecraft-quarantine-dry-run --mods <path> [--output <path>]");
        Console.WriteLine("  --minecraft-quarantine-apply --mods <path> --files \"a.jar;b.jar\" --yes");
        Console.WriteLine("  --minecraft-quarantine-rollback --mods <path> --yes");
        Console.WriteLine("  --minecraft-discover-instances");
        Console.WriteLine("  --minecraft-benchmark [--instance <path>] [--seconds 60] [--wait-seconds 30] [--output <path>]");
        Console.WriteLine("  --minecraft-self-test");
    }
}
