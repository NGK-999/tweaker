using System.IO;
using ApexTweaker.Minecraft.Models;

namespace ApexTweaker.Minecraft.Services;

internal sealed class MinecraftModConfigContractCatalog
{
    private static readonly IReadOnlyList<Contract> Contracts =
    [
        new(
            "sodium",
            "Sodium",
            ["sodium-options.json"],
            ModConfigAutomationStatus.Supported,
            ["performance.animateOnlyVisibleTextures", "performance.useEntityCulling", "performance.useFogOcclusion", "performance.useBlockFaceCulling", "advanced.enableMemoryTracing"],
            "Chaves validadas contra SodiumGameOptions 0.6.13; somente chaves existentes sao alteradas.",
            "https://github.com/CaffeineMC/sodium/blob/mc1.21.1-0.6.13/common/src/main/java/net/caffeinemc/mods/sodium/client/gui/SodiumGameOptions.java"),
        new(
            "immediatelyfast",
            "ImmediatelyFast",
            ["immediatelyfast.json"],
            ModConfigAutomationStatus.Supported,
            ["font_atlas_resizing", "map_atlas_generation", "fast_text_lookup", "fast_buffer_upload"],
            "Contrato validado para 1.6.11; hud_batching e preservado por compatibilidade.",
            "https://github.com/RaphiMC/ImmediatelyFast/blob/v1.6.11/common/src/main/java/net/raphimc/immediatelyfast/feature/core/ImmediatelyFastConfig.java"),
        new(
            "entityculling",
            "EntityCulling",
            ["entityculling.json"],
            ModConfigAutomationStatus.Supported,
            ["debugMode", "skipEntityCulling", "skipBlockEntityCulling", "tickCulling", "blockEntityFrustumCulling"],
            "Contrato validado para 1.10.5; valores desconhecidos sao preservados.",
            "https://github.com/tr7zw/EntityCulling/blob/1.10.5/EntityCulling-Versionless/src/main/java/dev/tr7zw/entityculling/versionless/Config.java"),
        new(
            "iris",
            "Iris",
            ["iris.properties"],
            ModConfigAutomationStatus.Supported,
            ["enableShaders"],
            "Somente enableShaders=false e automatizado; packs e outras preferencias permanecem manuais.",
            "https://github.com/IrisShaders/Iris"),
        new(
            "lithium",
            "Lithium",
            ["lithium.properties"],
            ModConfigAutomationStatus.DefaultsRecommended,
            [],
            "As otimizacoes estaveis ja ficam ativas por padrao; overrides servem principalmente para compatibilidade.",
            "https://github.com/CaffeineMC/lithium#configuration"),
        new(
            "ferritecore",
            "FerriteCore",
            [],
            ModConfigAutomationStatus.NoConfigurationNeeded,
            [],
            "O ganho principal ocorre ao instalar o mod; nao ha perfil client-side confiavel a inventar.",
            "https://github.com/malte0811/FerriteCore"),
        new(
            "modernfix",
            "ModernFix",
            ["modernfix-mixins.properties"],
            ModConfigAutomationStatus.ManualOnly,
            [],
            "Mixins variam por versao e modpack; alterar em massa pode introduzir incompatibilidade.",
            "https://github.com/embeddedt/ModernFix/wiki"),
        new(
            "moreculling",
            "More Culling",
            ["moreculling.toml", "moreculling.json"],
            ModConfigAutomationStatus.ManualOnly,
            [],
            "Configuracao visual exige validacao por versao e teste contra resource packs.",
            "https://github.com/FxMorin/MoreCulling"),
        new(
            "dynamic_fps",
            "Dynamic FPS",
            ["dynamic_fps.json"],
            ModConfigAutomationStatus.ManualOnly,
            [],
            "Economiza recursos em segundo plano; nao e usado como ganho de FPS em foco.",
            "https://github.com/juliand665/Dynamic-FPS"),
        new(
            "noisium",
            "Noisium",
            ["noisium.json"],
            ModConfigAutomationStatus.NoConfigurationNeeded,
            [],
            "Otimiza worldgen sem um contrato client-side necessario para este perfil.",
            "https://github.com/Steveplays28/noisium"),
        new(
            "sodium-extra",
            "Sodium Extra",
            ["sodium-extra-options.json"],
            ModConfigAutomationStatus.ManualOnly,
            [],
            "Adiciona controles visuais, mas as chaves variam; o ApexTweaker nao escreve sem contrato fixado.",
            "https://github.com/FlashyReese/sodium-extra-fabric"),
        new(
            "reeses-sodium-options",
            "Reese's Sodium Options",
            [],
            ModConfigAutomationStatus.NoConfigurationNeeded,
            [],
            "Reorganiza a interface do Sodium e nao oferece uma otimizacao mensuravel propria.",
            "https://github.com/FlashyReese/reeses-sodium-options")
    ];

    public IReadOnlyList<ModConfigContractAssessment> Assess(
        MinecraftAuditResult audit,
        string configDirectory)
    {
        return Contracts.Select(contract =>
        {
            var mod = audit.Mods.FirstOrDefault(item => string.Equals(item.Id, contract.ModId, StringComparison.OrdinalIgnoreCase));
            var installed = mod is not null;
            var files = contract.FileNames
                .Select(name => Path.Combine(configDirectory, name))
                .Where(File.Exists)
                .Select(Path.GetFullPath)
                .ToArray();
            return new ModConfigContractAssessment(
                contract.ModId,
                contract.DisplayName,
                installed,
                mod?.Version ?? string.Empty,
                installed ? contract.Status : ModConfigAutomationStatus.NotInstalled,
                files,
                contract.SupportedKeys,
                contract.Rationale,
                contract.SourceUrl);
        }).ToArray();
    }

    private sealed record Contract(
        string ModId,
        string DisplayName,
        IReadOnlyList<string> FileNames,
        ModConfigAutomationStatus Status,
        IReadOnlyList<string> SupportedKeys,
        string Rationale,
        string SourceUrl);
}
