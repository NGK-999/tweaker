using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal static class MinecraftExtremeExperimentCatalog
{
    public const int MinimumRenderDistance = 2;
    public const int MinimumSimulationDistance = 5;

    private static readonly IReadOnlyList<MinecraftExperimentDefinition> Experiments =
    [
        Resolution("resolution-1280x720", "1280 x 720", 1280, 720),
        Resolution("resolution-960x540", "960 x 540", 960, 540),
        Resolution("resolution-854x480", "854 x 480", 854, 480),
        Option("fps-20", "FPS cap", "20 FPS", MinecraftExperimentVariable.FpsCap, "maxFps", "20", "Reduz picos de CPU/GPU em hardware no limite."),
        Option("fps-24", "FPS cap", "24 FPS", MinecraftExperimentVariable.FpsCap, "maxFps", "24", "Busca fluidez cinematica com menor pressao de renderizacao."),
        Option("fps-30", "FPS cap", "30 FPS", MinecraftExperimentVariable.FpsCap, "maxFps", "30", "Baseline recomendado para jogabilidade minima."),
        Option("fps-45", "FPS cap", "45 FPS", MinecraftExperimentVariable.FpsCap, "maxFps", "45", "Testa folga acima do baseline sem liberar FPS."),
        Option("fps-60", "FPS cap", "60 FPS", MinecraftExperimentVariable.FpsCap, "maxFps", "60", "Somente para confirmar se existe folga real."),
        Option("render-2", "Render distance", "2 chunks", MinecraftExperimentVariable.RenderDistance, "renderDistance", "2", "Menor distancia de renderizacao valida usada pelo experimento."),
        Option("render-3", "Render distance", "3 chunks", MinecraftExperimentVariable.RenderDistance, "renderDistance", "3", "Compara ganho entre o minimo e 4 chunks."),
        Option("render-4", "Render distance", "4 chunks", MinecraftExperimentVariable.RenderDistance, "renderDistance", "4", "Baseline visual conservador."),
        Option("render-5", "Render distance", "5 chunks", MinecraftExperimentVariable.RenderDistance, "renderDistance", "5", "Testa qualidade adicional somente com folga."),
        Option("simulation-5", "Simulation distance", "5 chunks (minimo 1.21.1)", MinecraftExperimentVariable.SimulationDistance, "simulationDistance", "5", "Usa o menor valor aceito pelo controle vanilla 1.21.1."),
        Option("entity-030", "Entity distance", "30%", MinecraftExperimentVariable.EntityDistance, "entityDistanceScaling", "0.30", "Reduz entidades distantes renderizadas."),
        Option("entity-040", "Entity distance", "40%", MinecraftExperimentVariable.EntityDistance, "entityDistanceScaling", "0.40", "Compara visibilidade e custo de entidades."),
        Option("entity-050", "Entity distance", "50%", MinecraftExperimentVariable.EntityDistance, "entityDistanceScaling", "0.50", "Baseline seguro para Cobblemon."),
        Option("entity-075", "Entity distance", "75%", MinecraftExperimentVariable.EntityDistance, "entityDistanceScaling", "0.75", "Somente se Pokemon distantes forem necessarios."),
        new MinecraftExperimentDefinition(
            "visual-minimal",
            "Qualidade visual",
            "Tudo no minimo",
            MinecraftExperimentVariable.VisualQuality,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ao"] = "false",
                ["biomeBlendRadius"] = "0",
                ["clouds"] = "false",
                ["entityShadows"] = "false",
                ["fovEffectScale"] = "0.0",
                ["graphicsMode"] = "0",
                ["mipmapLevels"] = "0",
                ["particles"] = "2",
                ["screenEffectScale"] = "0.0"
            },
            null,
            "Agrupa apenas controles visuais vanilla conhecidos.",
            "Reduz fill-rate, particulas e efeitos sem alterar gameplay."),
        new MinecraftExperimentDefinition(
            "resource-packs-off",
            "Resource packs",
            "Sem resource pack local",
            MinecraftExperimentVariable.ResourcePacks,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["resourcePacks"] = "[]" },
            null,
            "Desativa a lista local sem excluir arquivos; confirme antes requisitos do servidor.",
            "Mede memoria e carregamento sem texturas locais adicionais."),
        Window("window-854x480", "Janela 854 x 480", 854, 480),
        Window("window-960x540", "Janela 960 x 540", 960, 540),
        Window("window-1280x720", "Janela 1280 x 720", 1280, 720),
        Heap("heap-1792", "Heap 1792 MB", 1792, "Testa menor paginacao quando o Windows fica sem RAM."),
        Heap("heap-2048", "Heap 2048 MB", 2048, "Baseline recomendado para 4 GB."),
        Heap("heap-2304", "Heap 2304 MB", 2304, "Testa falta de heap com RAM livre comprovada."),
        Heap("heap-2560", "Heap 2560 MB", 2560, "Ultimo patamar; reverta se pagefile ou stutter crescer.")
    ];

    public static IReadOnlyList<MinecraftExperimentDefinition> All => Experiments;

    public static MinecraftExperimentDefinition Get(string id)
    {
        var experiment = Experiments.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "Experimento extremo desconhecido.");
        Validate(experiment);
        return experiment;
    }

    public static void Validate(MinecraftExperimentDefinition experiment)
    {
        if (experiment.OptionValues.TryGetValue("renderDistance", out var render) &&
            (!int.TryParse(render, out var renderValue) || renderValue < MinimumRenderDistance || renderValue > 32))
        {
            throw new InvalidOperationException("Render distance deve ficar entre 2 e 32 no contrato 1.21.1.");
        }

        if (experiment.OptionValues.TryGetValue("simulationDistance", out var simulation) &&
            (!int.TryParse(simulation, out var simulationValue) || simulationValue < MinimumSimulationDistance || simulationValue > 32))
        {
            throw new InvalidOperationException("Simulation distance deve ficar entre 5 e 32 no contrato 1.21.1.");
        }

        if (experiment.HeapMb is not null && experiment.HeapMb is not (1792 or 2048 or 2304 or 2560))
        {
            throw new InvalidOperationException("Heap extremo deve usar 1792, 2048, 2304 ou 2560 MB.");
        }
    }

    private static MinecraftExperimentDefinition Resolution(string id, string name, int width, int height) =>
        new(
            id,
            "Resolucao",
            name,
            MinecraftExperimentVariable.Resolution,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["overrideWidth"] = width.ToString(),
                ["overrideHeight"] = height.ToString()
            },
            null,
            "Altera somente a resolucao configurada.",
            "Menos pixels podem reduzir carga na Intel HD.");

    private static MinecraftExperimentDefinition Window(string id, string name, int width, int height) =>
        new(
            id,
            "Modo de janela",
            name,
            MinecraftExperimentVariable.WindowMode,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["fullscreen"] = "false",
                ["overrideWidth"] = width.ToString(),
                ["overrideHeight"] = height.ToString()
            },
            null,
            "Forca janela com resolucao fixa sem alterar o monitor.",
            "Evita custo de resolucao nativa e facilita repetir a mesma cena.");

    private static MinecraftExperimentDefinition Option(
        string id,
        string category,
        string name,
        MinecraftExperimentVariable variable,
        string key,
        string value,
        string expected) =>
        new(
            id,
            category,
            name,
            variable,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [key] = value },
            null,
            $"Altera somente {key} para {value}.",
            expected);

    private static MinecraftExperimentDefinition Heap(string id, string name, int heapMb, string expected) =>
        new(
            id,
            "Java heap",
            name,
            MinecraftExperimentVariable.JavaHeap,
            new Dictionary<string, string>(),
            heapMb,
            $"Compara -Xms512M -Xmx{heapMb}M sem flags de GC nao comprovadas.",
            expected);
}
