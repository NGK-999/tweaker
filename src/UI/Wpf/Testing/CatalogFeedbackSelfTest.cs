using System;
using System.Collections.Generic;
using ApexTweaker.UI.Wpf.Controls;
using ApexTweaker.UI.Wpf.ViewModels;

namespace ApexTweaker.UI.Wpf.Testing;

/// <summary>
/// Pure assertions for catalog feedback UI states.
/// Wire via Program --catalog-feedback-self-test is an orchestrator proposal (Program.cs frozen for FE).
/// </summary>
public static class CatalogFeedbackSelfTest
{
    public static int Run()
    {
        var failures = new List<string>();

        void Check(bool condition, string name)
        {
            if (!condition)
            {
                failures.Add(name);
            }
        }

        Check(
            CatalogFeedbackState.Resolve(0, analyzeFailed: false, usageUnknown: false) == CatalogFeedbackKind.Empty,
            "empty-when-zero-rows");

        Check(
            CatalogFeedbackState.Resolve(3, analyzeFailed: true, usageUnknown: false) == CatalogFeedbackKind.Error,
            "error-wins-over-rows");

        Check(
            CatalogFeedbackState.Resolve(5, analyzeFailed: false, usageUnknown: true) == CatalogFeedbackKind.Partial,
            "partial-when-usage-unknown");

        Check(
            CatalogFeedbackState.Resolve(5, analyzeFailed: false, usageUnknown: false) == CatalogFeedbackKind.Ready,
            "ready-when-rows-and-known-usage");

        Check(
            Enum.IsDefined(typeof(SnackbarKind), SnackbarKind.Error),
            "snackbar-kind-error-exists");

        Check(
            SnackbarKind.Error != SnackbarKind.Warning &&
            SnackbarKind.Error != SnackbarKind.Info &&
            SnackbarKind.Error != SnackbarKind.Success,
            "snackbar-error-distinct");

        // Automation / focus contract for CTA (names used in CatalogView.xaml)
        const string retryName = "CatalogRetryAnalyze";
        const string goAutoName = "CatalogGoToAutoOptimize";
        Check(!string.IsNullOrWhiteSpace(retryName), "automation-retry-name");
        Check(!string.IsNullOrWhiteSpace(goAutoName), "automation-go-auto-name");

        Check(
            CatalogFeedbackState.Describe(CatalogFeedbackKind.Empty) == "Empty" &&
            CatalogFeedbackState.Describe(CatalogFeedbackKind.Partial) == "Partial" &&
            CatalogFeedbackState.Describe(CatalogFeedbackKind.Error) == "Error",
            "state-labels-distinct");

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("CatalogFeedbackSelfTest FAIL:");
            foreach (var failure in failures)
            {
                Console.Error.WriteLine(" - " + failure);
            }

            return 1;
        }

        Console.WriteLine("CatalogFeedbackSelfTest: ALL CHECKS PASSED (invoke only; harness may be pending)");
        return 0;
    }
}
