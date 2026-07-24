# Graph Report - Apextweaker  (2026-07-24)

## Corpus Check
- 163 files · ~130,530 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2769 nodes · 5759 edges · 157 communities (141 shown, 16 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 324 edges (avg confidence: 0.74)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `506f92ba`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- MainWindow
- TweakService
- ApexTweaker.Models
- CpuTopologyNative
- WindowsOptimizationModels.cs
- Contexto completo do ApexTweaker
- Window
- UserControl
- Funcionalidades do ApexTweaker
- WindowsOptimizationInventoryService
- Plano mestre de coordenação
- .Run
- UtilitiesView
- ApexTweaker
- Contrato FE ↔ BE (in-process)
- Arquitetura-alvo — ApexTweaker
- UserControl
- Arquitetura do backend
- Auditoria estrutural do backend
- Frontend handoff — L2 (Otimização Windows / Presets Gamer)
- Estado atual da arquitetura — ApexTweaker
- Backend handoff (template)
- Matriz de propriedade (ownership)
- Integration status
- Market coverage matrix — ApexTweaker vs EXM / BoosterX
- Backend Task — Market coverage B1–B8 (modo A)
- Frontend Task — Catalogo B9 (modo A)
- AGENTS.md
- README.md
- MarketUtilitiesService
- WindowsOptimizationInventoryService.cs
- MainWindow.xaml.cs
- TweakService.cs
- MinecraftModDescriptor
- PageTransitionAnimator
- UserControl
- TelemetryView
- UserControl
- AT_CPU_TOPOLOGY
- MinecraftAuditResult
- BackupService.cs
- MinecraftScientificExperimentService
- CobblemonEasyViewModel
- MinecraftView
- LightweightBenchmarkChart
- CobblemonEasyView
- RoutedEventArgs
- MinecraftScientificModels.cs
- MainWindow
- HardwareTelemetryService
- ApexTweaker.Minecraft.Models
- ExtremeMutationCommands.cs
- MinecraftProfileService
- TweakService
- MinecraftBenchmarkService
- SystemDiagnosticsService
- ValorantProcessOptimizer
- MinecraftEnvironmentService
- GpuOptimizationService
- MinecraftWizardViewModel
- ResourceDictionary
- .Run
- MinecraftScientificReportService
- HardwareTelemetryService.cs
- TelemetrySnapshot
- TelemetryPipeServer
- EtwFrameTracker
- UserControl
- Pastas principais
- .SetCurrentStep
- MutationSession
- Minecraft Scientific Optimization Engine
- KernelLatencyTracker
- AffinityIsolationCommand
- .BuildPlan
- MinecraftAuditModels.cs
- MinecraftInstanceDescriptor
- WindowsPowerModeService
- EdgeRemovalTweakCommand
- FabricVersionConstraint
- .ExecuteCommand
- WindowsOptimizationModels.cs
- MinecraftCommandLine
- CpuTopologyProfile
- .BuildKeyValueMutation
- .MonitorLoopAsync
- MinecraftProfilePlan
- DashboardView
- TelemetryPipeClient
- ISystemMutationCommand
- OptimizationEngine
- MutationExecutor
- README.md
- Cobblemon Low-End Lab v3.3.1
- Homologacao operacional Cobblemon em 4 GB
- MinecraftEasyModeService
- NetworkInterruptModerationTweakCommand
- TextBox
- .AddSystemRestorePointIfCurrentRootMutation
- .ReadPageFile
- Program.cs
- Minecraft geral e hooks de sessao - v3.3.1
- HardwareInfo
- Execution log
- WindowsOptimizationService
- WindowsOptimizationRule
- .CaptureGamingPerformanceProbe
- MinecraftEasyCorrectionPlan
- MinecraftInstanceService
- CreateEmergencyRestoreScript
- .ProbeJava
- .Capture
- .RunTweakAsync
- ModulesView
- .BuildOperation
- MemoryCompressionTweakCommand
- UserControl
- PerformanceView
- Primeiro teste seguro em 4 GB
- ApexTweaker v3.3.1 - Minecraft Rapido
- MinecraftEnvironmentService.cs
- ApexTweaker
- ProcessorIdleStatesTweakCommand
- .ExecuteAsync
- WpfUserControl
- EasyFpsComboBox
- Distribuicao
- Inspirações FE — maturidade “nível corporação”
- MinecraftEasyModSummary
- .GetProcessIoCounters
- MinecraftBenchmarkService.cs
- .EnsureAdministratorForWindowsOperation
- ValorantLocator
- TestResultPanel
- Master plan — sprint FE-ALL + FPS-BE
- Documentação ApexTweaker
- MinecraftBenchmarkSample
- TweakBackup
- GameOpenedCheckBox
- MinecraftDiagnosticPackageResult
- ValorantProcessOptimizer.cs
- TelemetryPipeClient.cs
- Research note — referências do usuário vs FPS
- .TryFindJsonProperty
- DevMode
- AppInfo.cs
- QuarantineList

## God Nodes (most connected - your core abstractions)
1. `MainWindow` - 134 edges
2. `CobblemonEasyViewModel` - 89 edges
3. `TweakService` - 88 edges
4. `UserControl` - 80 edges
5. `HardwareTelemetryService` - 79 edges
6. `MinecraftView` - 64 edges
7. `UserControl` - 58 edges
8. `MinecraftProfileService` - 55 edges
9. `ApexTweaker.Models` - 39 edges
10. `ApexTweaker.Services` - 37 edges

## Surprising Connections (you probably didn't know these)
- `UserControl` --references--> `CorrectionDetails`  [INFERRED]
  src/UI/Wpf/Views/CobblemonEasyView.xaml → src/UI/Wpf/ViewModels/CobblemonEasyViewModel.cs
- `MinecraftView` --references--> `ScientificExperimentPhase`  [EXTRACTED]
  src/UI/Wpf/Views/MinecraftView.xaml.cs → src/Minecraft/Models/MinecraftScientificModels.cs
- `BeginMutationSession()` --references--> `MutationSession`  [EXTRACTED]
  src/Services/BackupService.cs → src/Models/TweakMutationSession.cs
- `CaptureActivePowerScheme()` --references--> `MutationSession`  [EXTRACTED]
  src/Services/BackupService.cs → src/Models/TweakMutationSession.cs
- `CaptureCommandState()` --references--> `MutationSession`  [EXTRACTED]
  src/Services/BackupService.cs → src/Models/TweakMutationSession.cs

## Import Cycles
- None detected.

## Communities (157 total, 16 thin omitted)

### Community 2 - "ApexTweaker.Models"
Cohesion: 0.14
Nodes (12): ApexTweaker.Services, ApexTweaker, ApexTweaker.Application.Optimizations, ApexTweaker.Infrastructure, ApexTweaker.Models, ApexTweaker.Windows.Inventory, ApexTweaker.Core.Pipeline, UninstallTarget (+4 more)

### Community 3 - "CpuTopologyNative"
Cohesion: 0.05
Nodes (40): CoreGroupMapping, ApexTweaker.NativeInterop, LOGICAL_PROCESSOR_RELATIONSHIP, HardwareEnvironmentDetectionResult, HardwareEnvironmentDetector, HashSet, CoreGroupMapping, IntelHybridProbeStrategy (+32 more)

### Community 4 - "WindowsOptimizationModels.cs"
Cohesion: 0.21
Nodes (9): WindowsOptimizationSelfTest, IReadOnlyList, WindowsDeviceKind, WindowsOptimizationContext, WindowsOptimizationPlan, WindowsOptimizationPreset, WindowsPowerSource, WindowsUsageProfile (+1 more)

### Community 5 - "Contexto completo do ApexTweaker"
Cohesion: 0.16
Nodes (10): BiosChecklistCatalog, BiosChecklistItem, IReadOnlyList, CatalogRowViewModel, CatalogViewModel, SelectedPreset, StatusText, IReadOnlyList (+2 more)

### Community 6 - "Window"
Cohesion: 0.07
Nodes (31): Subtitle, MouseButtonEventArgs, AppVersionText, CatalogButton, CloseButton, CommandPaletteList, CommandPaletteOverlay, DashboardButton (+23 more)

### Community 7 - "UserControl"
Cohesion: 0.12
Nodes (16): Category, Guidance, KindLabel, Reason, RiskLabel, RiskNote, Vendor, BiosList (+8 more)

### Community 8 - "Funcionalidades do ApexTweaker"
Cohesion: 0.06
Nodes (32): Background, Backup e rollback, Catalogo, Cobblemon / Minecraft, Comandos estruturados adicionais, CPU/Scheduler, Criar restore point, Dashboard (+24 more)

### Community 9 - "WindowsOptimizationInventoryService"
Cohesion: 0.12
Nodes (11): ComputerInventory, DeviceGuardInventory, OperatingSystemInventory, ProcessorInventory, WindowsOptimizationInventoryService, DllImport, IReadOnlyList, MarshalAs (+3 more)

### Community 10 - "Plano mestre de coordenação"
Cohesion: 0.20
Nodes (9): Backend / orquestração — `.agents/skills/`, Codex / agents (usuário), Como o orquestrador seleciona skills, Frontend — `.claude/skills/`, Política, Projeto ApexTweaker, Skills já instaladas na máquina (observadas), Skills planejadas (criar depois da aprovação) (+1 more)

### Community 11 - ".Run"
Cohesion: 0.10
Nodes (15): MinecraftSelfTest, Action, Color, IReadOnlyDictionary, MinecraftBenchmarkResult, MinecraftEnvironmentSnapshot, MinecraftOperationalHomologationResult, MinecraftOperationalObservation (+7 more)

### Community 12 - "UtilitiesView"
Cohesion: 0.17
Nodes (12): AboutButton, CleanTempButton, RepairButton, RevertButton, RiotSupportButton, StorageSenseButton, TrimButton, UninstallButton (+4 more)

### Community 13 - "ApexTweaker"
Cohesion: 0.14
Nodes (14): ApexTweaker, net10.0-windows, System.Management (10.0.2), Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), LibreHardwareMonitorLib (0.9.6), Microsoft.Diagnostics.Tracing.TraceEvent (3.1.21), net10.0 (+6 more)

### Community 14 - "Contrato FE ↔ BE (in-process)"
Cohesion: 0.12
Nodes (15): 1. Transporte, 2.1 Diagnóstico, 2.2 Mutações Windows (legado), 2.3 Backup / rollback, 2.4 Minecraft, 2. Superfície legada usada pela UI (`MainWindow`), 3.1 Entrada, 3.2 Enums (confirmados no models WIP) (+7 more)

### Community 15 - "Arquitetura-alvo — ApexTweaker"
Cohesion: 0.13
Nodes (14): Arquitetura-alvo — ApexTweaker, Critérios de sucesso da arquitetura-alvo, Fases de migração (sem big-bang), Fronteira FE ↔ BE (alvo imediato), M0 — Congelar contratos (esta entrega), M1 — Completar backend Windows Optimization (análise), M2 — Fachada + UI demo, M3 — Apply via LGPO + rollback (+6 more)

### Community 16 - "UserControl"
Cohesion: 0.29
Nodes (5): ApexTweaker.UI.Wpf.Theming, ApexTweaker.UI.Wpf.Views, ApexTweaker.UI.Wpf.ViewModels, CommandPaletteItem, EasyPrimaryAction

### Community 17 - "Arquitetura do backend"
Cohesion: 0.14
Nodes (13): ApexTweaker, ApexTweaker.Application, ApexTweaker.Contracts, ApexTweaker.Native, ApexTweaker.Windows, Arquitetura do backend, Compatibilidade, Estado atual (+5 more)

### Community 18 - "Auditoria estrutural do backend"
Cohesion: 0.14
Nodes (13): Acoplamento, Arquivos da primeira etapa, Auditoria estrutural do backend, Codigo grande e duplicacao, Contratos, Escopo e restricoes, Estrutura observada, Estrutura proposta (+5 more)

### Community 19 - "Frontend handoff — L2 (Otimização Windows / Presets Gamer)"
Cohesion: 0.29
Nodes (6): Arquivos principais (UI), Como testar, Frontend Handoff — FE-ALL-P0 (+ integração orquestrador), Pendências leves, Resumo, Wiring BE (orquestrador)

### Community 20 - "Estado atual da arquitetura — ApexTweaker"
Cohesion: 0.17
Nodes (12): Acoplamentos e riscos, Build / lint / testes (confirmados), Estado atual da arquitetura — ApexTweaker, Estado Git (no momento da auditoria / atualização), Fluxo UI → backend (in-process), Legado (em produção na UI), Novo (WIP Codex, ainda não ligado à UI), O que NÃO existe (confirmado ausente) (+4 more)

### Community 21 - "Backend handoff (template)"
Cohesion: 0.15
Nodes (12): Arquivos criados, Arquivos modificados, Arquivos removidos, Backend handoff, Commit, Contratos afetados, Decisoes tomadas, Erros restantes (+4 more)

### Community 22 - "Matriz de propriedade (ownership)"
Cohesion: 0.25
Nodes (7): Agentes, Arquivos atualmente misturados (atenção), Matriz de propriedade (ownership), Matriz por caminho, Proibições absolutas, Resolução de conflito de ownership, Worktrees (modo automatizado)

### Community 23 - "Integration status"
Cohesion: 0.33
Nodes (5): Commits, Entregue nesta rodada, Integration status, Pendencias conhecidas, Verificacao

### Community 24 - "Market coverage matrix — ApexTweaker vs EXM / BoosterX"
Cohesion: 0.33
Nodes (5): Categorias cobertas (menu EXM Free), Decisões, Explicitamente fora, Mapa de lotes, Market coverage matrix — ApexTweaker vs EXM / BoosterX

### Community 25 - "Backend Task — Market coverage B1–B8 (modo A)"
Cohesion: 0.29
Nodes (6): Backend Task — FPS P0/P1 (modo A), Contexto, Estado parcial (continuar, não recomeçar), Handoff, Idempotência, Verificação

### Community 26 - "Frontend Task — Catalogo B9 (modo A)"
Cohesion: 0.22
Nodes (8): Fora de escopo, Frontend Task — FE-ALL-P0 (FPS painel + maturidade corporativa), Handoff, Já parcial neste branch, Parte A — Painel Desempenho / FPS, Parte B — Maturidade P0 (obrigatório), Parte C — Maturidade P1 (se tempo), Verificação

### Community 32 - "MarketUtilitiesService"
Cohesion: 0.33
Nodes (4): MarketCoverageSelfTest, MarketUtilitiesService, IEnumerable, IReadOnlyList

### Community 33 - "WindowsOptimizationInventoryService.cs"
Cohesion: 0.20
Nodes (8): ApexTweaker.Contracts.Inventory, ComputerInventory, DeviceGuardInventory, OperatingSystemInventory, ProcessorInventory, SystemPowerStatus, byte, uint

### Community 34 - "MainWindow.xaml.cs"
Cohesion: 0.22
Nodes (5): ApplicationPaths, IReadOnlyList, GamingFpsProbeSelfTest, Program, STAThread

### Community 35 - "TweakService.cs"
Cohesion: 0.06
Nodes (39): EventHandler, ProcessIds, ProcessPowerThrottlingState, FakeMinecraftSessionHookPlatform, IReadOnlyList, MinecraftSessionHookAction, MinecraftSessionHookMode, MinecraftSessionHookReport (+31 more)

### Community 36 - "MinecraftModDescriptor"
Cohesion: 0.05
Nodes (36): FileInfo, JsonDocument, MinecraftAuditIssue, MinecraftLoader, MinecraftModDescriptor, MinecraftQuarantineApplyResult, MinecraftQuarantineConfirmation, MinecraftQuarantineRollbackResult (+28 more)

### Community 37 - "PageTransitionAnimator"
Cohesion: 0.06
Nodes (39): ContentControl, PageTransitionAnimator, CancellationToken, DependencyObject, DependencyProperty, DoubleAnimation, FrameworkElement, IEasingFunction (+31 more)

### Community 38 - "UserControl"
Cohesion: 0.05
Nodes (57): BackCommand, BenchmarkPoints, Color, ComparisonSummary, CurrentStep.Description, CurrentStep.Detail, CurrentStep.StateColor, CurrentStep.StateLabel (+49 more)

### Community 39 - "TelemetryView"
Cohesion: 0.05
Nodes (34): Foreground, Text, ConsoleLineViewModel, PointCollection, Queue, SizeChangedEventArgs, TelemetryMetricsSnapshot, BenchmarkButton (+26 more)

### Community 40 - "UserControl"
Cohesion: 0.04
Nodes (48): ApproximateFps, BenchmarkSummary, ClosedAlone, CorrectionDetails, DestinationQuestion, DetectStep.IsCurrent, DetectStep.StateLabel, FixStep.IsCurrent (+40 more)

### Community 41 - "AT_CPU_TOPOLOGY"
Cohesion: 0.08
Nodes (46): DWORD, KAFFINITY, AT_BuildPreferredGameAffinityMask(), CopyEntry(), AT_API, uint32_t, AddOrMergeEntry(), AT_GetCpuTopology() (+38 more)

### Community 42 - "MinecraftAuditResult"
Cohesion: 0.10
Nodes (18): BottleneckCandidate, MinecraftAuditResult, MinecraftOperationalChecklist, MinecraftQuarantinePlan, MinecraftReportPaths, MinecraftSurvivalPlan, MinecraftBottleneckKind, ScientificDerivedMetrics (+10 more)

### Community 43 - "BackupService.cs"
Cohesion: 0.08
Nodes (38): IGrouping, RegistryBackupEntry, BeginMutationSession(), BuildUniqueMutationLedgerPath(), CaptureActivePowerScheme(), CaptureBcdEntries(), CaptureCommandState(), CaptureDisplayDriverEntries() (+30 more)

### Community 44 - "MinecraftScientificExperimentService"
Cohesion: 0.13
Nodes (13): MinecraftScientificExperiment, MinecraftScientificOperationResult, ScientificHypothesis, MinecraftScientificExperimentService, DateTimeOffset, HashSet, IReadOnlyDictionary, IReadOnlyList (+5 more)

### Community 45 - "CobblemonEasyViewModel"
Cohesion: 0.06
Nodes (26): MinecraftEasyState, CobblemonEasyViewModel, AuditReady, CorrectionDetails, DuplicateMods, EssentialMods, HasBackup, HeavyMods (+18 more)

### Community 46 - "MinecraftView"
Cohesion: 0.08
Nodes (7): CustomExperimentCheckBox, SaveHomologationButton, ScientificAdvanceButton, MinecraftView, bool, int, SelectionChangedEventArgs

### Community 47 - "LightweightBenchmarkChart"
Cohesion: 0.07
Nodes (25): Border, ApexTweaker.UI.Wpf.Controls, DrawingContext, FrameworkElement, INotifyCollectionChanged, NotifyCollectionChangedEventArgs, BenchmarkChartPoint, LightweightBenchmarkChart (+17 more)

### Community 48 - "CobblemonEasyView"
Cohesion: 0.12
Nodes (9): EasyPrimaryAction, ExportButton, PrimaryActionButton, RestoreButton, SaveTestButton, CobblemonEasyView, RoutedEventArgs, Task (+1 more)

### Community 49 - "RoutedEventArgs"
Cohesion: 0.09
Nodes (17): AdvancedModeTabButton, ApplyProfileButton, ApplyQuarantineButton, AuditButton, BenchmarkButton, BrowseButton, CancelBenchmarkButton, EasyModeTabButton (+9 more)

### Community 50 - "MinecraftScientificModels.cs"
Cohesion: 0.11
Nodes (20): MinecraftBottleneckDiagnosis, MinecraftExperimentMeasurement, MinecraftScientificComparison, ModConfigAutomationStatus, ScientificActionKind, ScientificActionRisk, ScientificBenchmarkOutcome, ScientificConfidence (+12 more)

### Community 51 - "MainWindow"
Cohesion: 0.10
Nodes (12): CancelEventArgs, KeyEventArgs, CommandPaletteInput, MainWindow, BackupService, bool, CancellationTokenSource, Dictionary (+4 more)

### Community 52 - "HardwareTelemetryService"
Cohesion: 0.12
Nodes (9): Computer, float, KernelLatencyTracker, BenchmarkState, HardwareTelemetryService, double, JsonSerializerOptions, Task (+1 more)

### Community 53 - "ApexTweaker.Minecraft.Models"
Cohesion: 0.12
Nodes (7): ApexTweaker.Minecraft.Models, ApexTweaker.Minecraft, ApexTweaker.Minecraft.Services, BottleneckCandidate, Contract, FileMutation, ProfileOperation

### Community 54 - "ExtremeMutationCommands.cs"
Cohesion: 0.09
Nodes (17): GROUP_AFFINITY, ResolvedGpuInterruptTarget, CACHE_RELATIONSHIP, GROUP_AFFINITY, L3CacheDescriptor, LOGICAL_PROCESSOR_RELATIONSHIP, MpoTweakCommand, MsiModeTweakCommand (+9 more)

### Community 55 - "MinecraftProfileService"
Cohesion: 0.13
Nodes (11): ProfileOperation, MinecraftBackupFileEntry, MinecraftRollbackResult, MinecraftProfileService, GeneratedRegex, IReadOnlyCollection, IReadOnlyList, JsonSerializerOptions (+3 more)

### Community 56 - "TweakService"
Cohesion: 0.11
Nodes (8): SendMessageTimeoutFlags, CaptureBcdValue(), TweakService, BackupService, DllImport, int, IntPtr, string

### Community 57 - "MinecraftBenchmarkService"
Cohesion: 0.12
Nodes (16): AvailableMemoryGb, BenchmarkEvidence, CommitUsedMb, ReadBytes, MinecraftBenchmarkService, CancellationToken, DateTime, DateTimeOffset (+8 more)

### Community 58 - "SystemDiagnosticsService"
Cohesion: 0.13
Nodes (6): DevMode, SystemDiagnosticsService, DllImport, IReadOnlyList, RegistryKey, string

### Community 59 - "ValorantProcessOptimizer"
Cohesion: 0.16
Nodes (14): AffinityPlan, NativeProcessOptimizationResult, ValorantProcessOptimizer, Action, CancellationToken, DllImport, HashSet, int (+6 more)

### Community 60 - "MinecraftEnvironmentService"
Cohesion: 0.17
Nodes (6): AvailableGb, JavaMemoryRecommendation, MinecraftEnvironmentService, int, IReadOnlyList, TotalGb

### Community 61 - "GpuOptimizationService"
Cohesion: 0.16
Nodes (9): DisplayAdapterDevice, GpuInfo, GpuMutationPlan, GpuOptimizationService, IReadOnlyList, JsonElement, List, RegistryKey (+1 more)

### Community 62 - "MinecraftWizardViewModel"
Cohesion: 0.12
Nodes (12): IRelayCommand, ObservableObject, MinecraftVisualStateItem, MinecraftWizardStepState, MinecraftWizardStepViewModel, MinecraftWizardViewModel, bool, double (+4 more)

### Community 63 - "ResourceDictionary"
Cohesion: 0.14
Nodes (16): ISet, ActiveBar, ActiveOverlay, HoverOverlay, PressOverlay, ResourceDictionary, RootBorder, RootGrid (+8 more)

### Community 64 - ".Run"
Cohesion: 0.16
Nodes (8): CommandRunner, CancellationToken, Process, Task, TimeSpan, CommandResult, HypervisorTweakCommand, TimerResolutionTweakCommand

### Community 65 - "MinecraftScientificReportService"
Cohesion: 0.19
Nodes (7): MinecraftScientificReportPaths, MinecraftScientificReportService, IEnumerable, IReadOnlyDictionary, JsonSerializerOptions, StringBuilder, UTF8Encoding

### Community 66 - "HardwareTelemetryService.cs"
Cohesion: 0.13
Nodes (16): EventArgs, CpuTelemetryKind, FrametimeCorrelationEvent, GameProcessInfo, LOGICAL_PROCESSOR_RELATIONSHIP, ProcessorCoreDescriptor, SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER, TelemetryDiagnosticEventArgs (+8 more)

### Community 67 - "TelemetrySnapshot"
Cohesion: 0.17
Nodes (4): FrametimeCorrelationEvent, IHardware, ISensor, TelemetrySnapshot

### Community 68 - "TelemetryPipeServer"
Cohesion: 0.13
Nodes (13): Channel, NamedPipeServerStream, TelemetryPipeServer, bool, byte, CancellationToken, CancellationTokenSource, ConcurrentDictionary (+5 more)

### Community 69 - "EtwFrameTracker"
Cohesion: 0.12
Nodes (12): EtwFrameTracker, bool, CancellationToken, CancellationTokenSource, ConcurrentDictionary, int, long, object (+4 more)

### Community 70 - "UserControl"
Cohesion: 0.13
Nodes (15): Description, StatusGlyph, StatusKey, StatusLabel, CompetitiveModeButton, DisableVbsButton, OptimizeFullscreenButton, StatusItemsControl (+7 more)

### Community 71 - "Pastas principais"
Cohesion: 0.10
Nodes (20): Dados em disco, Estrutura do projeto, `installer`, `native/ApexTweaker.Native`, Pastas geradas pelo .NET, Pastas principais, Pipeline de mutacao, `release-installer` (+12 more)

### Community 72 - ".SetCurrentStep"
Cohesion: 0.15
Nodes (5): OperationalHomologationStatus, EasyStepState, EasyStepViewModel, bool, IEnumerable

### Community 73 - "MutationSession"
Cohesion: 0.16
Nodes (13): BcdValueSnapshot, CommandStateSnapshot, MutationSession, PowerSchemeSnapshot, PowerSettingSnapshot, ProcessStateSnapshot, RegistryValueSnapshot, ServiceStateSnapshot (+5 more)

### Community 74 - "Minecraft Scientific Optimization Engine"
Cohesion: 0.11
Nodes (19): CLI completa, Contratos de configuracao de mods, Decisao de linguagem, Diagnostico de gargalo, Fato, inferencia e recomendacao, Fluxo GUI para uma instancia Prism real, Fontes tecnicas fixadas, Limitacoes restantes (+11 more)

### Community 75 - "KernelLatencyTracker"
Cohesion: 0.14
Nodes (9): DPCTraceData, ISRTraceData, KernelLatencyTracker, Action, CancellationTokenSource, long, object, string (+1 more)

### Community 76 - "AffinityIsolationCommand"
Cohesion: 0.15
Nodes (11): L3CacheDescriptor, ProcessorIsolationTopology, AffinityIsolationCommand, ProcessorIsolationTopology, Dictionary, DllImport, HashSet, IntPtr (+3 more)

### Community 77 - ".BuildPlan"
Cohesion: 0.21
Nodes (9): Plan, Reports, MinecraftInstanceEvidence, MinecraftScientificOptimizationPlan, ModConfigContractAssessment, MinecraftModConfigContractCatalog, IReadOnlyList, MinecraftScientificAutoOptimizeService (+1 more)

### Community 78 - "MinecraftAuditModels.cs"
Cohesion: 0.13
Nodes (18): AuditSeverity, BenchmarkStatus, DiskInfo, JavaMemoryTier, MinecraftAuditSummary, MinecraftBackupManifest, MinecraftExperimentVariable, MinecraftHomologationCriterion (+10 more)

### Community 79 - "MinecraftInstanceDescriptor"
Cohesion: 0.25
Nodes (8): MinecraftDiagnosticPackageContext, MinecraftInstanceDescriptor, MinecraftProfileApplyResult, MinecraftDiagnosticPackageService, ICollection, JsonSerializerOptions, long, string

### Community 80 - "WindowsPowerModeService"
Cohesion: 0.28
Nodes (4): WindowsPowerModeService, DllImport, Guid, string

### Community 81 - "EdgeRemovalTweakCommand"
Cohesion: 0.18
Nodes (7): Arguments, FileName, EdgeRemovalTweakCommand, BackupService, bool, string, UninstallTarget

### Community 82 - "FabricVersionConstraint"
Cohesion: 0.30
Nodes (6): IComparable, FabricVersionConstraint, VersionNumber, GeneratedRegex, Regex, VersionNumber

### Community 84 - "WindowsOptimizationModels.cs"
Cohesion: 0.24
Nodes (12): WindowsOptimizationCatalog, IReadOnlyList, IReadOnlySet, AdmxPolicyReference, EvidenceLevel, OptimizationRequirement, PerformanceEvidence, ResizableBarStatus (+4 more)

### Community 86 - "CpuTopologyProfile"
Cohesion: 0.18
Nodes (9): CpuTelemetryKind, CpuTopologyProfile, ProcessorCoreDescriptor, CpuTopologyProfile, HashSet, IntPtr, List, LOGICAL_PROCESSOR_RELATIONSHIP (+1 more)

### Community 87 - ".BuildKeyValueMutation"
Cohesion: 0.28
Nodes (6): FileMutation, MinecraftProfileChangeKind, MinecraftProfileSettingChange, ICollection, IEnumerable, IReadOnlyDictionary

### Community 88 - ".MonitorLoopAsync"
Cohesion: 0.17
Nodes (6): GameProcessInfo, WindowNativeMethods, CancellationToken, DllImport, IReadOnlyList, Process

### Community 89 - "MinecraftProfilePlan"
Cohesion: 0.23
Nodes (5): MinecraftExperimentDefinition, MinecraftProfilePlan, MinecraftExtremeExperimentCatalog, int, IReadOnlyList

### Community 90 - "DashboardView"
Cohesion: 0.16
Nodes (8): AutoOptimizeButton, RestorePointButton, SummaryText, UserControl, DashboardView, RoutedEventArgs, Button, TextBlock

### Community 91 - "TelemetryPipeClient"
Cohesion: 0.15
Nodes (12): IDisposable, NamedPipeClientStream, TelemetryPipeClient, bool, CancellationToken, CancellationTokenSource, int, JsonSerializerOptions (+4 more)

### Community 92 - "ISystemMutationCommand"
Cohesion: 0.19
Nodes (5): List, ISystemMutationCommand, SystemMutationCommand, Action, BackupService

### Community 93 - "OptimizationEngine"
Cohesion: 0.23
Nodes (6): CpuArchitectureProfile, OptimizationEngine, ProcessorBoostDecision, Action, double, string

### Community 94 - "MutationExecutor"
Cohesion: 0.19
Nodes (9): AsyncLocal, MutationExecutor, MutationPipelineScope, BackupService, CancellationToken, Func, IReadOnlyList, string (+1 more)

### Community 95 - "README.md"
Cohesion: 0.26
Nodes (3): Contratos usados pelo perfil, Matriz Fabric 1.21.1, Pacote local auditado

### Community 96 - "Cobblemon Low-End Lab v3.3.1"
Cohesion: 0.14
Nodes (14): Auditoria real do pacote local, Backup e rollback do perfil, Benchmark, CLI, Cobblemon Low-End Lab v3.3.1, EXTREME_4GB, Fluxos, Instancias reconhecidas (+6 more)

### Community 97 - "Homologacao operacional Cobblemon em 4 GB"
Cohesion: 0.14
Nodes (14): Adicionar ImmediatelyFast, Alternativa Modrinth App, Aplicar EXTREME_4GB, Benchmark operacional, Checklist do ZIP portatil, Comandos CLI equivalentes, Criar a instancia real no Prism Launcher, Decisao sobre Indium (+6 more)

### Community 98 - "MinecraftEasyModeService"
Cohesion: 0.23
Nodes (6): MinecraftContentProfileKind, MinecraftEasyModeService, Func, IReadOnlyCollection, IReadOnlyList, string

### Community 99 - "NetworkInterruptModerationTweakCommand"
Cohesion: 0.22
Nodes (6): NetworkInterruptModerationTweakCommand, BackupService, bool, List, RegistryKey, string

### Community 100 - "TextBox"
Cohesion: 0.17
Nodes (9): AverageFpsTextBox, JavaArgumentsTextBox, JoinSecondsTextBox, MenuSecondsTextBox, MinimumFpsTextBox, OperationalNotesTextBox, PathTextBox, TextChangedEventArgs (+1 more)

### Community 101 - ".AddSystemRestorePointIfCurrentRootMutation"
Cohesion: 0.17
Nodes (5): SystemRestoreService, int, IReadOnlyList, string, RegistryKey

### Community 102 - ".ReadPageFile"
Cohesion: 0.21
Nodes (7): AllocatedMb, DisplayDevice, InUseMb, PerformanceInformation, DllImport, MarshalAs, MemoryStatusEx

### Community 103 - "Program.cs"
Cohesion: 0.18
Nodes (5): ApexTweaker.UI.Wpf.Animations, ApexTweaker.UI.Wpf.Windows, ApexTweaker.UI.Wpf, ApplicationWarmup, bool

### Community 104 - "Minecraft geral e hooks de sessao - v3.3.1"
Cohesion: 0.17
Nodes (12): CLI, Desativado, Destino do teste, Extremo, Hooks de sessao, Limites reais, Minecraft geral e hooks de sessao - v3.3.1, O que nao foi implementado (+4 more)

### Community 105 - "HardwareInfo"
Cohesion: 0.21
Nodes (4): HardwareInfo, HardwareTier, PresetKind, PresetRecommendation

### Community 106 - "Execution log"
Cohesion: 0.18
Nodes (10): 2026-07-24 14:32 — Orquestracao FPS-P0-P1, 2026-07-24 14:38 � Orquestracao FE-ALL + FPS-BE, 2026-07-24 14:47 � BE PASS; FE finish relaunch, 2026-07-24 — discovery only (sem execução de agentes), 2026-07-24 — FPS P0/P1 orquestrado, 2026-07-24 — L2-SHELL-2 (parcial), 2026-07-24 — L2-SHELL despachado (Claude), 2026-07-24 — MARKET B1–B9 (modo A) (+2 more)

### Community 107 - "WindowsOptimizationService"
Cohesion: 0.29
Nodes (5): WindowsOptimizationApplicationFacade, IWindowsOptimizationInventory, GamingPerformanceProbe, WindowsOptimizationService, IReadOnlyList

### Community 108 - "WindowsOptimizationRule"
Cohesion: 0.42
Nodes (5): WindowsOptimizationRecommendationService, OptimizationDecisionKind, UsageAnswer, WindowsOptimizationDecision, WindowsOptimizationRule

### Community 109 - ".CaptureGamingPerformanceProbe"
Cohesion: 0.29
Nodes (4): FeatureState, RequestedFeatureState, ResizableBarProbe, IReadOnlyCollection

### Community 110 - "MinecraftEasyCorrectionPlan"
Cohesion: 0.18
Nodes (3): MinecraftEasyCorrectionPlan, MinecraftEasyInstanceStatus, MinecraftEasyServerReadiness

### Community 111 - "MinecraftInstanceService"
Cohesion: 0.25
Nodes (4): MinecraftInstanceService, IDictionary, IReadOnlyList, string

### Community 113 - "CreateEmergencyRestoreScript"
Cohesion: 0.40
Nodes (3): CreateEmergencyRestoreScript(), RegistryService, RegistryKey

### Community 114 - ".ProbeJava"
Cohesion: 0.33
Nodes (3): JavaRuntimeInfo, GeneratedRegex, Regex

### Community 115 - ".Capture"
Cohesion: 0.42
Nodes (4): MinecraftInstanceEvidenceService, IEnumerable, IReadOnlyDictionary, IReadOnlyList

### Community 118 - "ModulesView"
Cohesion: 0.27
Nodes (5): ModulesView, DependencyObject, IEnumerable, RoutedEventArgs, WpfButton

### Community 120 - "MemoryCompressionTweakCommand"
Cohesion: 0.36
Nodes (3): MemoryCompressionTweakCommand, BackupService, string

### Community 121 - "UserControl"
Cohesion: 0.31
Nodes (8): CoreButtonsPanel, GamesButtonsPanel, GpuButtonsPanel, MarketButtonsPanel, PeripheralButtonsPanel, UserControl, StackPanel, WrapPanel

### Community 122 - "PerformanceView"
Cohesion: 0.31
Nodes (4): PerformanceStatusItem, PerformanceView, RegistryKey, string

### Community 123 - "Primeiro teste seguro em 4 GB"
Cohesion: 0.25
Nodes (8): Criterio minimo, Evidencias e logs, Experimento real, Mods do primeiro baseline, Preparacao, Preset inicial obrigatorio, Primeiro teste seguro em 4 GB, Privilegio

### Community 124 - "ApexTweaker v3.3.1 - Minecraft Rapido"
Cohesion: 0.25
Nodes (7): ApexTweaker v3.3.1 - Minecraft Rapido, Assets locais, Novo fluxo, Objetivo, Seguranca preservada, Simplificacao visual, Validacao

### Community 125 - "MinecraftEnvironmentService.cs"
Cohesion: 0.32
Nodes (7): nuint, DisplayDevice, MemoryStatusEx, PerformanceInformation, string, uint, ulong

### Community 126 - "ApexTweaker"
Cohesion: 0.25
Nodes (8): ApexTweaker, Build local, Dados e seguranca, Distribuicao, Interface, Linha de comando, Minecraft One-Click Mode, Minecraft Scientific Optimization Engine

### Community 127 - "ProcessorIdleStatesTweakCommand"
Cohesion: 0.32
Nodes (3): ProcessorIdleStatesTweakCommand, BackupService, string

### Community 128 - ".ExecuteAsync"
Cohesion: 0.25
Nodes (6): MasterRollbackService, BackupService, CancellationToken, IProgress, IReadOnlyList, Task

### Community 129 - "WpfUserControl"
Cohesion: 0.25
Nodes (6): PresetCombo, CatalogView, RoutedEventArgs, SelectionChangedEventArgs, ComboBox, WpfUserControl

### Community 130 - "EasyFpsComboBox"
Cohesion: 0.29
Nodes (7): SelectedFps, SelectedHookMode, SelectedFps, SelectedHookMode, EasyFpsComboBox, EasyHookModeComboBox, ComboBox

### Community 131 - "Distribuicao"
Cohesion: 0.29
Nodes (7): Artefato portatil (oficial), Como gerar o portatil, Como o cliente deve executar, Distribuicao, Fluxo de release recomendado, Instalador (opcional), Observacoes

### Community 132 - "Inspirações FE — maturidade “nível corporação”"
Cohesion: 0.29
Nodes (6): Anti-padrões (grandes marcas que a comunidade odeia), Copy / tom, Inspirações FE — maturidade “nível corporação”, Mapa para ApexTweaker (estado atual → maduro), Próximo sprint FE sugerido, Referências boas (copiar padrões)

### Community 133 - "MinecraftEasyModSummary"
Cohesion: 0.33
Nodes (3): MinecraftEasyModSummary, ICollection, IReadOnlyList

### Community 134 - ".GetProcessIoCounters"
Cohesion: 0.40
Nodes (4): IoCounters, DllImport, MarshalAs, MemoryStatusEx

### Community 135 - "MinecraftBenchmarkService.cs"
Cohesion: 0.40
Nodes (5): BenchmarkEvidence, IoCounters, MemoryStatusEx, uint, ulong

### Community 137 - "ValorantLocator"
Cohesion: 0.40
Nodes (3): ValorantLocator, IEnumerable, string

### Community 138 - "TestResultPanel"
Cohesion: 0.40
Nodes (5): BoolToVisibility, IsTestPanelVisible, IsTestPanelVisible, TestResultPanel, Border

### Community 139 - "Master plan — sprint FE-ALL + FPS-BE"
Cohesion: 0.40
Nodes (4): Master plan — sprint FE-ALL + FPS-BE, Objetivo, Ordem de merge (orquestrador), Status live

### Community 140 - "Documentação ApexTweaker"
Cohesion: 0.40
Nodes (5): Arquitetura, Contratos e coordenação, Documentação ApexTweaker, Essencial, Minecraft / Cobblemon

### Community 142 - "TweakBackup"
Cohesion: 0.50
Nodes (4): BcdBackupEntry, TweakBackup, DateTime, IReadOnlyList

### Community 143 - "GameOpenedCheckBox"
Cohesion: 0.50
Nodes (4): GameOpened, GameOpened, GameOpenedCheckBox, CheckBox

### Community 149 - "DevMode"
Cohesion: 0.67
Nodes (3): short, DevMode, int

### Community 151 - "QuarantineList"
Cohesion: 0.67
Nodes (3): IssueList, QuarantineList, ListBox

## Knowledge Gaps
- **452 isolated node(s):** `net10.0-windows`, `CommunityToolkit.Mvvm (8.4.2)`, `LibreHardwareMonitorLib (0.9.6)`, `Microsoft.Diagnostics.Tracing.TraceEvent (3.1.21)`, `System.Management (10.0.2)` (+447 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **16 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainWindow` connect `MainWindow` to `.ExecuteAsync`, `MainWindow`, `TweakService`, `WpfUserControl`, `Window`, `.EnsureAdministratorForWindowsOperation`, `ValorantLocator`, `.Run`, `UtilitiesView`, `UserControl`, `MarketUtilitiesService`, `TweakService.cs`, `MinecraftModDescriptor`, `PageTransitionAnimator`, `TelemetryView`, `MinecraftAuditResult`, `MinecraftScientificExperimentService`, `MinecraftView`, `HardwareTelemetryService`, `MinecraftProfileService`, `TweakService`, `MinecraftBenchmarkService`, `SystemDiagnosticsService`, `MinecraftEnvironmentService`, `EtwFrameTracker`, `MinecraftInstanceDescriptor`, `MinecraftProfilePlan`, `DashboardView`, `OptimizationEngine`, `MinecraftEasyModeService`, `WindowsOptimizationService`, `MinecraftEasyCorrectionPlan`, `MinecraftInstanceService`, `.TryResolve`, `.RunTweakAsync`, `ModulesView`, `PerformanceView`?**
  _High betweenness centrality (0.399) - this node is a cross-community bridge._
- **Why does `TweakService` connect `TweakService` to `.Run`, `TweakService`, `ApexTweaker.Models`, `.AddSystemRestorePointIfCurrentRootMutation`, `ValorantLocator`, `WindowsOptimizationService`, `WindowsPowerModeService`, `.ExecuteCommand`, `OptimizationEngine`, `MainWindow`, `GpuOptimizationService`, `MutationExecutor`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **Why does `MinecraftView` connect `MinecraftView` to `WpfUserControl`, `TweakService.cs`, `TextBox`, `MinecraftEasyModSummary`, `UserControl`, `.SetCurrentStep`, `MinecraftAuditResult`, `.Run`, `MinecraftBenchmarkSample`, `MinecraftEasyCorrectionPlan`, `UserControl`, `RoutedEventArgs`, `MinecraftScientificModels.cs`, `MainWindow`, `MinecraftDiagnosticPackageResult`, `MinecraftWizardViewModel`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **Are the 42 inferred relationships involving `CobblemonEasyViewModel` (e.g. with `ApproximateFps` and `AuditReady`) actually correct?**
  _`CobblemonEasyViewModel` has 42 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0-windows`, `CommunityToolkit.Mvvm (8.4.2)`, `LibreHardwareMonitorLib (0.9.6)` to the rest of the system?**
  _452 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ApexTweaker.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.1354679802955665 - nodes in this community are weakly interconnected._
- **Should `CpuTopologyNative` be split into smaller, more focused modules?**
  _Cohesion score 0.05048076923076923 - nodes in this community are weakly interconnected._