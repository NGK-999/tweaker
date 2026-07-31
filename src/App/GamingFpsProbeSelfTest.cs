using ApexTweaker.Application.Optimizations;
using ApexTweaker.Models;
using ApexTweaker.Services;

namespace ApexTweaker;

internal static class GamingFpsProbeSelfTest
{
    public static int Run()
    {
        var failures = 0;

        void Check(bool condition, string message)
        {
            if (!condition)
            {
                Console.Error.WriteLine("FAIL: " + message);
                failures++;
                return;
            }

            Console.WriteLine("PASS: " + message);
        }

        var optimizationService = new WindowsOptimizationService();
        var diagnostics = new SystemDiagnosticsService();

        var probe = optimizationService.CaptureGamingPerformanceProbe();
        var report = diagnostics.BuildDiagnosticReport();

        Check(Enum.IsDefined(probe.VbsState), "probe exposes VBS state");
        Check(Enum.IsDefined(probe.MemoryIntegrityState), "probe exposes Memory Integrity/HVCI state");
        Check(Enum.IsDefined(probe.HagsState), "probe exposes HAGS requested state");
        Check(Enum.IsDefined(probe.GameModeState), "probe exposes Game Mode state");
        Check(Enum.IsDefined(probe.GameDvrState), "probe exposes Game DVR state");
        Check(probe.ResizableBar.Checklist?.Id == "bios.resizable-bar", "probe points to BIOS checklist for ReBAR");
        Check(!string.IsNullOrWhiteSpace(probe.ResizableBar.Summary), "probe returns ReBAR summary");

        Check(report.Any(line => line.Contains("HAGS solicitado:", StringComparison.OrdinalIgnoreCase)),
            "diagnostic report mentions HAGS");
        Check(report.Any(line => line.Contains("Memory Integrity / HVCI:", StringComparison.OrdinalIgnoreCase)),
            "diagnostic report mentions HVCI");
        Check(report.Any(line => line.Contains("Resizable BAR:", StringComparison.OrdinalIgnoreCase)),
            "diagnostic report mentions ReBAR");

        var rules = WindowsOptimizationCatalog.Rules;
        Check(rules.Any(rule => rule.Id == "fps.vbs-hvci" && !rule.MayApplyAutomatically && rule.RequiresRestart),
            "catalog blocks auto-apply for fps.vbs-hvci and marks restart");
        Check(rules.Any(rule => rule.Id == "fps.hags-status"), "catalog contains fps.hags-status");
        Check(rules.Any(rule => rule.Id == "fps.rebar-checklist"), "catalog contains fps.rebar-checklist");
        Check(rules.Any(rule => rule.Id == "fps.fso-per-game"), "catalog contains fps.fso-per-game");
        Check(rules.Any(rule => rule.Id == "fps.competitive-overlays"), "catalog contains fps.competitive-overlays");
        Check(rules.Any(rule => rule.Id == "ctt.essential-bundle"), "catalog contains ctt.essential-bundle");
        Check(rules.Any(rule => rule.Id == "ctt.advanced-bundle"), "catalog contains ctt.advanced-bundle");
        Check(rules.Any(rule => rule.Id == "dangerous.ctt-disable-bitlocker" && !rule.MayApplyAutomatically),
            "catalog blocks auto-apply for dangerous.ctt-disable-bitlocker");
        Check(rules.First(rule => rule.Id == "ctt.essential-bundle").MayApplyAutomatically,
            "ctt.essential-bundle may apply automatically when Safe");
        Check(!rules.First(rule => rule.Id == "ctt.advanced-bundle").MayApplyAutomatically,
            "ctt.advanced-bundle does not auto-apply (Conditional)");

        if (failures == 0)
        {
            Console.WriteLine("Gaming FPS probe self-test: ALL PASS");
            return 0;
        }

        Console.Error.WriteLine($"Gaming FPS probe self-test: {failures} failure(s)");
        return 1;
    }
}
