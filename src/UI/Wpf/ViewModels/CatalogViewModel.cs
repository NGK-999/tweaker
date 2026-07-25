using System;
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

    [ObservableProperty]
    private CatalogFeedbackKind feedbackKind = CatalogFeedbackKind.Idle;

    [ObservableProperty]
    private string feedbackTitle = string.Empty;

    [ObservableProperty]
    private string feedbackDetail = string.Empty;

    [ObservableProperty]
    private bool usageUnknown = true;

    public IReadOnlyList<WindowsOptimizationPreset> Presets { get; } =
        Enum.GetValues<WindowsOptimizationPreset>();

    public bool ShowEmptyPanel => FeedbackKind == CatalogFeedbackKind.Empty;

    public bool ShowPartialPanel => FeedbackKind == CatalogFeedbackKind.Partial;

    public bool ShowErrorPanel => FeedbackKind == CatalogFeedbackKind.Error;

    public bool ShowRulesList => FeedbackKind is CatalogFeedbackKind.Ready or CatalogFeedbackKind.Partial;

    public bool ShowGoToAutoCta =>
        FeedbackKind is CatalogFeedbackKind.Ready or CatalogFeedbackKind.Partial or CatalogFeedbackKind.Empty;

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
        UsageUnknown = true;

        try
        {
            // Usage profile UI not wired yet — Unknown is intentional Partial signal.
            var plan = optimizationService.Analyze(SelectedPreset, WindowsUsageProfile.Unknown);
            foreach (var decision in plan.Decisions.OrderBy(d => d.Rule.Category).ThenBy(d => d.Rule.Name))
            {
                Rows.Add(new CatalogRowViewModel(decision));
            }

            ApplyFeedback(rowCount: Rows.Count, analyzeFailed: false, usageUnknown: true);

            StatusText =
                $"{SelectedPreset}: {plan.Recommended.Count} recomendados, " +
                $"{plan.RequiringConfirmation.Count} confirmacao, {plan.Blocked.Count} bloqueados. " +
                "Esta tela so analisa; aplicacao e no Dashboard (Auto-Optimize).";
        }
        catch (Exception ex)
        {
            Rows.Clear();
            ApplyFeedback(rowCount: 0, analyzeFailed: true, usageUnknown: true);
            StatusText = "Falha ao analisar o catalogo.";
            FeedbackDetail =
                "Nao foi possivel gerar o plano. Nenhuma otimizacao foi aplicada. " +
                "Detalhe: " + ex.Message;
        }

        OnPropertyChanged(nameof(ShowEmptyPanel));
        OnPropertyChanged(nameof(ShowPartialPanel));
        OnPropertyChanged(nameof(ShowErrorPanel));
        OnPropertyChanged(nameof(ShowRulesList));
        OnPropertyChanged(nameof(ShowGoToAutoCta));
    }

    private void ApplyFeedback(int rowCount, bool analyzeFailed, bool usageUnknown)
    {
        UsageUnknown = usageUnknown;
        FeedbackKind = CatalogFeedbackState.Resolve(rowCount, analyzeFailed, usageUnknown);

        switch (FeedbackKind)
        {
            case CatalogFeedbackKind.Empty:
                FeedbackTitle = "Nenhuma regra neste resultado";
                FeedbackDetail =
                    "A analise nao retornou itens. Tente outro preset ou analise de novo. " +
                    "Nenhuma alteracao foi aplicada nesta tela.";
                break;
            case CatalogFeedbackKind.Partial:
                FeedbackTitle = "Uso do PC desconhecido";
                FeedbackDetail =
                    "Recomendacoes conservadoras: o perfil de uso ainda nao foi informado. " +
                    "Revise a lista abaixo. Para aplicar otimizacoes, va ao Dashboard e use Auto-Optimize.";
                break;
            case CatalogFeedbackKind.Error:
                FeedbackTitle = "Falha na analise";
                // Detail set by caller when exception message available.
                if (string.IsNullOrWhiteSpace(FeedbackDetail))
                {
                    FeedbackDetail = "A analise falhou. Nenhuma otimizacao foi aplicada.";
                }

                break;
            case CatalogFeedbackKind.Ready:
                FeedbackTitle = string.Empty;
                FeedbackDetail = string.Empty;
                break;
            default:
                FeedbackTitle = string.Empty;
                FeedbackDetail = string.Empty;
                break;
        }
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
