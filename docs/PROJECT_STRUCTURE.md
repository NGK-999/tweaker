# Estrutura do projeto

ApexTweaker e um utilitario Windows (.NET 10) focado em performance, telemetria de frametime, backup e rollback reversivel. A shell ativa e **WPF**; codigo WinForms legado ainda compila no mesmo assembly, mas nao e iniciado por `Program.cs`.

Versao atual: **2.0.1**.

## Pastas principais

### `src/App`

- Entrada da aplicacao.
- `Program.cs`: disclaimer WPF, loading, `MainWindow`.
- `AppInfo.cs`: nome, versao e creditos.

### `src/UI/Wpf`

Shell WPF ativa (unica UI do app):

- `MainWindow.xaml` — janela principal com sidebar e host de paginas.
- `Views/DashboardView` — Auto-Tuning e restore point.
- `Views/ModulesView` — modulos individuais de otimizacao.
- `Views/TelemetryView` — teste A/B, grafico, metricas e console.
- `Views/UtilitiesView` — revert, desinstalar, sobre, suporte Riot.
- `Windows/StartupDisclaimerWindow` — aviso legal inicial.
- `Windows/LoadingWindow` — warmup de hardware durante o boot da UI.
- `Animations/PageTransitionAnimator` — transicoes sequenciais entre paginas.
- `ApplicationWarmup.cs` — cache de diagnostico no loading.

### `src/UI` (auxiliar)

- `TelemetryPipeClient.cs` — cliente IPC de telemetria.

### `src/Services`

Regras e acoes do backend:

| Arquivo | Funcao |
|---------|--------|
| `TweakService.cs` | Fachada de presets e modulos |
| `OptimizationEngine.cs` | Classificacao de hardware e presets permitidos |
| `SystemDiagnosticsService.cs` | Coleta de informacoes do Windows |
| `MutationExecutor.cs` | Pipeline central de mutacoes |
| `BackupService.cs` | Snapshots, ledger e backups granulares |
| `MasterRollbackService.cs` | Rollback transacional LIFO |
| `SystemRestoreService.cs` | Ponto de restauracao do Windows |
| `RegistryService.cs` | Leitura/escrita no Registro |
| `GpuOptimizationService.cs` | Planos de mutacao de GPU |
| `HardwareTelemetryService.cs` | Sensores, snapshots e sessoes A/B |
| `EtwFrameTracker.cs` | Frametime via ETW (DxgKrnl) |
| `ValorantLocator.cs` | Localiza executavel do VALORANT |
| `ValorantProcessOptimizer.cs` | Afinidade/prioridade em processos do jogo |
| `WindowsPowerModeService.cs` | Planos de energia |
| `TelemetryPipeServer.cs` | Servidor IPC de telemetria |

Comandos estruturados em `ExtremeMutationCommands.cs` e `MemoryCompressionTweakCommand.cs`.

### `src/Core/Pipeline`

Comandos isolados que implementam `ISystemMutationCommand`:

- `ProcessorIdleStatesTweakCommand`
- `EdgeRemovalTweakCommand`
- `NetworkInterruptModerationTweakCommand`

### `src/Models`

Tipos de dados: `HardwareInfo`, `HardwareTier`, `PresetKind`, `PresetRecommendation`, `CommandResult`, `TweakBackup`, `RegistryBackupEntry`, `TweakMutationSession`, `GpuInfo`.

### `src/Infrastructure`

- `CommandRunner.cs` — execucao de PowerShell, powercfg e processos externos.

### `src/NativeInterop`

- P/Invoke para `ApexTweaker.Native.dll` (topologia de CPU e afinidade).

### `native/ApexTweaker.Native`

DLL C++ compilada no build:

- `cpu_topology.cpp`, `affinity_engine.cpp`
- Exporta `AT_GetCpuTopology` e `AT_BuildPreferredGameAffinityMask`.

### `installer`

- `ApexTweaker.iss` — script Inno Setup para `ApexTweaker-Setup.exe`.

### `scripts`

- `Build-Installer.ps1`, `Convert-PngToIco.ps1`, `Create-GitHubRepo.ps1`.

### `release-v2`

Pasta oficial de distribuicao portatil:

- `ApexTweaker.exe` (single-file, self-contained)
- `ApexTweaker.Native.dll` (copiada junto ao publicar)

### `release-installer`

Saida do instalador Inno Setup (`ApexTweaker-Setup.exe`).

## Pipeline de mutacao

Toda alteracao de sistema deve passar por:

```
Validate -> Snapshot -> Execute -> Verify/ReadBack -> Log
```

Orquestrado por `MutationExecutor` com ledger em `BackupService`. Nenhuma mutacao deve retornar sucesso apenas porque um comando terminou com exit code zero; o estado real deve ser relido.

## Dados em disco

| Caminho | Conteudo |
|---------|----------|
| `C:\ProgramData\ApexTweaker\Backups` | Backups granulares, ledger de mutacoes, sessoes de telemetria |
| `C:\ProgramData\ApexTweaker\Logs\latest_runtime.log` | Log da sessao em execucao |

## Pastas geradas pelo .NET

- `bin` — saida compilada.
- `obj` — temporarios de build.

Podem ser apagadas sem perder codigo-fonte.

## Solucao e projeto

- Solucao: `ApexTweaker.sln`
- Projeto: `ApexTweaker.csproj` (assembly `ApexTweaker`)
