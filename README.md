<p align="center">
  <img src="docs/assets/banner.svg" alt="ApexTweaker" width="920"/>
</p>

<p align="center">
  <strong>Otimizador Windows para jogos</strong> — diagnóstico, telemetria e tweaks
  <em>reversíveis</em>, com preparação segura de Minecraft em PCs limitados.
</p>

<p align="center">
  <a href="https://github.com/NGK-999/tweaker/releases"><img src="https://img.shields.io/badge/version-3.3.1-0ea5e9?style=for-the-badge&labelColor=0b1220" alt="Version 3.3.1"/></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&labelColor=0b1220&logo=dotnet&logoColor=white" alt=".NET 10"/></a>
  <img src="https://img.shields.io/badge/platform-Windows-0078D4?style=for-the-badge&labelColor=0b1220&logo=windows&logoColor=white" alt="Windows"/>
  <a href="#segurança-primeiro"><img src="https://img.shields.io/badge/demo-fail--closed-10b981?style=for-the-badge&labelColor=0b1220" alt="Demo fail-closed"/></a>
</p>

<p align="center">
  <a href="#segurança-primeiro">Segurança</a> ·
  <a href="#interface">Interface</a> ·
  <a href="#minecraft-resumo">Minecraft</a> ·
  <a href="#build-local">Build</a> ·
  <a href="#distribuição">Download</a> ·
  <a href="#arquitetura-e-produto">Docs</a>
</p>

---

## Por que existe

| | |
|:--|:--|
| **Medir** | Hardware, VBS/HVCI/HAGS, frametime — entender o PC antes de mexer |
| **Otimizar** | Tweaks Windows com snapshot/ledger e rollback |
| **Preparar** | Minecraft (vanilla/modded) sem prometer FPS mágico nem apagar mods |

---

## Segurança primeiro

> Em `--demo`, o ApexTweaker **não muta** o Windows. Fail-closed.

| Regra | Comportamento |
|:------|:--------------|
| **Demo** | `--demo` bloqueia mutações Windows |
| **Elevação** | UAC só quando a mutação realmente exige admin |
| **Minecraft** | Backup antes de escrever · rollback separado · sem delete de mods |
| **Kernel** | Sem drivers · sem injeção · sem RealTime |

```powershell
# UI sem risco de mutação Windows
dotnet run --project ApexTweaker.csproj -c Release -- --demo

# Self-tests
dotnet run --project ApexTweaker.csproj -c Release -- --demo-self-test
dotnet run --project ApexTweaker.csproj -c Release -- --catalog-feedback-self-test
dotnet run --project ApexTweaker.csproj -c Release -- --gaming-fps-probe-self-test
```

---

## Interface

Shell WPF com transição leve (opacity). Catálogo e probe de Desempenho rodam fora do UI thread.

| Aba | Função |
|:----|:-------|
| **Dashboard** | Auto-Tuning, restore point, resumo de hardware |
| **Desempenho** | Probe VBS / HVCI / HAGS / Game DVR / ReBAR + estabilidade |
| **Módulos** | Tweaks individuais com snapshot |
| **Telemetria** | Teste A/B, frametime, sensores |
| **Catálogo** | Analyze de presets (sem aplicar) + checklist BIOS |
| **Minecraft Rápido** | `Encontrar → Preparar → Testar → Restaurar` |
| **Utilidades** | Rollback, limpeza, TRIM, SFC/DISM, desinstalação |

---

## Minecraft (resumo)

```text
Encontrar  →  Preparar  →  Testar  →  Resolver / Restaurar
```

- Perfis leves (ex.: `POTATO_4GB`) só após confirmação e backup
- Cobblemon como perfil opcional quando detectado
- Avançado: auditoria de mods, quarentena SHA-256, motor científico

**Docs:** [hooks de sessão](docs/MINECRAFT_GENERAL_AND_SESSION_HOOKS.md) ·
[motor científico](docs/SCIENTIFIC_ENGINE.md) ·
[Cobblemon low-end](docs/COBBLEMON_LOW_END.md) ·
[índice](docs/README.md)

---

## Build local

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet publish ApexTweaker.csproj -c Release -r win-x64 --self-contained true -o release-v2
```

Requisito: Visual Studio Build Tools com C++ para `ApexTweaker.Native.dll`.

---

## Distribuição

| Artefato | Link |
|:---------|:-----|
| App | [ApexTweaker.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.exe) |
| Native | [ApexTweaker.Native.dll](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.Native.dll) |
| Setup | [ApexTweaker-Setup.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker-Setup.exe) |

Self-contained (não exige .NET instalado). Detalhes: [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md).

---

## Dados locais

| Dados | Caminho |
|:------|:--------|
| Backups Windows | `C:\ProgramData\ApexTweaker\Backups` |
| Backups Minecraft | `%LOCALAPPDATA%\ApexTweaker\MinecraftBackups` |
| Relatórios / experimentos | `%LOCALAPPDATA%\ApexTweaker\MinecraftReports` |
| Telemetria | `%LOCALAPPDATA%\ApexTweaker\Telemetry` |

---

## CLI útil

```powershell
dotnet run --project ApexTweaker.csproj -- --minecraft-help

dotnet run --project ApexTweaker.csproj -- `
  --minecraft-audit `
  --mods "$env:USERPROFILE\Downloads\mods\mods" `
  --output ".\artifacts\minecraft-audit"

dotnet run --project ApexTweaker.csproj -- --minecraft-self-test
```

Escrita via CLI exige `--yes` e instância válida.

---

## Arquitetura e produto

[Estado atual](docs/architecture/current-state.md) ·
[Estado alvo](docs/architecture/target-state.md) ·
[Estrutura](docs/PROJECT_STRUCTURE.md) ·
[Plano de coordenação](docs/coordination/master-plan.md)

---

<p align="center">
  <sub>ApexTweaker · Igor Silva · .NET 10 + WPF</sub>
</p>
