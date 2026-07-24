using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;
using Microsoft.Win32;

namespace ApexTweaker.Services;

/// <summary>
/// B1 market utilities: temp clean, TRIM, SFC/DISM repair planning, Storage Sense.
/// Destructive clean is scoped; repair commands honor <paramref name="execute"/> (false = dry-run/report only).
/// </summary>
internal sealed class MarketUtilitiesService
{
    private readonly CommandRunner commandRunner;

    public MarketUtilitiesService(CommandRunner? commandRunner = null)
    {
        this.commandRunner = commandRunner ?? new CommandRunner();
    }

    public IReadOnlyList<string> CleanTemporaryFiles(bool execute)
    {
        var log = new List<string> { execute ? "Limpeza: removendo temporarios seguros." : "Limpeza (dry-run): inventariando temporarios." };
        var targets = new[]
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
        };

        long bytes = 0;
        var fileCount = 0;
        foreach (var root in targets.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in SafeEnumerateFiles(root))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (!info.Exists || info.Length == 0)
                    {
                        continue;
                    }

                    // Skip very new files (likely in use by this process).
                    if (info.LastWriteTimeUtc > DateTime.UtcNow.AddMinutes(-5))
                    {
                        continue;
                    }

                    bytes += info.Length;
                    fileCount++;
                    if (execute)
                    {
                        try
                        {
                            info.Delete();
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                        {
                            // ignore locked files
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // ignore
                }
            }
        }

        log.Add($"{(execute ? "Removidos/tentados" : "Candidatos")}: {fileCount} arquivos (~{bytes / (1024 * 1024)} MB).");
        return log;
    }

    public IReadOnlyList<string> TrimSolidStateVolumes(bool execute)
    {
        var log = new List<string> { execute ? "Storage: solicitando TRIM (defrag /L)." : "Storage (dry-run): planejando TRIM." };
        var result = execute
            ? commandRunner.Run("defrag", "C: /L")
            : new CommandResult(0, "dry-run: defrag C: /L", string.Empty);

        log.Add(result.ExitCode == 0
            ? (execute ? "TRIM solicitado no volume C:." : "TRIM seria solicitado no volume C:.")
            : $"TRIM nao concluido: {result.Output}");
        return log;
    }

    public IReadOnlyList<string> PlanOrRunSystemFileRepair(bool execute)
    {
        var log = new List<string>
        {
            execute
                ? "Repair: executando DISM CheckHealth + SFC (pode demorar)."
                : "Repair (dry-run): comandos planejados sem mutacao."
        };

        if (!execute)
        {
            log.Add("Planejado: DISM /Online /Cleanup-Image /CheckHealth");
            log.Add("Planejado: sfc /scannow (somente sob confirmacao do usuario)");
            return log;
        }

        var dism = commandRunner.Run("DISM", "/Online /Cleanup-Image /CheckHealth");
        log.Add(dism.ExitCode == 0 ? "DISM CheckHealth OK." : $"DISM: {dism.Output}");
        var sfc = commandRunner.Run("sfc", "/scannow");
        log.Add(sfc.ExitCode == 0 ? "SFC concluido." : $"SFC: {sfc.Output}");
        return log;
    }

    public IReadOnlyList<string> DisableStorageSense(bool execute)
    {
        var log = new List<string> { "Storage Sense: desativar limpeza automatica agressiva do Windows." };
        const string path = @"Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy";
        if (!execute)
        {
            log.Add("Dry-run: 01=0 em StoragePolicy.");
            return log;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(path, writable: true)
                ?? throw new InvalidOperationException("Nao foi possivel abrir StoragePolicy.");
            key.SetValue("01", 0, RegistryValueKind.DWord);
            log.Add("Storage Sense desativado (HKCU StoragePolicy 01=0).");
        }
        catch (Exception ex)
        {
            log.Add($"Falha Storage Sense: {ex.Message}");
        }

        return log;
    }

    public IReadOnlyList<string> GetBufferbloatGuidance()
    {
        return
        [
            "Bufferbloat: Apex nao altera firmware do roteador.",
            "Recomendado: teste em https://www.waveform.com/tools/bufferbloat",
            "Se grade D/F: ative SQM/QoS no roteador (Cake/fq_codel) ou limite upload ~90% do link.",
            "No PC: use ApplyAdvancedNetworkTweaks para NIC (RSS/interrupt moderation) — nao substitui SQM."
        ];
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files.Take(5000))
            {
                yield return file;
            }

            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(dir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var child in children.Take(200))
            {
                stack.Push(child);
            }
        }
    }
}
