# Market coverage matrix — ApexTweaker vs EXM / BoosterX

Data: 2026-07-24  
Modo: **A** (catálogo completo; Auto só Safe/Competitive; Dangerous com confirmação; BIOS = checklist)

## Decisões

| Item | Decisão |
|------|---------|
| Auto-Optimize | Nunca aplica `dangerous.*`, Update blocker, Defender off, SmartScreen off, delete Edge, mass Xbox kill em preset Streamer |
| Apply manual | Tudo disponível na UI com badge + confirm quando Risky/Dangerous |
| BIOS | Checklist estático por vendor — sem flash |
| Paridade | Categorias do EXM Free gist V4.1 + utilitários BoosterX-like; **não** drivers kernel EXM |

## Mapa de lotes

| Lote | Categoria EXM | Apex | Status |
|------|---------------|------|--------|
| B1 | PC clean, Storage, Fix Corrupted | `MarketUtilitiesService` + Utilidades UI | Implementado nesta entrega |
| B2 | Windows Tweaks (UI noise) | `ApplyUiNoiseTweaks` + catálogo | Implementado |
| B3 | Memory Tweaks | `ApplyMemoryTweaks` (+ compression/kernel existentes) | Implementado |
| B4 | Network avançado | `ApplyAdvancedNetworkTweaks` + guia bufferbloat | Implementado |
| B5 | Debloat condicional | `ApplyConditionalDebloat` + inventário usage | Implementado |
| B6 | Timer / affinity | já `ApplyTimerResolutionTweak` / `ApplyRyzenAffinityIsolation` + catálogo gated | Catalogado + UI Advanced |
| B7 | Dangerous | IDs `dangerous.*` no catálogo; apply só via Advanced confirm | Catalogado |
| B8 | BIOS | `BiosChecklistCatalog` + view | Implementado |
| B9 | FE catálogo | `CatalogView` + nav | Implementado |

## Categorias cobertas (menu EXM Free)

| EXM | Apex entry |
|-----|------------|
| General Tweaks | Background + UI noise + Policy |
| Mouse and Keyboard | Input/USB |
| Windows Tweaks | UI noise + Policy |
| PC clean | Limpar temporários |
| Memory Tweaks | Memory module |
| Disable Startup Services | Debloat condicional (serviços pontuais) |
| GPU / CPU / USB / Power | Módulos existentes |
| Debloat | Debloat condicional + Policy |
| Storage Tweaks | TRIM + Storage Sense |
| Fix Corrupted Files | SFC/DISM repair |

## Explicitamente fora

- Flash BIOS, HIDUSBF, NSudo SYSTEM elevation, kill switch, telemetria HWID de terceiros, copiar EXM Electron.
- WinUtil: WinGet/ISO/Updates Disable ALL / OOSU / Brave debloat — ver [winutil-coverage-matrix.md](winutil-coverage-matrix.md).

## WinUtil (CTT)

Paridade Essential + Advanced (confirm) via `ApplyCttEssentialTweaks` / `ApplyCttAdvancedTweaks` (2026-07-25). BitLocker = `dangerous.ctt-disable-bitlocker`.
