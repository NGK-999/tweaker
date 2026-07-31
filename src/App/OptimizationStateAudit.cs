using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using ApexTweaker.Services;

namespace ApexTweaker;

/// <summary>
/// Read-only audit of expected optimization markers vs current machine state.
/// Does not mutate Windows. Exit code 0 always when audit completes; prints PRESENT/MISSING.
/// </summary>
internal static class OptimizationStateAudit
{
    public static int Run()
    {
        var rows = new List<AuditRow>();
        var engine = new OptimizationEngine();

        void Check(string package, string id, bool present, string detail) =>
            rows.Add(new AuditRow(package, id, present ? "PRESENT" : "MISSING", detail));

        var freshness = engine.GetOptimizationFreshness();
        Check(
            "AutoGate",
            "LegacyMarkers",
            engine.CheckLegacyOptimizationMarkers(),
            $"freshness={freshness}; revision={engine.ReadInstalledOptimizationRevision()}/{OptimizationEngine.CurrentOptimizationRevision}");
        Check(
            "AutoGate",
            "RevisionCurrent",
            freshness == OptimizationEngine.OptimizationFreshness.Current,
            freshness == OptimizationEngine.OptimizationFreshness.NeedsUpgrade
                ? "marcadores antigos — Auto-Tuning deve oferecer Atualizar gaps"
                : freshness.ToString());

        Check("Background", "AllowAutoGameMode=1", ReadDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode") == 1, DetailDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode"));
        Check("Background", "AppCaptureEnabled=0", ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled") == 0, DetailDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled"));
        Check("Background", "Win32PrioritySeparation=38", ReadDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation") == 38, DetailDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation"));
        Check("Background", "GameDVR_Enabled=0", ReadDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled") == 0, DetailDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled"));

        Check("UI", "MenuShowDelay=0", string.Equals(ReadString(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay"), "0", StringComparison.Ordinal), $"actual={ReadString(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay") ?? "<null>"}");
        Check("UI", "MinAnimate=0", string.Equals(ReadString(Registry.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate"), "0", StringComparison.Ordinal), $"actual={ReadString(Registry.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate") ?? "<null>"}");
        Check("UI", "EnableTransparency=0", ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency") == 0, DetailDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency"));
        Check("UI", "ToastEnabled=0", ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled") == 0, DetailDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled"));

        Check("Input", "MouseSpeed=0", string.Equals(ReadString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed"), "0", StringComparison.Ordinal), $"actual={ReadString(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed") ?? "<null>"}");

        Check("Memory", "DisablePagingExecutive=1", ReadDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive") == 1, DetailDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive"));
        Check("Memory", "LargeSystemCache=0", ReadDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache") == 0, DetailDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache"));

        var doConfig = ReadDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config", "DODownloadMode");
        var doPolicy = ReadDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode");
        Check("Policy", "DODownloadMode=0", doConfig == 0 || doPolicy == 0, $"config={Fmt(doConfig)}; policy={Fmt(doPolicy)}");
        Check("Policy", "DisableWindowsConsumerFeatures=1", ReadDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures") == 1, DetailDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures"));

        Check("CTT-Essential", "AdvertisingInfo.Enabled=0", ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled") == 0, DetailDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled"));
        Check("CTT-Essential", "EnableActivityFeed=0", ReadDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed") == 0, DetailDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed"));
        Check("CTT-Essential", "TaskbarDa=0", ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa") == 0, DetailDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa"));
        Check("CTT-Essential", "DisableWpbtExecution=1", ReadDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "DisableWpbtExecution") == 1, DetailDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "DisableWpbtExecution"));
        Check("CTT-Essential", "PreventDeviceMetadataFromNetwork=1", ReadDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork") == 1, DetailDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork"));
        Check("CTT-Essential", "TaskbarEndTask=1", ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask") == 1, DetailDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask"));
        Check("CTT-Essential", "HibernateEnabled=0", ReadDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled") == 0, DetailDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled"));
        var location = ReadString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "Value");
        Check("CTT-Essential", "Location=Deny", string.Equals(location, "Deny", StringComparison.OrdinalIgnoreCase), $"actual={location ?? "<null>"}");

        Check("CTT-Advanced", "GlobalUserDisabled=1", ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled") == 1, DetailDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled"));
        var disabledComponents = ReadDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents");
        Check("CTT-Advanced", "IPv4PreferredOrOff", disabledComponents is 32 or 255, DetailDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents"));
        Check("CTT-Advanced", "DisableCoInstallers=1", ReadDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Device Installer", "DisableCoInstallers") == 1, DetailDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Device Installer", "DisableCoInstallers"));

        foreach (var serviceName in new[] { "DiagTrack", "dmwappushservice", "MapsBroker", "lfsvc", "CscService" })
        {
            var start = ReadDword(Registry.LocalMachine, $@"SYSTEM\CurrentControlSet\Services\{serviceName}", "Start");
            var ok = start is 4 || (serviceName == "MapsBroker" && start is 3 or 4) || start is null;
            var label = start switch
            {
                2 => "Auto",
                3 => "Manual",
                4 => "Disabled",
                null => "ABSENT",
                _ => $"Start={start}"
            };
            Check("Services", serviceName, ok, label);
        }

        var throttling = ReadDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex");
        // DWORD 0xFFFFFFFF is stored as -1 / uint.MaxValue depending on reader.
        Check("Network", "NetworkThrottlingIndex=-1", throttling is -1 or unchecked((int)0xFFFFFFFF), DetailDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex"));

        var present = rows.Count(r => r.Status == "PRESENT");
        var missing = rows.Count(r => r.Status == "MISSING");

        Console.WriteLine("=== APEX OPTIMIZATION STATE AUDIT (READ-ONLY) ===");
        Console.WriteLine($"Host: {Environment.MachineName} | {DateTimeOffset.Now:o}");
        Console.WriteLine($"PRESENT={present}  MISSING={missing}  TOTAL={rows.Count}");
        Console.WriteLine();

        foreach (var group in rows.GroupBy(r => r.Package))
        {
            var p = group.Count(r => r.Status == "PRESENT");
            var m = group.Count(r => r.Status == "MISSING");
            Console.WriteLine($"[{group.Key}] {p} present / {m} missing");
        }

        Console.WriteLine();
        Console.WriteLine("--- MISSING ---");
        foreach (var row in rows.Where(r => r.Status == "MISSING"))
        {
            Console.WriteLine($"MISS  {row.Package,-14} {row.Id,-40} {row.Detail}");
        }

        Console.WriteLine();
        Console.WriteLine("--- PRESENT ---");
        foreach (var row in rows.Where(r => r.Status == "PRESENT"))
        {
            Console.WriteLine($"OK    {row.Package,-14} {row.Id,-40} {row.Detail}");
        }

        Console.WriteLine();
        Console.WriteLine(freshness == OptimizationEngine.OptimizationFreshness.NeedsUpgrade
            ? "HINT: rode o Dashboard → Atualizar otimizacoes (gaps) como Administrador (nao --demo)."
            : freshness == OptimizationEngine.OptimizationFreshness.Current
                ? "HINT: revisao atual; use Reaplicar se quiser forcar, ou Modulos para CTT Advanced."
                : "HINT: rode Auto-Tuning como Administrador (nao --demo).");

        return 0;
    }

    private static int? ReadDword(RegistryKey root, string path, string name) =>
        RegistryService.TryReadDword(root, path, name, out var value) ? value : null;

    private static string? ReadString(RegistryKey root, string path, string name) =>
        RegistryService.TryReadString(root, path, name, out var value) ? value : null;

    private static string DetailDword(RegistryKey root, string path, string name) =>
        $"actual={Fmt(ReadDword(root, path, name))}";

    private static string Fmt(int? value) => value?.ToString() ?? "<null>";

    private sealed record AuditRow(string Package, string Id, string Status, string Detail);
}
