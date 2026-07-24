using System.Collections.ObjectModel;
using System.Linq;
using ApexTweaker.Models;
using ApexTweaker.Services;
using ApexTweaker.UI.Wpf.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApexTweaker.UI.Wpf.ViewModels;

internal sealed partial class CatalogViewModel : ObservableObject
{
    private readonly WindowsOptimizationService optimizationService = new();

    public ObservableCollection<CatalogRowViewModel> Rows { get; } = new();

    public ObservableCollection<BiosChecklistItem> BiosItems { get; } = new();

    [ObservableProperty]
    private string statusText = "Selecione um preset para analisar.";

    [ObservableProperty]
    private WindowsOptimizationPreset selectedPreset = WindowsOptimizationPreset.GamerSafe;

    public IReadOnlyList<WindowsOptimizationPreset> Presets { get; } =
        Enum.GetValues<WindowsOptimizationPreset>();

    public void LoadBios()
    {
        BiosItems.Clear();
        foreach (var item in BiosChecklistCatalog.Items)
        {
            BiosItems.Add(item);
        }
    }

    public void Analyze()
    {
        Rows.Clear();
        var plan = optimizationService.Analyze(SelectedPreset, WindowsUsageProfile.Unknown);
        foreach (var decision in plan.Decisions.OrderBy(d => d.Rule.Category).ThenBy(d => d.Rule.Name))
        {
            Rows.Add(new CatalogRowViewModel(decision));
        }

        StatusText =
            $"{SelectedPreset}: {plan.Recommended.Count} recomendados, " +
            $"{plan.RequiringConfirmation.Count} confirmacao, {plan.Blocked.Count} bloqueados. " +
            "Auto-Optimize nunca aplica dangerous.*";
    }
}

internal sealed class CatalogRowViewModel
{
    public CatalogRowViewModel(WindowsOptimizationDecision decision)
    {
        Decision = decision;
        Rule = decision.Rule;
    }

    public WindowsOptimizationDecision Decision { get; }

    public WindowsOptimizationRule Rule { get; }

    public string KindLabel => Decision.Kind switch
    {
        OptimizationDecisionKind.Recommended => "Recomendado",
        OptimizationDecisionKind.RequiresConfirmation => "Confirmacao",
        OptimizationDecisionKind.ExperimentalOnly => "Experimental",
        OptimizationDecisionKind.AlreadyConfigured => "Ja aplicado",
        OptimizationDecisionKind.NotApplicable => "N/A",
        OptimizationDecisionKind.Blocked => "Bloqueado",
        _ => Decision.Kind.ToString()
    };

    public string RiskLabel => Rule.Risk switch
    {
        WindowsOptimizationRisk.Safe => "Seguro",
        WindowsOptimizationRisk.Conditional => "Condicional",
        WindowsOptimizationRisk.Experimental => "Experimental",
        WindowsOptimizationRisk.Dangerous => "Perigoso",
        _ => Rule.Risk.ToString()
    };

    public RiskLevel BadgeLevel => Rule.Risk switch
    {
        WindowsOptimizationRisk.Dangerous => RiskLevel.Dangerous,
        WindowsOptimizationRisk.Experimental => RiskLevel.Advanced,
        WindowsOptimizationRisk.Conditional => RiskLevel.Advanced,
        _ => RiskLevel.Safe
    };

    public string Category => Rule.Category;

    public string Name => Rule.Name;

    public string Reason => Decision.Reason;

    public string Impact => string.IsNullOrWhiteSpace(Rule.ExpectedImpact)
        ? Reason
        : Rule.ExpectedImpact;

    public bool RequiresRestart => Rule.RequiresRestart;

    public bool IsDangerous => Rule.Risk == WindowsOptimizationRisk.Dangerous;
}
