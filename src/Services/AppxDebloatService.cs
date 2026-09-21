using System;
using System.Collections.Generic;
using System.Linq;
using ApexTweaker.Infrastructure;
using ApexTweaker.Models;

namespace ApexTweaker.Services;

internal sealed record BloatwareApp(
    string AppId,
    string FriendlyName,
    string Description,
    bool IsSafe);

/// <summary>
/// Service responsible for identifying and removing AppX/UWP bloatware packages safely,
/// with full respect for --demo mode and dry-run reporting.
/// </summary>
internal sealed class AppxDebloatService
{
    private readonly CommandRunner commandRunner;

    public AppxDebloatService(CommandRunner? commandRunner = null)
    {
        this.commandRunner = commandRunner ?? new CommandRunner();
    }

    public static IReadOnlyList<BloatwareApp> KnownBloatwareApps { get; } =
    [
        new("Microsoft.3DBuilder", "3D Builder", "Modelador 3D b\u00E1sico", true),
        new("Microsoft.Microsoft3DViewer", "3D Viewer", "Visualizador de modelos 3D", true),
        new("Microsoft.549981C3F5F10", "Cortana", "Assistente de voz Cortana (descontinuada)", true),
        new("Microsoft.BingFinance", "Bing Finan\u00E7as", "Not\u00EDcias financeiras e a\u00E7\u00F5es via Bing", true),
        new("Microsoft.BingNews", "Bing Not\u00EDcias", "Agregador de not\u00EDcias via Bing", true),
        new("Microsoft.BingSports", "Bing Esportes", "Not\u00EDcias e resultados esportivos", true),
        new("Microsoft.BingWeather", "Bing Clima", "Previs\u00E3o do tempo via Bing", true),
        new("Clipchamp.Clipchamp", "Clipchamp", "Editor de v\u00EDdeo online da Microsoft", true),
        new("Microsoft.GetHelp", "Obter Ajuda", "Aplicativo de suporte e ajuda", true),
        new("Microsoft.Getstarted", "Dicas / Primeiros Passos", "Dicas e introdu\u00E7\u00E3o do Windows", true),
        new("Microsoft.Messaging", "Mensagens", "Aplicativo de mensagens legadas", true),
        new("Microsoft.MicrosoftOfficeHub", "Office Hub / 365", "Atalho promocional do Microsoft 365", true),
        new("Microsoft.MicrosoftSolitaireCollection", "Solitaire Collection", "Cole\u00E7\u00E3o de jogos de paci\u00EAncia", true),
        new("Microsoft.MixedReality.Portal", "Mixed Reality Portal", "Portal de realidade mista", true),
        new("Microsoft.People", "Pessoas", "Agenda de contatos do Windows", true),
        new("Microsoft.WindowsAlarms", "Alarmes e Rel\u00F3gio", "Despertador e cron\u00F4metro b\u00E1sico", true),
        new("Microsoft.WindowsFeedbackHub", "Hub de Feedback", "Envio de telemetria e coment\u00E1rios", true),
        new("Microsoft.WindowsMaps", "Mapas", "Mapas e rotas offline", true),
        new("Microsoft.YourPhone", "V\u00EDnculo com Celular", "Sincroniza\u00E7\u00E3o com smartphone", true),
        new("Microsoft.ZuneMusic", "Groove M\u00FAsica", "Reprodutor de \u00E1udio antigo", true),
        new("Microsoft.ZuneVideo", "Filmes e TV", "Reprodutor de v\u00EDdeo padr\u00E3o", true),
        new("XP9CXNGPPJ97XX", "Microsoft Copilot App", "Aplicativo UWP do Copilot integrado", true),
        new("Microsoft.Windows.AIHub", "Copilot+ AI Hub", "Hub de recursos de IA para Copilot+", true),
        new("Microsoft.XboxApp", "Xbox Console Companion", "App antigo de console Xbox", true)
    ];

    public IReadOnlyList<string> RemoveApp(string appId, bool dryRun)
    {
        var log = new List<string>();
        var isDemo = RuntimeModeContext.Current == RuntimeMode.Demo;

        if (dryRun || isDemo)
        {
            var prefix = isDemo ? "[DEMO]" : "[DRY-RUN]";
            log.Add($"{prefix} Remocao simulada de '{appId}': nenhum pacote UWP sera alterado.");
            return log;
        }

        try
        {
            var userCommand = $"Get-AppxPackage -Name '*{appId}*' | Remove-AppxPackage -ErrorAction SilentlyContinue";
            var userResult = commandRunner.Run("powershell.exe", $"-NoProfile -NonInteractive -Command \"{userCommand}\"");

            var provCommand = $"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like '*{appId}*' }} | Remove-ProvisionedAppxPackage -Online -AllUsers -ErrorAction SilentlyContinue";
            var provResult = commandRunner.Run("powershell.exe", $"-NoProfile -NonInteractive -Command \"{provCommand}\"");

            log.Add($"AppX '{appId}': desinstalado do usuario atual (exit {userResult.ExitCode}) e desprovisionado do sistema (exit {provResult.ExitCode}).");
        }
        catch (Exception ex)
        {
            log.Add($"[ERRO] Falha ao tentar desinstalar AppX '{appId}': {ex.Message}");
        }

        return log;
    }

    public IReadOnlyList<string> RemoveSafeBloatware(bool dryRun)
    {
        var log = new List<string>
        {
            dryRun || RuntimeModeContext.Current == RuntimeMode.Demo
                ? "Debloat UWP (dry-run): simulando remocao de pacotes seguros."
                : "Debloat UWP: iniciando remocao de pacotes bloatware seguros."
        };

        var safeApps = KnownBloatwareApps.Where(a => a.IsSafe).ToList();
        var processed = 0;

        foreach (var app in safeApps)
        {
            var result = RemoveApp(app.AppId, dryRun);
            log.AddRange(result);
            processed++;
        }

        log.Add($"Debloat UWP concluido. {processed} pacote(s) seguros processados.");
        return log;
    }
}
