using System;
using System.Linq;
using ApexTweaker.Application.Optimizations;
using ApexTweaker.Models;
using ApexTweaker.Services;

namespace ApexTweaker;

internal static class MarketCoverageSelfTest
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
            }
            else
            {
                Console.WriteLine("PASS: " + message);
            }
        }

        var rules = WindowsOptimizationCatalog.Rules;
        Check(rules.Count >= 20, $"catalog size >= 20 (actual {rules.Count})");
        Check(rules.Any(r => r.Id.StartsWith("utility.", StringComparison.Ordinal)), "utility.* rules present");
        Check(rules.Any(r => r.Id.StartsWith("ui.", StringComparison.Ordinal)), "ui.* rules present");
        Check(rules.Any(r => r.Id.StartsWith("memory.", StringComparison.Ordinal)), "memory.* rules present");
        Check(rules.Any(r => r.Id.StartsWith("network.", StringComparison.Ordinal)), "network.* rules present");
        Check(rules.Any(r => r.Id.StartsWith("debloat.", StringComparison.Ordinal)), "debloat.* rules present");
        Check(rules.Any(r => r.Id.StartsWith("bios.", StringComparison.Ordinal)), "bios.* rules present");
        Check(rules.Count(r => r.Risk == WindowsOptimizationRisk.Dangerous) >= 5, "dangerous rules >= 5");
        Check(rules.Where(r => r.Risk == WindowsOptimizationRisk.Dangerous).All(r => !r.MayApplyAutomatically),
            "dangerous never MayApplyAutomatically");

        Check(BiosChecklistCatalog.Items.Count >= 5, "BIOS checklist items >= 5");

        var utilities = new MarketUtilitiesService();
        var clean = utilities.CleanTemporaryFiles(execute: false);
        Check(clean.Count > 0, "clean temp dry-run returns log");
        var trim = utilities.TrimSolidStateVolumes(execute: false);
        Check(trim.Count > 0, "trim dry-run returns log");
        var repair = utilities.PlanOrRunSystemFileRepair(execute: false);
        Check(repair.Any(line => line.Contains("DISM", StringComparison.OrdinalIgnoreCase)), "repair dry-run mentions DISM");
        var guidance = utilities.GetBufferbloatGuidance();
        Check(guidance.Count >= 3, "bufferbloat guidance present");

        var service = new WindowsOptimizationService();
        var plan = service.Analyze(WindowsOptimizationPreset.GamerSafe, WindowsUsageProfile.Unknown);
        Check(plan.Decisions.Count > 0, "Analyze GamerSafe returns decisions");
        Check(plan.Decisions.Where(d => d.Rule.Risk == WindowsOptimizationRisk.Dangerous)
                .All(d => d.Kind is OptimizationDecisionKind.Blocked
                    or OptimizationDecisionKind.RequiresConfirmation
                    or OptimizationDecisionKind.ExperimentalOnly),
            "dangerous decisions are not Recommended");

        if (failures == 0)
        {
            Console.WriteLine("Market coverage self-test: ALL PASS");
            return 0;
        }

        Console.Error.WriteLine($"Market coverage self-test: {failures} failure(s)");
        return 1;
    }
}
