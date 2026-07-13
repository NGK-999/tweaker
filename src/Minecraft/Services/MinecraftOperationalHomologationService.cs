using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftOperationalHomologationService
{
    private readonly MinecraftInstanceService instanceService = new();

    public MinecraftOperationalChecklist BuildChecklist(
        MinecraftAuditResult audit,
        MinecraftQuarantinePlan quarantine,
        MinecraftProfilePlan? profilePlan = null)
    {
        var targetMemory = MinecraftEnvironmentService.RecommendJavaMemory(
            Math.Min(audit.Environment.TotalMemoryGb, 4m),
            Math.Min(audit.Environment.AvailableMemoryGb, 4m));
        var javaArguments = profilePlan?.JavaArguments ?? targetMemory.Arguments;
        var fpsLimit = profilePlan?.MaximumFps ?? 45;
        var modDecisions = quarantine.Candidates.Select(candidate =>
            $"{candidate.FileName}: {candidate.OperationalRecommendation}; lado={candidate.SideAssessment}; " +
            $"servidor={candidate.ServerEntryImpact}; conteudo={candidate.ContentImpact}").ToArray();
        var remainingRisks = new List<string>
        {
            "A lista local nao prova quais mods o servidor exige; o manifesto do servidor continua obrigatorio.",
            "Quatro GB permanecem um cenario experimental sujeito a paginacao e stutter.",
            "FPS e entrada no servidor exigem observacao manual no hardware alvo."
        };
        if (profilePlan is null && !audit.InstanceRootDetected)
        {
            remainingRisks.Insert(
                0,
                "A pasta auditada nao e uma instancia completa; apply real depende da instancia criada no PC alvo.");
        }

        return new MinecraftOperationalChecklist(
            DateTimeOffset.UtcNow,
            audit.ModsDirectory,
            profilePlan?.Instance.GameDirectory ?? audit.InstanceRoot,
            profilePlan is not null || audit.InstanceRootDetected,
            javaArguments,
            fpsLimit,
            [
                "Confirmar que o ZIP veio da release autenticada e conferir SHA-256 antes de executar.",
                "Criar uma copia completa da instancia e obter o manifesto exato do servidor.",
                "Fechar Minecraft e launcher antes de alterar configs ou JARs.",
                "Confirmar Java 21 x64 e pagefile ativo/gerenciado pelo Windows.",
                "Registrar a lista e os hashes dos mods antes do primeiro teste."
            ],
            [
                "Preferir Prism Launcher: Add Instance > Minecraft 1.21.1 > Fabric.",
                "Selecionar Java 21 x64 em Edit > Settings > Java e executar o teste do runtime.",
                "Abrir a instancia uma vez sem shaders para gerar options.txt e config, depois fechar o jogo.",
                "Colocar somente o conjunto confirmado do servidor em .minecraft\\mods.",
                "No ApexTweaker, selecionar a pasta da instancia Prism, sua .minecraft ou a pasta mods."
            ],
            [
                $"Executar auditoria e revisar duplicidades antes do perfil. JVM proposta: {javaArguments}.",
                $"Executar dry-run EXTREME_4GB com limite de {fpsLimit} FPS.",
                "Confirmar backup, arquivos e valores antes/depois; somente entao executar Apply.",
                "Verificar render 4, simulation 5, particulas minimas, nuvens off, biome blend 0, mipmap 0 e VSync off.",
                "Confirmar Iris enableShaders=false e desativar manualmente resource packs pesados.",
                "Nao colocar nenhum mod em quarentena durante o primeiro baseline."
            ],
            [
                "Baseline: medir sem quarentena e sem ImmediatelyFast, usando o mesmo servidor, rota e resolucao.",
                "Registrar tempo do clique ate o menu e do menu ate entrar no mundo/servidor.",
                "Executar benchmark automatico por 60 segundos depois de entrar e permanecer no mesmo local.",
                "Registrar FPS medio e minimo pelo F3, Spark ou PresentMon; o ApexTweaker nao inventa FPS.",
                "Repetir com 20, 24, 30, 45 e 60 FPS; manter o menor limite que entregar frametime estavel.",
                "Testar heaps 2048, 2304 e 2560 MB somente quando a recomendacao de RAM livre permitir.",
                "Adicionar ImmediatelyFast sozinho, repetir o teste e reverter se houver crash ou artefato de HUD.",
                "Testar uma quarentena por vez e validar novamente a entrada no servidor."
            ],
            [
                "O jogo abre e chega ao menu.",
                "Entra no servidor ou mundo sem fechamento por falta de memoria.",
                "Permanece jogavel em 1280x720 sem quedas severas constantes.",
                "FPS medio ideal de pelo menos 30; minimo abaixo de 15 exige nova avaliacao.",
                "latest.log e crash-reports nao mostram OutOfMemoryError nem crash novo.",
                "Todos os arquivos alterados possuem backup e rollback testado."
            ],
            [
                "Nunca excluir JARs; quarentena exige selecao e confirmacao explicitas.",
                "Nunca mover mod comum aos dois lados sem confirmar o manifesto do servidor.",
                "Sempre preservar SHA-256, relatorio antes/depois e backup fora da pasta ativa.",
                "Nunca desativar Defender, Windows Update ou pagefile permanentemente.",
                "Nunca executar Prism Launcher ou Minecraft como administrador.",
                "Aplicar apenas uma variavel por rodada de homologacao."
            ],
            modDecisions,
            remainingRisks);
    }

    public MinecraftOperationalHomologationResult Evaluate(
        string selectedPath,
        MinecraftOperationalObservation observation,
        MinecraftBenchmarkResult? automaticBenchmark = null)
    {
        if (!instanceService.TryResolve(selectedPath, out var instance))
        {
            throw new InvalidOperationException("A homologacao exige uma instancia real com options.txt e subpasta mods.");
        }

        var hasObservation = observation.GameOpened ||
                             observation.MenuReached ||
                             observation.WorldEntered ||
                             observation.ServerEntered ||
                             observation.MenuLoadSeconds is not null ||
                             observation.JoinLoadSeconds is not null ||
                              observation.AverageFps is not null ||
                              observation.MinimumFps is not null ||
                              observation.PlayableAt720p ||
                              observation.SevereDrops ||
                              observation.Crashed ||
                              observation.OutOfMemory;

        var automaticCrash = automaticBenchmark?.CrashEvidence == true ||
                             automaticBenchmark?.Status == BenchmarkStatus.Failed;
        var automaticOom = automaticBenchmark?.OutOfMemoryEvidence == true;
        var enteredTarget = observation.WorldEntered || observation.ServerEntered;
        var noCrashOrOom = !observation.Crashed &&
                           !observation.OutOfMemory &&
                           !automaticCrash &&
                           !automaticOom;
        var fpsAcceptable = observation.AverageFps is >= 30d;
        var minimumAcceptable = observation.MinimumFps is >= 15d;
        var criteria = new[]
        {
            new MinecraftHomologationCriterion("Jogo abriu", observation.GameOpened, observation.GameOpened ? "SIM" : "NAO"),
            new MinecraftHomologationCriterion("Menu carregou", observation.MenuReached, FormatSeconds(observation.MenuLoadSeconds)),
            new MinecraftHomologationCriterion("Entrou em mundo ou servidor", enteredTarget,
                observation.ServerEntered ? "SERVIDOR" : observation.WorldEntered ? "MUNDO" : "NAO"),
            new MinecraftHomologationCriterion("Sem crash/OOM", noCrashOrOom,
                noCrashOrOom ? "Nenhuma evidencia marcada" : "Crash ou falta de memoria detectada"),
            new MinecraftHomologationCriterion("Jogavel em 720p", observation.PlayableAt720p,
                observation.PlayableAt720p ? "SIM" : "NAO"),
            new MinecraftHomologationCriterion("FPS medio >= 30", fpsAcceptable,
                observation.AverageFps?.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) ?? "NAO MEDIDO"),
            new MinecraftHomologationCriterion("FPS minimo >= 15", minimumAcceptable,
                observation.MinimumFps?.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) ?? "NAO MEDIDO"),
            new MinecraftHomologationCriterion("Sem quedas severas", !observation.SevereDrops,
                observation.SevereDrops ? "QUEDAS MARCADAS" : "NAO MARCADAS")
        };

        var status = !hasObservation
            ? OperationalHomologationStatus.NotTested
            : !observation.GameOpened || !observation.MenuReached || !enteredTarget || !noCrashOrOom
                ? OperationalHomologationStatus.Failed
                : !observation.PlayableAt720p || !fpsAcceptable || !minimumAcceptable || observation.SevereDrops ||
                  automaticBenchmark?.Status == BenchmarkStatus.Unstable
                    ? OperationalHomologationStatus.Unstable
                    : OperationalHomologationStatus.Approved;

        var risks = new List<string>();
        if (!observation.ServerEntered)
        {
            risks.Add("Entrada no servidor ainda nao foi comprovada nesta rodada.");
        }

        if (observation.AverageFps is null || observation.MinimumFps is null)
        {
            risks.Add("FPS medio/minimo incompleto; use F3, Spark ou PresentMon.");
        }

        if (automaticBenchmark is null || automaticBenchmark.Status == BenchmarkStatus.NotTested)
        {
            risks.Add("Benchmark automatico nao foi associado a esta homologacao.");
        }

        return new MinecraftOperationalHomologationResult(
            DateTimeOffset.UtcNow,
            instance.GameDirectory,
            status,
            observation,
            automaticBenchmark,
            criteria,
            risks,
            [
                "Preserve esta rodada como baseline antes de mudar outro mod ou heap.",
                "Se falhou, restaure o ultimo perfil/quarentena e anexe latest.log e crash report.",
                "Somente promova uma configuracao depois de duas rodadas reproduziveis."
            ]);
    }

    private static string FormatSeconds(decimal? seconds)
    {
        return seconds is null
            ? "NAO MEDIDO"
            : $"{seconds.Value:0.0} s";
    }
}
