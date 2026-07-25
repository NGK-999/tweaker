using System;
using System.Threading.Tasks;
using System.Windows;
using ApexTweaker.Models;
using WpfButton = System.Windows.Controls.Button;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace ApexTweaker.UI.Wpf.Views;

internal sealed class PerformanceStatusItem
{
    public PerformanceStatusItem(string name, string statusLabel, string statusGlyph, string statusKey, string description, string detail)
    {
        Name = name;
        StatusLabel = statusLabel;
        StatusGlyph = statusGlyph;
        StatusKey = statusKey;
        Description = description;
        Detail = detail;
    }

    public string Name { get; }

    public string StatusLabel { get; }

    public string StatusGlyph { get; }

    public string StatusKey { get; }

    public string Description { get; }

    public string Detail { get; }
}

public partial class PerformanceView : WpfUserControl
{
    public event Func<Task>? DisableVbsHvciRequested;

    public event Func<Task>? OptimizeFullscreenRequested;

    public event Func<Task>? CompetitiveModeRequested;

    public PerformanceView()
    {
        InitializeComponent();
        StatusItemsControl.ItemsSource = Array.Empty<PerformanceStatusItem>();
    }

    internal void ApplyProbe(GamingPerformanceProbe probe)
    {
        StatusItemsControl.ItemsSource = BuildStatusItems(probe);
    }

    public void SetBusy(bool busy)
    {
        DisableVbsButton.IsEnabled = !busy;
        OptimizeFullscreenButton.IsEnabled = !busy;
        CompetitiveModeButton.IsEnabled = !busy;
    }

    public void SetValorantStatus(string? valorantExePath)
    {
        ValorantStatusText.Text = string.IsNullOrWhiteSpace(valorantExePath)
            ? "Desliga Fullscreen Optimizations no exe do jogo quando detectado. Valorant nao encontrado; o ajuste geral do Windows ainda pode ser aplicado."
            : $"Desliga Fullscreen Optimizations no exe detectado: {valorantExePath}";
    }

    private static PerformanceStatusItem[] BuildStatusItems(GamingPerformanceProbe probe)
    {
        return
        [
            FromFeature(
                "VBS",
                probe.VbsState,
                enabledIsGood: false,
                enabledMeaning: "Virtualization Based Security ativa (overhead de virtualizacao).",
                disabledMeaning: "VBS desativada.",
                unknownMeaning: "Nao foi possivel ler o estado da VBS."),
            FromFeature(
                "Memory Integrity",
                probe.MemoryIntegrityState,
                enabledIsGood: false,
                enabledMeaning: "HVCI (Memory Integrity) ativa.",
                disabledMeaning: "HVCI desativada.",
                unknownMeaning: "Nao foi possivel ler o estado do HVCI."),
            FromRequested(
                "HAGS",
                probe.HagsState,
                requestedMeaning: "Hardware-Accelerated GPU Scheduling solicitado.",
                notRequestedMeaning: "HAGS nao solicitado.",
                unknownMeaning: "Nao foi possivel ler o estado do HAGS."),
            FromFeature(
                "Game Mode",
                probe.GameModeState,
                enabledIsGood: true,
                enabledMeaning: "Game Mode ativo.",
                disabledMeaning: "Game Mode inativo.",
                unknownMeaning: "Nao foi possivel ler o Game Mode."),
            FromFeature(
                "Game DVR",
                probe.GameDvrState,
                enabledIsGood: false,
                enabledMeaning: "Game DVR/captura ativa; pode gerar overhead.",
                disabledMeaning: "Game DVR desativado (melhor para competitivo).",
                unknownMeaning: "Nao foi possivel ler o Game DVR."),
            FromRebar(probe.ResizableBar)
        ];
    }

    private static PerformanceStatusItem FromFeature(
        string name,
        FeatureState state,
        bool enabledIsGood,
        string enabledMeaning,
        string disabledMeaning,
        string unknownMeaning)
    {
        return state switch
        {
            FeatureState.Enabled => new PerformanceStatusItem(
                name,
                "Ativo",
                enabledIsGood ? "OK" : "!",
                enabledIsGood ? "good" : "warn",
                enabledMeaning,
                $"{name}: Enabled"),
            FeatureState.Disabled => new PerformanceStatusItem(
                name,
                "Inativo",
                enabledIsGood ? "!" : "OK",
                enabledIsGood ? "warn" : "good",
                disabledMeaning,
                $"{name}: Disabled"),
            _ => new PerformanceStatusItem(name, "Desconhecido", "?", "unknown", unknownMeaning, $"{name}: Unknown")
        };
    }

    private static PerformanceStatusItem FromRequested(
        string name,
        RequestedFeatureState state,
        string requestedMeaning,
        string notRequestedMeaning,
        string unknownMeaning)
    {
        return state switch
        {
            RequestedFeatureState.Requested => new PerformanceStatusItem(
                name, "Solicitado", "OK", "good", requestedMeaning, $"{name}: Requested"),
            RequestedFeatureState.NotRequested => new PerformanceStatusItem(
                name, "Nao solicitado", "!", "warn", notRequestedMeaning, $"{name}: NotRequested"),
            _ => new PerformanceStatusItem(name, "Desconhecido", "?", "unknown", unknownMeaning, $"{name}: Unknown")
        };
    }

    private static PerformanceStatusItem FromRebar(ResizableBarProbe rebar)
    {
        var checklist = rebar.Checklist is null
            ? "Checklist BIOS: Above 4G Decoding + Resizable BAR Support."
            : $"{rebar.Checklist.Title}: {rebar.Checklist.Guidance}";

        return rebar.Status switch
        {
            ResizableBarStatus.Enabled => new PerformanceStatusItem(
                "ReBAR", "Ativo", "OK", "good", rebar.Summary, checklist),
            ResizableBarStatus.DisabledOrUnsupported => new PerformanceStatusItem(
                "ReBAR", "Inativo", "!", "warn", rebar.Summary, checklist),
            _ => new PerformanceStatusItem(
                "ReBAR", "Desconhecido", "?", "unknown", rebar.Summary, checklist)
        };
    }

    private void AdvancedButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { DataContext: PerformanceStatusItem item })
        {
            MessageBox.Show(item.Detail, $"{item.Name} — detalhes", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void DisableVbsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DisableVbsHvciRequested is not null)
        {
            await DisableVbsHvciRequested.Invoke();
        }
    }

    private async void OptimizeFullscreenButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (OptimizeFullscreenRequested is not null)
        {
            await OptimizeFullscreenRequested.Invoke();
        }
    }

    private async void CompetitiveModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CompetitiveModeRequested is not null)
        {
            await CompetitiveModeRequested.Invoke();
        }
    }
}
