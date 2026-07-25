# ApexTweaker

Otimizador Windows para jogos — diagnóstico, telemetria e tweaks **reversíveis**,
mais preparação segura de Minecraft em PCs limitados.

**Versão 3.3.1** · .NET 10 + WPF · Autor: **Igor Silva**  
Repo: [NGK-999/tweaker](https://github.com/NGK-999/tweaker)

## Por que existe

- Medir e entender o PC (hardware, VBS/HVCI/HAGS, frametime).
- Aplicar otimizações Windows com **snapshot/ledger** e rollback.
- Preparar Minecraft (vanilla/modded) sem prometer FPS mágico nem apagar mods.

## Segurança primeiro

| Regra | Comportamento |
|-------|----------------|
| Demo | `--demo` **não muta** Windows (fail-closed) |
| Elevação | UAC só quando a mutação Windows exige admin |
| Minecraft | Backup antes de escrever; rollback separado; sem delete de mods |
| Kernel | Sem drivers / injeção / RealTime |

```powershell
# Abrir a UI sem risco de mutação Windows
dotnet run --project ApexTweaker.csproj -c Release -- --demo

# Self-tests de segurança / feedback / probe
dotnet run --project ApexTweaker.csproj -c Release -- --demo-self-test
dotnet run --project ApexTweaker.csproj -c Release -- --catalog-feedback-self-test
dotnet run --project ApexTweaker.csproj -c Release -- --gaming-fps-probe-self-test
```

## Interface

| Aba | Função |
|-----|--------|
| **Dashboard** | Auto-Tuning, restore point, resumo de hardware |
| **Desempenho** | Probe VBS/HVCI/HAGS/Game DVR/ReBAR + ações de estabilidade |
| **Módulos** | Tweaks individuais com snapshot |
| **Telemetria** | Teste A/B, frametime, sensores |
| **Catálogo** | Analyze de presets (sem aplicar) + checklist BIOS |
| **Minecraft Rápido** | `Encontrar → Preparar → Testar → Restaurar` |
| **Utilidades** | Rollback, limpeza, TRIM, SFC/DISM, desinstalação |

Navegação com transição leve (opacity); análise do Catálogo e probe de Desempenho
correm fora do UI thread.

## Minecraft (resumo)

Fluxo fácil na página **Minecraft Rápido**:

`Encontrar → Preparar → Testar → Resolver ou Restaurar`

- Perfis leves (ex.: `POTATO_4GB`) só após confirmação e backup.
- Cobblemon detectado como perfil opcional.
- Modo avançado: auditoria de mods, quarentena com SHA-256, motor científico.

Documentação:

- [Minecraft + session hooks](docs/MINECRAFT_GENERAL_AND_SESSION_HOOKS.md)
- [Motor científico](docs/SCIENTIFIC_ENGINE.md)
- [Cobblemon low-end](docs/COBBLEMON_LOW_END.md)
- [Índice docs](docs/README.md)

## Build local

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet publish ApexTweaker.csproj -c Release -r win-x64 --self-contained true -o release-v2
```

Requisito: Visual Studio Build Tools com C++ para `ApexTweaker.Native.dll`.

## Distribuição

- [ApexTweaker.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.exe)
- [ApexTweaker.Native.dll](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.Native.dll)
- [ApexTweaker-Setup.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker-Setup.exe)

Publicação self-contained (não exige .NET instalado). Repo privado: releases
podem retornar 404 sem conta autorizada. Detalhes: [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md).

## Dados locais

| Dados | Caminho |
|-------|---------|
| Backups Windows | `C:\ProgramData\ApexTweaker\Backups` |
| Backups Minecraft | `%LOCALAPPDATA%\ApexTweaker\MinecraftBackups` |
| Relatórios / experimentos | `%LOCALAPPDATA%\ApexTweaker\MinecraftReports` (+ Experiments, SessionHooks) |
| Telemetria | `%LOCALAPPDATA%\ApexTweaker\Telemetry` |

## CLI útil

```powershell
# Ajuda Minecraft
dotnet run --project ApexTweaker.csproj -- --minecraft-help

# Auditoria somente leitura
dotnet run --project ApexTweaker.csproj -- `
  --minecraft-audit `
  --mods "$env:USERPROFILE\Downloads\mods\mods" `
  --output ".\artifacts\minecraft-audit"

# Self-test Minecraft
dotnet run --project ApexTweaker.csproj -- --minecraft-self-test
```

Escrita via CLI exige `--yes` e instância válida. Fluxo científico completo:
[docs/SCIENTIFIC_ENGINE.md](docs/SCIENTIFIC_ENGINE.md).

## Arquitetura e produto

- [Estado atual](docs/architecture/current-state.md)
- [Estado alvo](docs/architecture/target-state.md)
- [Estrutura do projeto](docs/PROJECT_STRUCTURE.md)
- [Coordenação / plano](docs/coordination/master-plan.md)
