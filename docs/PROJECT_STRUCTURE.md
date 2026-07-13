# Estrutura do projeto

ApexTweaker e um utilitario Windows (.NET 10) focado em performance, telemetria de frametime, backup e rollback reversivel. A shell ativa e **WPF**; codigo WinForms legado ainda compila no mesmo assembly, mas nao e iniciado por `Program.cs`.

Versao atual: **3.3.0**.

## Pastas principais

### `src/App`

- Entrada da aplicacao.
- `Program.cs`: disclaimer WPF, loading, `MainWindow`.
- `AppInfo.cs`: nome, versao e creditos.
- `ApplicationPaths.cs`: separa dados de usuario em LocalAppData dos backups
  administrativos do Windows em ProgramData.

### `src/UI/Wpf`

Shell WPF ativa (unica UI do app):

- `MainWindow.xaml` — janela principal com sidebar e host de paginas.
- `Views/DashboardView` — Auto-Tuning e restore point.
- `Views/ModulesView` — modulos individuais de otimizacao.
- `Views/TelemetryView` — teste A/B, grafico, metricas e console.
- `Views/MinecraftView` - diagnostico cientifico, experimentos, auditoria,
  host da experiencia facil e do laboratorio avancado.
- `Views/CobblemonEasyView` - nome interno legado; hospeda o fluxo Minecraft
  geral com seis acoes, hooks de sessao, restauracao e diagnostico.
- `ViewModels/CobblemonEasyViewModel.cs` - nome interno legado preservado para
  reduzir risco de quebra XAML; estados e mensagens sao Minecraft gerais.
- `ViewModels/MinecraftWizardViewModel.cs` - estado, navegacao, progresso,
  cancelamento e estados visuais via CommunityToolkit.Mvvm.
- `Controls/LightweightBenchmarkChart.cs` - grafico WPF sem SkiaSharp.
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
| `ApplicationPrivilegeService.cs` | classifica operacoes e solicita elevacao sob demanda |

Comandos estruturados em `ExtremeMutationCommands.cs` e `MemoryCompressionTweakCommand.cs`.

### `src/Minecraft`

Modulo isolado para Minecraft/Cobblemon:

- `Models/MinecraftAuditModels.cs` - contratos de auditoria, perfil, quarentena, benchmark e homologacao.
- `Models/MinecraftScientificModels.cs` - contratos de evidencia, gargalo,
  hipotese, medicao, comparacao, decisao e experimento persistente.
- `Services/ModJarScanner.cs` - metadados Fabric/Forge/NeoForge e JARs aninhados.
- `Services/MinecraftAuditService.cs` - classificacao, dependencias e conflitos.
- `Services/MinecraftEnvironmentService.cs` - hardware, Java, pagefile e launchers.
- `Services/MinecraftInstanceService.cs` - deteccao de launchers e raiz real da instancia.
- `Services/MinecraftProfileService.cs` - dry-run, configs, memoria, backup e rollback.
- `Services/MinecraftExtremeExperimentCatalog.cs` - Potato e hipoteses
  fechadas de resolucao, FPS, chunks, entidades, resource packs e heap.
- `Services/MinecraftQuarantineService.cs` - movimentacao reversivel de JARs com SHA-256.
- `Services/MinecraftSurvivalPlanService.cs` - veredito e plano manual para 4 GB.
- `Services/MinecraftBenchmarkService.cs` - ambiente, processo, configs, logs e crashes.
- `Services/MinecraftOperationalHomologationService.cs` - checklist e avaliacao da rodada real.
- `Services/MinecraftReportService.cs` - JSON, Markdown e TXT.
- `Services/MinecraftInstanceEvidenceService.cs` - hashes de configs/mods,
  opcoes vanilla e resource packs ativos.
- `Services/MinecraftBottleneckDiagnosticService.cs` - regras de diagnostico
  rastreaveis e nivel de confianca.
- `Services/MinecraftModConfigContractCatalog.cs` - contratos de configuracao
  suportados, manuais ou sem necessidade de escrita.
- `Services/MinecraftScientificMetricsService.cs` - consolida telemetria,
  observacao guiada, logs e resultados detalhados.
- `Services/MinecraftScientificComparisonService.cs` - limiares, pesos,
  regressao critica e decisao `KEEP`/`REVERT`/`RETEST`.
- `Services/MinecraftScientificAutoOptimizeService.cs` - plano conservador por
  gargalo; nunca movimenta mods automaticamente.
- `Services/MinecraftScientificExperimentStore.cs` - armazenamento JSON atomico
  e validacao de identificadores.
- `Services/MinecraftScientificExperimentService.cs` - maquina de estados do
  baseline ate a finalizacao e rollback pelo backup exato.
- `Services/MinecraftScientificReportService.cs` - relatorios cientificos JSON,
  Markdown e TXT.
- `MinecraftCommandLine.cs` - automacao headless.
- `MinecraftSelfTest.cs` - teste integrado sem pacotes externos.

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
| `%LOCALAPPDATA%\ApexTweaker\MinecraftBackups` | Backups de perfis de instancia Minecraft |
| `%LOCALAPPDATA%\ApexTweaker\MinecraftQuarantineBackups` | Backups e manifestos de JARs em quarentena |
| `%LOCALAPPDATA%\ApexTweaker\MinecraftReports` | Auditorias e benchmarks Minecraft |
| `%LOCALAPPDATA%\ApexTweaker\MinecraftExperiments` | Estado e relatorios dos experimentos cientificos |
| `%LOCALAPPDATA%\ApexTweaker\Telemetry` | Sessoes de telemetria sem administrador |

## Pastas geradas pelo .NET

- `bin` — saida compilada.
- `obj` — temporarios de build.

Podem ser apagadas sem perder codigo-fonte.

## Solucao e projeto

- Solucao: `ApexTweaker.sln`
- Projeto: `ApexTweaker.csproj` (assembly `ApexTweaker`)
