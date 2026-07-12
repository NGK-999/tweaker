using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal static class MinecraftModCatalog
{
    private static readonly IReadOnlyList<ModRecommendation> Catalog =
    [
        new("fabric-api", "Fabric API", RecommendationLayer.EssentialSafe, false,
            "Base exigida pelo Cobblemon e pela maior parte do ecossistema Fabric.",
            "https://modrinth.com/mod/fabric-api", []),
        new("fabric-language-kotlin", "Fabric Language Kotlin", RecommendationLayer.EssentialSafe, false,
            "Runtime Kotlin exigido por alguns addons; o Cobblemon atual ja o incorpora como JAR aninhado.",
            "https://modrinth.com/mod/fabric-language-kotlin", []),
        new("cobblemon", "Cobblemon", RecommendationLayer.EssentialSafe, false,
            "Mod principal do pacote e provavel requisito do servidor.",
            "https://modrinth.com/mod/cobblemon", ["fabric-api"]),
        new("sodium", "Sodium", RecommendationLayer.EssentialSafe, false,
            "Substitui o renderizador e reduz custo de CPU e microtravamentos.",
            "https://modrinth.com/mod/sodium", ["fabric-api"]),
        new("lithium", "Lithium", RecommendationLayer.EssentialSafe, false,
            "Otimiza logica, ticks e servidor integrado sem mudar mecanicas.",
            "https://modrinth.com/mod/lithium", []),
        new("ferritecore", "FerriteCore", RecommendationLayer.EssentialSafe, false,
            "Reduz memoria de modelos e blockstates; prioridade alta em PCs com 4 GB.",
            "https://modrinth.com/mod/ferrite-core", []),
        new("immediatelyfast", "ImmediatelyFast", RecommendationLayer.EssentialSafe, false,
            "Otimiza entidades, HUD, texto e renderizacao imediata; e client-side.",
            "https://modrinth.com/mod/immediatelyfast", []),
        new("modernfix", "ModernFix", RecommendationLayer.Recommended, false,
            "Reduz memoria e tempo de carregamento, mas deve ser validado com o modpack completo.",
            "https://modrinth.com/mod/modernfix", []),
        new("entityculling", "Entity Culling", RecommendationLayer.Recommended, false,
            "Evita renderizar entidades ocultas; ganho depende da cena.",
            "https://modrinth.com/mod/entityculling", []),
        new("moreculling", "More Culling", RecommendationLayer.Recommended, false,
            "Amplia culling de blocos; requer Cloth Config e teste visual.",
            "https://modrinth.com/mod/moreculling", ["cloth-config"]),
        new("dynamic_fps", "Dynamic FPS", RecommendationLayer.Recommended, false,
            "Reduz CPU/GPU quando o jogo esta em segundo plano; nao aumenta FPS em foco.",
            "https://modrinth.com/mod/dynamic-fps", ["fabric-api"]),
        new("fastquit", "FastQuit", RecommendationLayer.Experimental, false,
            "Melhora apenas a saida de mundos single-player; pouco impacto ao jogar em servidor.",
            "https://modrinth.com/mod/fastquit", []),
        new("noisium", "Noisium", RecommendationLayer.Experimental, false,
            "Ajuda worldgen no servidor integrado; em multiplayer o ganho exige instalacao no servidor.",
            "https://modrinth.com/mod/noisium", []),
        new("sodium-extra", "Sodium Extra", RecommendationLayer.Experimental, false,
            "Adiciona controles para reduzir efeitos, mas nao e requisito de desempenho.",
            "https://modrinth.com/mod/sodium-extra", ["sodium"]),
        new("reeses-sodium-options", "Reese's Sodium Options", RecommendationLayer.Experimental, false,
            "Apenas reorganiza a interface de opcoes do Sodium.",
            "https://modrinth.com/mod/reeses-sodium-options", ["sodium"]),
        new("modmenu", "Mod Menu", RecommendationLayer.Experimental, false,
            "Facilita configuracao, mas nao melhora FPS por si so.",
            "https://modrinth.com/mod/modmenu", []),
        new("distanthorizons", "Distant Horizons", RecommendationLayer.AvoidOrRemove, false,
            "LOD distante aumenta memoria, disco e geracao de chunks; inadequado ao perfil EXTREME_4GB.",
            "https://modrinth.com/mod/distanthorizons", []),
        new("iris", "Iris", RecommendationLayer.AvoidOrRemove, false,
            "Sem shaders, e uma camada grafica dispensavel no perfil de 4 GB.",
            "https://modrinth.com/mod/iris", ["sodium"]),
        new("indium", "Indium", RecommendationLayer.AvoidOrRemove, false,
            "Sodium 0.6 fornece a API do Indium; uma copia separada pode ser redundante ou incompativel.",
            "https://modrinth.com/mod/indium", ["sodium"]),
        new("entity_model_features", "Entity Model Features", RecommendationLayer.AvoidOrRemove, false,
            "Recurso visual que aumenta custo e nao e apropriado para o perfil extremo.",
            "https://modrinth.com/mod/entity-model-features", []),
        new("entity_texture_features", "Entity Texture Features", RecommendationLayer.AvoidOrRemove, false,
            "Recurso visual que aumenta custo e nao e apropriado para o perfil extremo.",
            "https://modrinth.com/mod/entitytexturefeatures", [])
    ];

    public static readonly HashSet<string> PerformanceIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "sodium",
        "lithium",
        "ferritecore",
        "immediatelyfast",
        "modernfix",
        "entityculling",
        "moreculling",
        "dynamic_fps",
        "fastquit",
        "noisium",
        "krypton",
        "zfastnoise"
    };

    public static readonly HashSet<string> LibraryIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "fabric-api",
        "fabric-language-kotlin",
        "cloth-config",
        "architectury",
        "accessories",
        "accessories_compat_layer",
        "athena",
        "balm",
        "forgeconfigapiport",
        "fusion",
        "fzzy_config",
        "geckolib",
        "libjf",
        "midnightlib",
        "owo",
        "platform",
        "rctapi",
        "resourcefullib",
        "sophisticatedcore",
        "supermartijn642corelib",
        "tim_core",
        "trinkets"
    };

    public static readonly HashSet<string> ExtremeRemovalCandidates = new(StringComparer.OrdinalIgnoreCase)
    {
        "distanthorizons",
        "iris",
        "continuity",
        "entity_model_features",
        "entity_texture_features"
    };

    public static IReadOnlyList<ModRecommendation> BuildRecommendations(
        IReadOnlyCollection<MinecraftModDescriptor> mods)
    {
        var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in mods)
        {
            if (!string.IsNullOrWhiteSpace(mod.Id))
            {
                available.Add(mod.Id);
            }

            available.UnionWith(mod.Provides);
            available.UnionWith(mod.EmbeddedModIds);
        }

        return Catalog
            .Select(item => item with { Installed = available.Contains(item.Id) })
            .OrderBy(item => item.Layer)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static ModRecommendation? Find(string id)
    {
        return Catalog.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
