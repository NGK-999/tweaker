# Estado atual da arquitetura — ApexTweaker

Data da auditoria: 2026-07-24  
Auditor: Orquestrador (Cursor)  
Fonte: código no working tree + `README.md` + `docs/PROJECT_STRUCTURE.md` + `docs/ARCHITECTURE_V3.md` + `docs/BACKEND_ARCHITECTURE_AUDIT.md` + graphify

## Resumo executivo

O ApexTweaker **não** é uma aplicação com frontend e backend separados por HTTP.
É um host **.NET 10 + WPF** (versão **3.3.1**) com DLL nativa C++ opcional
(`native/ApexTweaker.Native`).

Historicamente era **um único assembly**. Durante esta auditoria o Codex iniciou
(WIP não commitado) a divisão em projetos:

| Projeto | Papel aparente (WIP) |
|---------|----------------------|
| `ApexTweaker.csproj` | host WPF |
| `src/ApexTweaker.Contracts` | contratos/DTOs de otimização Windows |
| `src/ApexTweaker.Application` | catálogo, recomendação, fachada, self-test |
| `src/ApexTweaker.Windows` | inventário Windows (implementação) |

Essa migração estava **em andamento** no working tree; ownership abaixo trata
os novos projetos como backend/contratos sob regras do orquestrador.

- **Ponto de entrada:** `src/App/Program.cs`
- **Shell UI:** `src/UI/Wpf/MainWindow.xaml(.cs)` (+ partial `MainWindow.Minecraft.cs` no WIP)
- **Backend in-process:** `src/Services`, `src/Core`, `src/Minecraft`, `src/Infrastructure`, + novos projetos Application/Windows
- **Contratos:** `src/Models` (legado) + `src/ApexTweaker.Contracts` (novo WIP)
- **IPC limitado:** named pipe de telemetria (`TelemetryPipeServer` / `TelemetryPipeClient`)
- **Sem:** `package.json`, Electron/Tauri, controllers HTTP, OpenAPI, projeto de testes xUnit/NUnit

## Árvore relevante (confirmada)

```text
ApexTweaker.sln
ApexTweaker.csproj          # WinExe, net10.0-windows, UseWPF=true
native/ApexTweaker.Native/  # C++ topologia/afinidade
src/
  App/                      # Program.cs, ApplicationPaths, DemoSafetySelfTest
  Core/Pipeline/            # comandos estruturados de mutação
  Infrastructure/           # CommandRunner, RuntimeMode
  Models/                   # DTOs/enums compartilhados
  Minecraft/                # motor Minecraft isolado (models + services + CLI)
  NativeInterop/            # P/Invoke
  Services/                 # tweaks Windows, backup, telemetria, otimização
  UI/Wpf/                   # Views, ViewModels, Themes, Windows
scripts/                    # Build-Release, Build-Installer, Test-Release
docs/                       # documentação de produto e arquitetura
```

## Pontos de entrada

| Entrada | Comportamento | Status |
|---------|---------------|--------|
| GUI WPF | Disclaimer → Loading → `MainWindow` | confirmado |
| CLI Minecraft | `MinecraftCommandLine.TryRun(args)` | confirmado |
| `--demo` / `--demo-self-test` | ativa `RuntimeMode.IsDemo`; self-test de classificação | confirmado (WIP não commitado) |
| `--minecraft-self-test` | self-test Minecraft | confirmado |

## Fluxo UI → backend (in-process)

`MainWindow` instancia diretamente ~20 serviços (`new TweakService()`, etc.) e
chama métodos públicos que retornam `IReadOnlyList<string>` (log de linhas).

Exemplos confirmados em `MainWindow.xaml.cs`:

- Auto-Tuning → `tweakService.ApplyAutonomousOptimization(...)`
- Módulos → `ApplyPowerTweaks`, `ApplyCpuSchedulerTweaks`, `ApplyGpuDisplayTweaks`, …
- Políticas/Serviços → `ApplyPolicyAndServiceTweaks()` (legado agressivo)
- Backup / Restore → `BackupService` / `MasterRollbackService`
- Minecraft → serviços em `src/Minecraft/Services`

Não há camada de API HTTP. O “contrato” atual é a **superfície pública C#** dos
serviços + records/enums em `Models`.

## Presets e otimização Windows — duas gerações

### Legado (em produção na UI)

| Peça | Arquivo | Papel |
|------|---------|-------|
| `PresetKind` | `Safe` / `Competitive` / `Extreme` | classificação por hardware |
| `OptimizationEngine` | recomenda preset por RAM/cores | |
| `TweakService` | aplica power/CPU/GPU/rede/políticas | |
| `ApplyPolicyAndServiceTweaks` | reg + desliga serviços Xbox/Search/WER | risco alto |

### Novo (WIP Codex, ainda não ligado à UI)

| Peça | Arquivo | Papel |
|------|---------|-------|
| `WindowsOptimizationPreset` | 5 presets gamer | |
| `WindowsOptimizationCatalog` | regras ADMX tipadas | |
| `WindowsOptimizationInventoryService` | inventário SO/hardware/MDM/OneDrive | |
| `WindowsOptimizationRecommendationService` | decide Recommended/Blocked/… | |
| `WindowsOptimizationService.Analyze` | fachada de análise | **sem Apply/LGPO ainda** |

## Estado Git (no momento da auditoria / atualização)

Branch: `main` tracking `origin/main` @ `c3d4733`.

Alterações locais relevantes (não commitadas — **não descartar**):

- Modificados: `ApexTweaker.csproj`, `ApexTweaker.sln`, `Program.cs`, `CommandRunner.cs`, `MutationExecutor.cs`, `RegistryService.cs`, `MainWindow.xaml.cs`, `MinecraftSelfTest.cs`
- Remoções/movimentações WIP: `GpuInfo.cs`, `NativeMethods.cs`, `HardwareEnvironmentDetector.cs`, `IntelHybridProbeStrategy.cs` (verificar se migraram para os novos projetos antes de restaurar)
- Novos: `src/ApexTweaker.Application/**`, `src/ApexTweaker.Contracts/**`, `src/ApexTweaker.Windows/**`, `RuntimeMode.cs`, `DemoSafetySelfTest.cs`, `WindowsOptimizationService.cs` (App), partial Minecraft UI, docs de coordenação

## Acoplamentos e riscos

1. **`MainWindow.xaml.cs` ~2177 linhas** — mistura navegação UI, orquestração e chamadas de domínio.
2. **`TweakService` ~1310 linhas** — fachada + mutação + verificação.
3. **Dois modelos de preset incompatíveis** (`PresetKind` vs `WindowsOptimizationPreset`) sem ponte.
4. **Políticas legadas conflitam** com o catálogo novo (ex.: desligar Xbox/Game Pass).
5. **Sem projeto de testes unitários**; validação = self-tests CLI + `scripts/Test-Release.ps1`.
6. **UI pode chamar infraestrutura** (assembly único); não há fronteira de compilação FE/BE.
7. Documentação `PROJECT_STRUCTURE.md` menciona WinForms legado; **não encontrado** no tree atual (possível doc desatualizado).

## Build / lint / testes (confirmados)

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet publish ApexTweaker.csproj -c Release -r win-x64 --self-contained true -o release-v2
dotnet run --project ApexTweaker.csproj -- --minecraft-self-test
dotnet run --project ApexTweaker.csproj -- --demo-self-test
.\scripts\Test-Release.ps1
```

- **Lint dedicado:** não encontrado (sem ESLint/StyleCop explícito no repo).
- **Requisito nativo:** VS Build Tools + C++ para `ApexTweaker.Native.dll`.

## O que NÃO existe (confirmado ausente)

- Servidor HTTP / REST / GraphQL
- OpenAPI / Swagger
- Electron, Tauri, React host
- `package.json` / lockfiles JS
- Controllers, rotas, schemas Zod/JSON Schema de API
- Suite xUnit/NUnit/MSTest

Qualquer plano que assuma “endpoints REST entre FE e BE” está **fora da realidade atual**.
