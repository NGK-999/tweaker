namespace ApexTweaker.Models;

internal sealed record BiosChecklistItem(
    string Id,
    string Vendor,
    string Title,
    string Guidance,
    string RiskNote);

internal static class BiosChecklistCatalog
{
    public static IReadOnlyList<BiosChecklistItem> Items { get; } =
    [
        new("bios.xmp-expo", "Generic", "XMP / EXPO (memoria)",
            "Ative o perfil certificado da memoria (XMP Intel ou EXPO AMD) no UEFI. Confirme estabilidade com stress curto.",
            "Perfis agressivos demais podem causar boot loop — tenha Clear CMOS."),
        new("bios.resizable-bar", "Generic", "Resizable BAR / Smart Access Memory",
            "Ative Above 4G Decoding + Resizable BAR quando GPU e placa-mae suportarem.",
            "Em algumas placas antigas Above 4G + CSM on conflita — desligue CSM/Legacy."),
        new("bios.csm-off", "Generic", "CSM / Legacy boot off",
            "Desligue CSM para boot UEFI puro (melhor com Secure Boot e drivers modernos).",
            "Pode impedir boot de midia legacy — tenha midia UEFI."),
        new("bios.vt-off-gaming", "Generic", "Virtualization for pure gaming (opcional)",
            "Se nao usa WSL/Hyper-V/Android Emulator, VT-d/SVM pode ficar off em laboratorio de latencia.",
            "Quebra containers/WSL — so com uso NoVirtualization confirmado."),
        new("bios.asus", "ASUS", "ASUS AI Tweaker / Extreme Tweaker",
            "AI Overclocking off se quiser clocks previsiveis; ASUS MultiCore Enhancement conforme guia da CPU; EXPO/XMP em AI Tweaker.",
            "Siga QVL da placa-mae."),
        new("bios.msi", "MSI", "MSI Click BIOS",
            "OC: A-XMP/EXPO; Advanced > PCIe Configuration > Resizable BAR; desative Fast Boot ao diagnosticar.",
            "Game Boost automatico pode elevar voltagem — preferivel perfil manual estavel."),
        new("bios.gigabyte", "Gigabyte", "Gigabyte BIOS",
            "Tweaker: XMP/EXPO; Settings > IO Ports > Above 4G; CSM Support Disabled.",
            "PerfDrive / Enhanced Multi-Core pode alterar boost — meça frametime."),
        new("bios.asrock", "ASRock", "ASRock UEFI",
            "OC Tweaker: XMP; Advanced > Chipset > Above 4G Decoding / Re-Size BAR.",
            "Load Optimized Defaults antes de reaplicar perfil se instavel.")
    ];
}
