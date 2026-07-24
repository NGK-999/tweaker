using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ApexTweaker.Services;
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
    private const string DeviceGuardPath = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
    private const string HvciPath = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
    private const string GraphicsDriversPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string GameBarPath = @"Software\Microsoft\GameBar";
    private const string GameConfigStorePath = @"System\GameConfigStore";

    public event Func<Task>? DisableVbsHvciRequested;

    public event Func<Task>? OptimizeFullscreenRequested;

    public event Func<Task>? CompetitiveModeRequested;

    public PerformanceView()
    {
        InitializeComponent();
        StatusItemsControl.ItemsSource = BuildStatusItems();
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
            ? "Ajusta HAGS e caminho de tela cheia exclusiva. Valorant nao detectado; o ajuste geral do Windows ainda sera aplicado."
            : $"Ajusta HAGS e caminho de tela cheia exclusiva. Valorant detectado: {valorantExePath}";
    }

    // ponytail: probes de leitura reaproveitam as mesmas chaves de registro que
    // SystemDiagnosticsService ja consulta (nao editado; apenas lido aqui).
    // ReBAR nao tem probe confiavel sem WMI elevado, entao fica "Unknown" com checklist manual.
    private static PerformanceStatusItem[] BuildStatusItems()
    {
        return
        [
            BuildRequestedStateItem(
                "VBS",
                Registry.LocalMachine, DeviceGuardPath, "EnableVirtualizationBasedSecurity",
                expectedValue: 1,
                enabledMeaning: "Virtualization Based Security ativa (overhead de virtualização).",
                disabledMeaning: "VBS desativada.",
                unknownMeaning: "Não foi possível ler o estado da VBS neste Windows."),
            BuildRequestedStateItem(
                "Memory Integrity",
                Registry.LocalMachine, HvciPath, "Enabled",
                expectedValue: 1,
                enabledMeaning: "HVCI (Memory Integrity) ativa.",
                disabledMeaning: "HVCI desativada.",
                unknownMeaning: "Não foi possível ler o estado do HVCI."),
            BuildRequestedStateItem(
                "HAGS",
                Registry.LocalMachine, GraphicsDriversPath, "HwSchMode",
                expectedValue: 2,
                enabledMeaning: "Hardware-Accelerated GPU Scheduling solicitado.",
                disabledMeaning: "HAGS desativado.",
                unknownMeaning: "Não foi possível ler o estado do HAGS."),
            BuildRequestedStateItem(
                "Game Mode / GameDVR",
                Registry.CurrentUser, GameConfigStorePath, "GameDVR_Enabled",
                expectedValue: 0,
                enabledMeaning: "Game DVR desativado (recomendado para competitivo).",
                disabledMeaning: "Game DVR ativo; pode gerar overhead durante a partida.",
                unknownMeaning: "Não foi possível ler o estado do Game DVR."),
            new PerformanceStatusItem(
                "ReBAR",
                "Desconhecido",
                "?",
                "unknown",
                "Resizable BAR depende de configuração de BIOS; sem leitura confiável via software.",
                "Verifique no BIOS: habilite 'Above 4G Decoding' e 'Resizable BAR Support'. " +
                "Confirme depois em Gerenciador de Dispositivos > Placas de vídeo > Propriedades, " +
                "ou executando dxdiag e checando a seção de exibição.")
        ];
    }

    private static PerformanceStatusItem BuildRequestedStateItem(
        string name,
        RegistryKey root,
        string path,
        string valueName,
        int expectedValue,
        string enabledMeaning,
        string disabledMeaning,
        string unknownMeaning)
    {
        var value = RegistryService.GetDword(root, path, valueName, -1);
        if (value < 0)
        {
            return new PerformanceStatusItem(name, "Desconhecido", "?", "unknown", unknownMeaning, $"{root}\\{path}\\{valueName}: indisponível");
        }

        var isExpected = value == expectedValue;
        var label = isExpected ? "Ativo" : "Inativo";
        var glyph = isExpected ? "✓" : "✕";
        var statusKey = isExpected ? "good" : "warn";
        var description = isExpected ? enabledMeaning : disabledMeaning;
        return new PerformanceStatusItem(name, label, glyph, statusKey, description, $"{root}\\{path}\\{valueName} = {value}");
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
