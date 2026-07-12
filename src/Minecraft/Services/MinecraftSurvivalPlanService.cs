using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftSurvivalPlanService
{
    public MinecraftSurvivalPlan Build(MinecraftAuditResult audit, MinecraftQuarantinePlan quarantine)
    {
        var environment = audit.Environment;
        var verdict = environment.TotalMemoryGb <= 4.5m
            ? "EXPERIMENTAL: pode abrir em 720p minimo, mas estabilidade nao e garantida."
            : "VIAVEL PARA TESTE: valide no mundo e servidor reais antes de concluir.";
        if (!environment.Java.Found || !environment.Java.Is64Bit)
        {
            verdict = "BLOQUEADO ATE CORRIGIR JAVA: selecione Java 21 x64 no launcher.";
        }

        var required = audit.Recommendations
            .Where(item => item.Layer == RecommendationLayer.EssentialSafe)
            .Select(item => $"{item.Name}: {(item.Installed ? "INSTALADO" : "AUSENTE")}")
            .ToArray();
        var recommended = audit.Recommendations
            .Where(item => item.Layer == RecommendationLayer.Recommended)
            .Select(item => $"{item.Name}: {(item.Installed ? "INSTALADO" : "TESTAR EM COPIA")}")
            .ToArray();
        var quarantineCandidates = quarantine.Candidates
            .Select(item => $"{item.FileName} [{item.Risk}] - {item.Reason}")
            .ToArray();

        var risks = new List<string>
        {
            "4 GB sao compartilhados por Windows, Java e video integrado; paginacao e stutter continuam provaveis.",
            "Mods exigidos pelo servidor nao podem ser inferidos com certeza apenas pelo JAR local.",
            "Geracao de chunks e muitas entidades Cobblemon podem derrubar FPS minimo mesmo com o perfil.",
            "FPS nao e prometido nem medido automaticamente pelo ApexTweaker."
        };
        if (environment.PageFileAllocatedMb == 0)
        {
            risks.Add("Pagefile nao detectado; com 4 GB isso aumenta muito o risco de crash por memoria.");
        }

        var manual = audit.ManualActions
            .Concat(environment.ManualRecommendations)
            .Concat(
            [
                "Use SSD e mantenha o pagefile ativo e gerenciado pelo Windows.",
                "Feche navegador, Discord e launchers pesados antes de iniciar o Minecraft.",
                "Teste uma alteracao por vez e preserve o manifesto exato do servidor."
            ])
            .Concat(environment.TotalMemoryGb <= 4.5m
                ? new[] { "Upgrade minimo altamente recomendado: 8 GB, preferencialmente 2x4 GB em dual-channel." }
                : Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MinecraftSurvivalPlan(
            DateTimeOffset.UtcNow,
            verdict,
            environment.RecommendedJavaArguments,
            required,
            recommended,
            quarantineCandidates,
            [
                "Resolucao 1280x720",
                "Render distance 4 / simulation distance 4",
                "Graficos rapidos, nuvens e sombras de entidades desligadas",
                "Particulas minimas, biome blend 0, mipmap 0",
                "VSync desligado e limite inicial de 45 FPS",
                "Shaders e resource packs pesados desativados"
            ],
            risks,
            manual);
    }
}
