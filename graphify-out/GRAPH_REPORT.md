# Graph Report - Apextweaker  (2026-07-30)

## Corpus Check
- 181 files · ~141,707 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 3045 nodes · 6093 edges · 189 communities (154 shown, 35 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 275 edges (avg confidence: 0.72)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `2b3bd519`
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
- .AddSystemRestorePointIfCurrentRootMutation
- .ReadPageFile
- Program.cs
- Minecraft geral e hooks de sessao - v3.3.1
- HardwareInfo
- Execution log
- .Main
- WindowsOptimizationRule
- IntelHybridProbeStrategy.cs
- MinecraftEasyCorrectionPlan
- MinecraftInstanceService
- .TryResolve
- CreateEmergencyRestoreScript
- .ProbeJava
- .Capture
- .StartMonitoringGame
- .RunTweakAsync
- ModulesView
- .RunAsync
- MemoryCompressionTweakCommand
- .EnsureAdministratorForWindowsOperation
- Integration status — P0 demo gate + feedback shell
- Primeiro teste seguro em 4 GB
- ApexTweaker v3.3.1 - Minecraft Rapido
- .RunBcdEditSetting
- ApexTweaker
- ProcessorIdleStatesTweakCommand
- .ExecuteAsync
- QuarantineList
- EasyFpsComboBox
- Distribuicao
- Inspirações FE — maturidade “nível corporação”
- Program
- TweakBackup
- PowerReadACValueIndex
- ValorantProcessOptimizer.cs
- SystemMutationCommand
- TestResultPanel
- Master plan — sprint FE-ALL + FPS-BE
- Documentação ApexTweaker
- .Run
- .SendMessageTimeout
- GameOpenedCheckBox
- Task P0.2.1 — resolução canônica + timeout/cancel em estágio
- .Run
- P0.2-BLOCKERS — routing
- Research note — referências do usuário vs FPS
- TelemetryPipeClient.cs
- MinecraftInstanceService
- MinecraftBenchmarkService.cs
- GroupRegistryEntries
- RegistryKey
- BackupService
- CancellationTokenSource
- DevMode
- int
- ApplicationWarmup
- IntPtr
- SystemRestoreService
- RegistryKey
- .ProbeJava
- AppInfo.cs
- SystemPowerStatus
- ValorantProcessOptimizer.cs
- .TryReadOperationalObservation
- .AddHistoryPoint
- .BuildRecommendations
- FE-DISTILL-MINIMAL — briefing Claude FE
- Action
- .GetSystemPowerStatus
- DESIGN — ApexTweaker (WPF)
- PowerReadACValueIndex
- RoutedEventArgs
- MinecraftDiagnosticPackageResult
- .OnDxgKrnlEvent
- .TryFindJsonProperty
- Task
- TextChangedEventArgs
- WpfButton
- Color
- FrameworkElement
- IReadOnlyDictionary
- .QuarantineList_OnSelectionChanged

## God Nodes (most connected - your core abstractions)
1. `MainWindow` - 138 edges
2. `TweakService` - 94 edges
3. `CobblemonEasyViewModel` - 89 edges
4. `UserControl` - 80 edges
5. `HardwareTelemetryService` - 78 edges
6. `MinecraftView` - 63 edges
7. `UserControl` - 58 edges
8. `MinecraftProfileService` - 54 edges
9. `ApexTweaker.Models` - 40 edges
10. `ApexTweaker.Services` - 37 edges

## Surprising Connections (you probably didn't know these)
- `UserControl` --references--> `FeedbackDetail`  [INFERRED]
  src/UI/Wpf/Views/CatalogView.xaml → src/UI/Wpf/ViewModels/CatalogViewModel.cs
- `UserControl` --references--> `FeedbackTitle`  [INFERRED]
  src/UI/Wpf/Views/CatalogView.xaml → src/UI/Wpf/ViewModels/CatalogViewModel.cs
- `UserControl` --references--> `SevereStutter`  [INFERRED]
  src/UI/Wpf/Views/CobblemonEasyView.xaml → src/UI/Wpf/ViewModels/CobblemonEasyViewModel.cs
- `MinecraftView` --references--> `ScientificExperimentPhase`  [EXTRACTED]
  src/UI/Wpf/Views/MinecraftView.xaml.cs → src/Minecraft/Models/MinecraftScientificModels.cs
- `BeginMutationSession()` --references--> `MutationSession`  [EXTRACTED]
  src/Services/BackupService.cs → src/Models/TweakMutationSession.cs

## Import Cycles
- None detected.

## Communities (189 total, 35 thin omitted)

### Community 0 - "MainWindow"
Cohesion: 0.19
Nodes (4): ApplicationOperation, MinecraftOperationalObservation, MinecraftProfileKind, Task

### Community 1 - "TweakService"
Cohesion: 0.24
Nodes (7): Border, DependencyProperty, DependencyPropertyChangedEventArgs, RiskBadge, RiskLevel, DependencyObject, TextBlock

### Community 2 - "ApexTweaker.Models"
Cohesion: 0.14
Nodes (11): ApexTweaker.Services, ApexTweaker.Application.Optimizations, ApexTweaker.Infrastructure, ApexTweaker.Models, ApexTweaker.Windows.Inventory, ApexTweaker.Core.Pipeline, UninstallTarget, AdapterMutationTarget (+3 more)

### Community 3 - "CpuTopologyNative"
Cohesion: 0.06
Nodes (28): FileMutation, JsonNode, JsonObject, ProfileOperation, MinecraftBackupFileEntry, MinecraftExperimentDefinition, MinecraftExperimentVariable, MinecraftProfileApplyResult (+20 more)

### Community 4 - "WindowsOptimizationModels.cs"
Cohesion: 0.36
Nodes (4): WindowsOptimizationSelfTest, IReadOnlyList, WindowsDeviceKind, WindowsPowerSource

### Community 5 - "Contexto completo do ApexTweaker"
Cohesion: 0.20
Nodes (11): DoubleAnimation, UiMotion, CancellationToken, DependencyObject, DependencyProperty, FrameworkElement, IEasingFunction, Storyboard (+3 more)

### Community 6 - "Window"
Cohesion: 0.08
Nodes (26): Subtitle, RoutedEventArgs, AppVersionText, CatalogButton, CloseButton, DashboardButton, HeaderSubtitleText, HeaderTitleText (+18 more)

### Community 7 - "UserControl"
Cohesion: 0.09
Nodes (26): BadgeLevel, Category, BoolToVis, FeedbackDetail, FeedbackTitle, Guidance, Impact, KindLabel (+18 more)

### Community 8 - "Funcionalidades do ApexTweaker"
Cohesion: 0.06
Nodes (32): Background, Backup e rollback, Catalogo, Cobblemon / Minecraft, Comandos estruturados adicionais, CPU/Scheduler, Criar restore point, Dashboard (+24 more)

### Community 9 - "WindowsOptimizationInventoryService"
Cohesion: 0.10
Nodes (13): ComputerInventory, DeviceGuardInventory, OperatingSystemInventory, ProcessorInventory, FeatureState, WindowsOptimizationInventoryService, DllImport, IReadOnlyCollection (+5 more)

### Community 10 - "Plano mestre de coordenação"
Cohesion: 0.20
Nodes (9): Backend / orquestração — `.agents/skills/`, Codex / agents (usuário), Como o orquestrador seleciona skills, Frontend — `.claude/skills/`, Política, Projeto ApexTweaker, Skills já instaladas na máquina (observadas), Skills planejadas (criar depois da aprovação) (+1 more)

### Community 12 - "UtilitiesView"
Cohesion: 0.06
Nodes (33): AutoOptimizeButton, RestorePointButton, SummaryText, UserControl, DashboardView, RoutedEventArgs, Button, TextBlock (+25 more)

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
Cohesion: 0.27
Nodes (7): AcceptCheckBox, ConfirmButton, Window, StartupDisclaimerWindow, RoutedEventArgs, Button, CheckBox

### Community 17 - "Arquitetura do backend"
Cohesion: 0.14
Nodes (13): ApexTweaker, ApexTweaker.Application, ApexTweaker.Contracts, ApexTweaker.Native, ApexTweaker.Windows, Arquitetura do backend, Compatibilidade, Estado atual (+5 more)

### Community 18 - "Auditoria estrutural do backend"
Cohesion: 0.14
Nodes (13): Acoplamento, Arquivos da primeira etapa, Auditoria estrutural do backend, Codigo grande e duplicacao, Contratos, Escopo e restricoes, Estrutura observada, Estrutura proposta (+5 more)

### Community 19 - "Frontend handoff — L2 (Otimização Windows / Presets Gamer)"
Cohesion: 0.29
Nodes (6): Como testar, Fora de escopo (ok), Frontend handoff — FE-DISTILL-MINIMAL, O que mudou, Resultado, Riscos

### Community 20 - "Estado atual da arquitetura — ApexTweaker"
Cohesion: 0.15
Nodes (12): Acoplamentos e riscos, Build / lint / testes (confirmados), Estado atual da arquitetura — ApexTweaker, Estado Git (no momento da auditoria / atualização), Fluxo UI → backend (in-process), Legado (em produção na UI), Novo (WIP Codex, ainda não ligado à UI), O que NÃO existe (confirmado ausente) (+4 more)

### Community 21 - "Backend handoff (template)"
Cohesion: 0.18
Nodes (10): Arquivos alterados, Backend handoff, Contratos / Models, Diff resumido, Escopo executado, Observado vs inferido, Pendencias, Propostas de contrato para fase futura (+2 more)

### Community 22 - "Matriz de propriedade (ownership)"
Cohesion: 0.25
Nodes (7): Agentes, Arquivos atualmente misturados (atenção), Matriz de propriedade (ownership), Matriz por caminho, Proibições absolutas, Resolução de conflito de ownership, Worktrees (modo automatizado)

### Community 23 - "Integration status"
Cohesion: 0.33
Nodes (5): Commits, Entregue nesta rodada, Integration status, Pendencias conhecidas, Verificacao

### Community 24 - "Market coverage matrix — ApexTweaker vs EXM / BoosterX"
Cohesion: 0.14
Nodes (10): ApexTweaker.UI.Wpf.Controls, ApexTweaker.UI.Wpf.Theming, ApexTweaker.UI.Wpf.Views, ApexTweaker.UI.Wpf.ViewModels, ApexTweaker.UI.Wpf.Testing, ApexTweaker.Minecraft, BenchmarkChartPoint, CommandPaletteItem (+2 more)

### Community 32 - "MarketUtilitiesService"
Cohesion: 0.18
Nodes (9): MinecraftQuarantineApplyResult, MinecraftQuarantineConfirmation, MinecraftQuarantineFileEntry, MinecraftQuarantineRollbackResult, MinecraftQuarantineService, IEnumerable, IReadOnlyList, JsonSerializerOptions (+1 more)

### Community 33 - "WindowsOptimizationInventoryService.cs"
Cohesion: 0.22
Nodes (8): ApexTweaker.Contracts.Inventory, ComputerInventory, DeviceGuardInventory, OperatingSystemInventory, ProcessorInventory, SystemPowerStatus, byte, uint

### Community 34 - "MainWindow.xaml.cs"
Cohesion: 0.04
Nodes (45): BackupService, bool, CancelEventArgs, CancellationTokenSource, CatalogView, DashboardView, Dictionary, EtwFrameTracker (+37 more)

### Community 35 - "TweakService.cs"
Cohesion: 0.06
Nodes (39): EventHandler, ProcessIds, ProcessPowerThrottlingState, FakeMinecraftSessionHookPlatform, IReadOnlyList, MinecraftSessionHookAction, MinecraftSessionHookMode, MinecraftSessionHookReport (+31 more)

### Community 36 - "MinecraftModDescriptor"
Cohesion: 0.19
Nodes (7): CommandClassifier, CommandIntent, TrustedCommandResolution, HashSet, IEnumerable, Regex, string

### Community 37 - "PageTransitionAnimator"
Cohesion: 0.29
Nodes (8): ContentControl, PageTransitionAnimator, CancellationToken, FrameworkElement, IEasingFunction, object, Task, TimeSpan

### Community 38 - "UserControl"
Cohesion: 0.04
Nodes (64): BackCommand, BenchmarkPoints, Color, ComparisonSummary, CurrentStep.Description, CurrentStep.Detail, CurrentStep.StateColor, CurrentStep.StateLabel (+56 more)

### Community 39 - "TelemetryView"
Cohesion: 0.05
Nodes (34): Foreground, Text, ConsoleLineViewModel, PointCollection, Queue, SizeChangedEventArgs, TelemetryMetricsSnapshot, BenchmarkButton (+26 more)

### Community 40 - "UserControl"
Cohesion: 0.04
Nodes (53): ApproximateFps, BenchmarkSummary, ClosedAlone, CorrectionDetails, DestinationQuestion, DetectStep.IsCurrent, DetectStep.StateLabel, FixStep.IsCurrent (+45 more)

### Community 41 - "AT_CPU_TOPOLOGY"
Cohesion: 0.08
Nodes (46): DWORD, KAFFINITY, AT_BuildPreferredGameAffinityMask(), CopyEntry(), AT_API, uint32_t, AddOrMergeEntry(), AT_GetCpuTopology() (+38 more)

### Community 42 - "MinecraftAuditResult"
Cohesion: 0.10
Nodes (17): BottleneckCandidate, MinecraftAuditResult, MinecraftOperationalChecklist, MinecraftQuarantinePlan, MinecraftSurvivalPlan, MinecraftBottleneckKind, ScientificDerivedMetrics, MinecraftBottleneckDiagnosticService (+9 more)

### Community 43 - "BackupService.cs"
Cohesion: 0.12
Nodes (29): IGrouping, RegistryBackupEntry, BeginMutationSession(), BuildUniqueMutationLedgerPath(), CaptureBcdEntries(), CaptureDisplayDriverEntries(), CaptureRegistryEntries(), CaptureRegistryValue() (+21 more)

### Community 44 - "MinecraftScientificExperimentService"
Cohesion: 0.13
Nodes (13): MinecraftScientificExperiment, MinecraftScientificOperationResult, ScientificHypothesis, MinecraftScientificExperimentService, DateTimeOffset, HashSet, IReadOnlyDictionary, IReadOnlyList (+5 more)

### Community 45 - "CobblemonEasyViewModel"
Cohesion: 0.05
Nodes (27): MinecraftEasyCorrectionPlan, MinecraftEasyState, CobblemonEasyViewModel, AuditReady, DuplicateMods, EssentialMods, HasBackup, HeavyMods (+19 more)

### Community 46 - "MinecraftView"
Cohesion: 0.08
Nodes (8): CustomExperimentCheckBox, PathTextBox, SaveHomologationButton, ScientificAdvanceButton, MinecraftView, bool, int, TextChangedEventArgs

### Community 47 - "LightweightBenchmarkChart"
Cohesion: 0.11
Nodes (9): KeyEventArgs, MouseButtonEventArgs, CommandPaletteInput, CommandPaletteList, CommandPaletteOverlay, Border, ListBox, TextBox (+1 more)

### Community 48 - "CobblemonEasyView"
Cohesion: 0.15
Nodes (5): EasyPrimaryAction, CobblemonEasyView, bool, RoutedEventArgs, Task

### Community 49 - "RoutedEventArgs"
Cohesion: 0.09
Nodes (17): AdvancedModeTabButton, ApplyProfileButton, ApplyQuarantineButton, AuditButton, BenchmarkButton, BrowseButton, CancelBenchmarkButton, EasyModeTabButton (+9 more)

### Community 50 - "MinecraftScientificModels.cs"
Cohesion: 0.12
Nodes (19): MinecraftBottleneckDiagnosis, MinecraftExperimentMeasurement, MinecraftScientificComparison, ModConfigAutomationStatus, ScientificActionKind, ScientificActionRisk, ScientificBenchmarkOutcome, ScientificConfidence (+11 more)

### Community 52 - "HardwareTelemetryService"
Cohesion: 0.13
Nodes (14): CatalogViewModel, FeedbackDetail, FeedbackKind, FeedbackTitle, SelectedPreset, StatusText, UsageUnknown, bool (+6 more)

### Community 53 - "ApexTweaker.Minecraft.Models"
Cohesion: 0.13
Nodes (6): ApexTweaker.Minecraft.Models, ApexTweaker.Minecraft.Services, BottleneckCandidate, Contract, FileMutation, ProfileOperation

### Community 54 - "ExtremeMutationCommands.cs"
Cohesion: 0.19
Nodes (10): FileInfo, JsonDocument, ModJarScanner, Dictionary, HashSet, JsonElement, List, long (+2 more)

### Community 55 - "MinecraftProfileService"
Cohesion: 0.29
Nodes (4): MarketCoverageSelfTest, MarketUtilitiesService, IEnumerable, IReadOnlyList

### Community 56 - "TweakService"
Cohesion: 0.27
Nodes (7): MinecraftLoader, MinecraftAuditService, HashSet, IEnumerable, IReadOnlyCollection, IReadOnlySet, List

### Community 57 - "MinecraftBenchmarkService"
Cohesion: 0.06
Nodes (33): AvailableMemoryGb, BenchmarkEvidence, CommitUsedMb, IoCounters, ReadBytes, MinecraftDiagnosticPackageContext, MinecraftInstanceDescriptor, MinecraftOperationalHomologationResult (+25 more)

### Community 58 - "SystemDiagnosticsService"
Cohesion: 0.13
Nodes (6): DevMode, SystemDiagnosticsService, DllImport, IReadOnlyList, RegistryKey, string

### Community 59 - "ValorantProcessOptimizer"
Cohesion: 0.15
Nodes (15): AffinityPlan, NativeProcessOptimizationResult, HardwareInfo, ValorantProcessOptimizer, Action, CancellationToken, DllImport, HashSet (+7 more)

### Community 60 - "MinecraftEnvironmentService"
Cohesion: 0.09
Nodes (18): AllocatedMb, AvailableGb, DisplayDevice, InUseMb, PerformanceInformation, DiskInfo, JavaMemoryRecommendation, JavaRuntimeInfo (+10 more)

### Community 61 - "GpuOptimizationService"
Cohesion: 0.20
Nodes (8): DisplayAdapterDevice, GpuInfo, GpuMutationPlan, GpuOptimizationService, IReadOnlyList, JsonElement, List, string

### Community 62 - "MinecraftWizardViewModel"
Cohesion: 0.11
Nodes (13): IRelayCommand, ObservableObject, MinecraftReportPaths, MinecraftVisualStateItem, MinecraftWizardStepState, MinecraftWizardStepViewModel, MinecraftWizardViewModel, bool (+5 more)

### Community 63 - "ResourceDictionary"
Cohesion: 0.08
Nodes (27): Color, DrawingContext, FrameworkElement, INotifyCollectionChanged, IReadOnlyDictionary, ISet, NotifyCollectionChangedEventArgs, LightweightBenchmarkChart (+19 more)

### Community 64 - ".Run"
Cohesion: 0.11
Nodes (12): CommandRunner, CancellationToken, int, Process, Task, TimeSpan, CommandResult, HypervisorTweakCommand (+4 more)

### Community 65 - "MinecraftScientificReportService"
Cohesion: 0.15
Nodes (11): Plan, Reports, MinecraftScientificOptimizationPlan, MinecraftScientificReportPaths, ScientificEvidenceType, MinecraftScientificReportService, IEnumerable, IReadOnlyDictionary (+3 more)

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
Cohesion: 0.15
Nodes (5): TweakService, BackupService, Func, IReadOnlyList, string

### Community 71 - "Pastas principais"
Cohesion: 0.10
Nodes (20): Dados em disco, Estrutura do projeto, `installer`, `native/ApexTweaker.Native`, Pastas geradas pelo .NET, Pastas principais, Pipeline de mutacao, `release-installer` (+12 more)

### Community 72 - ".SetCurrentStep"
Cohesion: 0.13
Nodes (4): EasyStepState, EasyStepViewModel, bool, IEnumerable

### Community 73 - "MutationSession"
Cohesion: 0.12
Nodes (18): BcdValueSnapshot, CommandStateSnapshot, MutationSession, PowerSchemeSnapshot, PowerSettingSnapshot, ProcessStateSnapshot, RegistryValueSnapshot, ServiceStateSnapshot (+10 more)

### Community 74 - "Minecraft Scientific Optimization Engine"
Cohesion: 0.11
Nodes (19): CLI completa, Contratos de configuracao de mods, Decisao de linguagem, Diagnostico de gargalo, Fato, inferencia e recomendacao, Fluxo GUI para uma instancia Prism real, Fontes tecnicas fixadas, Limitacoes restantes (+11 more)

### Community 75 - "KernelLatencyTracker"
Cohesion: 0.14
Nodes (9): DPCTraceData, ISRTraceData, KernelLatencyTracker, Action, CancellationTokenSource, long, object, string (+1 more)

### Community 76 - "AffinityIsolationCommand"
Cohesion: 0.24
Nodes (5): MinecraftSelfTest, Action, Color, IReadOnlyDictionary, MinecraftEnvironmentSnapshot

### Community 77 - ".BuildPlan"
Cohesion: 0.18
Nodes (10): MinecraftInstanceEvidence, ModConfigContractAssessment, MinecraftInstanceEvidenceService, IEnumerable, IReadOnlyDictionary, IReadOnlyList, MinecraftModConfigContractCatalog, IReadOnlyList (+2 more)

### Community 78 - "MinecraftAuditModels.cs"
Cohesion: 0.15
Nodes (15): AuditSeverity, BenchmarkStatus, JavaMemoryTier, MinecraftAuditIssue, MinecraftAuditSummary, MinecraftBackupManifest, MinecraftHomologationCriterion, MinecraftLauncherKind (+7 more)

### Community 79 - "MinecraftInstanceDescriptor"
Cohesion: 0.19
Nodes (5): SendMessageTimeoutFlags, DllImport, IntPtr, RegistryKey, SystemPowerStatus

### Community 80 - "WindowsPowerModeService"
Cohesion: 0.28
Nodes (4): WindowsPowerModeService, DllImport, Guid, string

### Community 81 - "EdgeRemovalTweakCommand"
Cohesion: 0.18
Nodes (7): Arguments, FileName, EdgeRemovalTweakCommand, BackupService, bool, string, UninstallTarget

### Community 82 - "FabricVersionConstraint"
Cohesion: 0.24
Nodes (5): MinecraftModDescriptor, Dictionary, HashSet, IDictionary, IReadOnlyList

### Community 83 - ".ExecuteCommand"
Cohesion: 0.15
Nodes (11): L3CacheDescriptor, ProcessorIsolationTopology, AffinityIsolationCommand, ProcessorIsolationTopology, Dictionary, DllImport, HashSet, IntPtr (+3 more)

### Community 84 - "WindowsOptimizationModels.cs"
Cohesion: 0.19
Nodes (15): WindowsOptimizationCatalog, IReadOnlyList, IReadOnlySet, AdmxPolicyReference, EvidenceLevel, OptimizationDecisionKind, OptimizationRequirement, PerformanceEvidence (+7 more)

### Community 85 - "MinecraftCommandLine"
Cohesion: 0.27
Nodes (4): RequestedFeatureState, ResizableBarProbe, PerformanceStatusItem, PerformanceView

### Community 86 - "CpuTopologyProfile"
Cohesion: 0.24
Nodes (7): RootCard, Window, LoadingWindow, int, RoutedEventArgs, Border, Window

### Community 87 - ".BuildKeyValueMutation"
Cohesion: 0.14
Nodes (12): AsyncLocal, OperationOutcomeKind, MutationExecutor, MutationPipelineScope, BackupService, CancellationToken, DateTimeOffset, Func (+4 more)

### Community 88 - ".MonitorLoopAsync"
Cohesion: 0.17
Nodes (6): GameProcessInfo, WindowNativeMethods, CancellationToken, DllImport, IReadOnlyList, Process

### Community 89 - "MinecraftProfilePlan"
Cohesion: 0.25
Nodes (3): ValorantLocator, IEnumerable, string

### Community 90 - "DashboardView"
Cohesion: 0.11
Nodes (17): Description, StatusGlyph, StatusKey, StatusLabel, CompetitiveModeButton, DisableVbsButton, OptimizeFullscreenButton, ProbeLoadingBanner (+9 more)

### Community 91 - "TelemetryPipeClient"
Cohesion: 0.15
Nodes (12): IDisposable, NamedPipeClientStream, TelemetryPipeClient, bool, CancellationToken, CancellationTokenSource, int, JsonSerializerOptions (+4 more)

### Community 92 - "ISystemMutationCommand"
Cohesion: 0.22
Nodes (6): DemoSafetySelfTest, Func, IReadOnlyList, List, Task, OperationOutcome

### Community 93 - "OptimizationEngine"
Cohesion: 0.19
Nodes (8): CpuArchitectureProfile, OptimizationEngine, OptimizationFreshness, ProcessorBoostDecision, Action, double, int, string

### Community 94 - "MutationExecutor"
Cohesion: 0.21
Nodes (7): ApexTweaker.UI.Wpf.Animations, Snackbar, SnackbarKind, DispatcherTimer, int, Storyboard, TextBlock

### Community 95 - "README.md"
Cohesion: 0.13
Nodes (13): Categorias cobertas (menu EXM Free), Decisões, Explicitamente fora, Mapa de lotes, Market coverage matrix — ApexTweaker vs EXM / BoosterX, WinUtil (CTT), Advanced CAUTION, Customize Preferences (+5 more)

### Community 96 - "Cobblemon Low-End Lab v3.3.1"
Cohesion: 0.14
Nodes (14): Auditoria real do pacote local, Backup e rollback do perfil, Benchmark, CLI, Cobblemon Low-End Lab v3.3.1, EXTREME_4GB, Fluxos, Instancias reconhecidas (+6 more)

### Community 97 - "Homologacao operacional Cobblemon em 4 GB"
Cohesion: 0.14
Nodes (14): Adicionar ImmediatelyFast, Alternativa Modrinth App, Aplicar EXTREME_4GB, Benchmark operacional, Checklist do ZIP portatil, Comandos CLI equivalentes, Criar a instancia real no Prism Launcher, Decisao sobre Indium (+6 more)

### Community 98 - "MinecraftEasyModeService"
Cohesion: 0.16
Nodes (8): MinecraftContentProfileKind, MinecraftEasyInstanceStatus, MinecraftEasyServerReadiness, MinecraftEasyModeService, Func, IReadOnlyCollection, IReadOnlyList, string

### Community 99 - "NetworkInterruptModerationTweakCommand"
Cohesion: 0.22
Nodes (6): NetworkInterruptModerationTweakCommand, BackupService, bool, List, RegistryKey, string

### Community 101 - ".AddSystemRestorePointIfCurrentRootMutation"
Cohesion: 0.18
Nodes (9): CpuTelemetryKind, CpuTopologyProfile, ProcessorCoreDescriptor, CpuTopologyProfile, HashSet, IntPtr, List, LOGICAL_PROCESSOR_RELATIONSHIP (+1 more)

### Community 102 - ".ReadPageFile"
Cohesion: 0.13
Nodes (14): 1. Outcome isolado por operação, 2. Cancelamento, 3. Ledger e outcome final, 4. CommandClassifier adversarial, 5. Snackbar global, Acceptance criteria, Correções adicionais, Decisão de merge (+6 more)

### Community 103 - "Program.cs"
Cohesion: 0.38
Nodes (5): ModRecommendation, MinecraftModCatalog, HashSet, IReadOnlyCollection, IReadOnlyList

### Community 104 - "Minecraft geral e hooks de sessao - v3.3.1"
Cohesion: 0.15
Nodes (12): CLI, Desativado, Destino do teste, Extremo, Hooks de sessao, Limites reais, Minecraft geral e hooks de sessao - v3.3.1, O que nao foi implementado (+4 more)

### Community 105 - "HardwareInfo"
Cohesion: 0.30
Nodes (6): IComparable, FabricVersionConstraint, VersionNumber, GeneratedRegex, Regex, VersionNumber

### Community 106 - "Execution log"
Cohesion: 0.15
Nodes (12): 2026-07-24 14:32 — Orquestracao FPS-P0-P1, 2026-07-24 14:38 � Orquestracao FE-ALL + FPS-BE, 2026-07-24 14:47 � BE PASS; FE finish relaunch, 2026-07-24 14:57 � Integracao no main, 2026-07-24 — discovery only (sem execução de agentes), 2026-07-24 — FPS P0/P1 orquestrado, 2026-07-24 — L2-SHELL-2 (parcial), 2026-07-24 — L2-SHELL despachado (Claude) (+4 more)

### Community 107 - ".Main"
Cohesion: 0.18
Nodes (3): HardwareTier, PresetKind, PresetRecommendation

### Community 108 - "WindowsOptimizationRule"
Cohesion: 0.42
Nodes (5): WindowsOptimizationRecommendationService, WindowsOptimizationContext, WindowsOptimizationDecision, WindowsOptimizationRule, CatalogRowViewModel

### Community 109 - "IntelHybridProbeStrategy.cs"
Cohesion: 0.36
Nodes (4): RuntimeMode, RuntimeModeContext, RuntimeMutationDecision, object

### Community 110 - "MinecraftEasyCorrectionPlan"
Cohesion: 0.22
Nodes (8): Contexto, Entrega, FE-SHELL-FLUENCY-P1 — prompt Claude (poder total), Modelo / esforço, Método, Objetivo, Permitido, Proibido

### Community 111 - "MinecraftInstanceService"
Cohesion: 0.40
Nodes (4): Kind, Message, OperationOutcome, SnackbarKind

### Community 112 - ".TryResolve"
Cohesion: 0.22
Nodes (9): AnalyzeButton, GoToAutoButton, RetryEmptyButton, RetryErrorButton, CatalogView, bool, RoutedEventArgs, Task (+1 more)

### Community 113 - "CreateEmergencyRestoreScript"
Cohesion: 0.33
Nodes (3): MinecraftEasyModSummary, ICollection, IReadOnlyList

### Community 114 - ".ProbeJava"
Cohesion: 0.25
Nodes (7): Contexto, FE-SHELL-POLISH-P1 — prompt Claude, Método, Objetivo, Permitido, Proibido, Resultado esperado

### Community 115 - ".Capture"
Cohesion: 0.12
Nodes (9): Computer, float, KernelLatencyTracker, BenchmarkState, HardwareTelemetryService, double, JsonSerializerOptions, Task (+1 more)

### Community 119 - ".RunAsync"
Cohesion: 0.25
Nodes (7): Contexto, Entrega, P0.2-BLOCKERS — prompt de execução, Regras, Testes mínimos a adicionar/garantir, Trabalho, Verificação

### Community 120 - "MemoryCompressionTweakCommand"
Cohesion: 0.23
Nodes (4): ResolvedGpuInterruptTarget, MpoTweakCommand, MsiModeTweakCommand, string

### Community 121 - ".EnsureAdministratorForWindowsOperation"
Cohesion: 0.05
Nodes (37): CoreGroupMapping, ApexTweaker.NativeInterop, HardwareEnvironmentDetectionResult, HardwareEnvironmentDetector, HashSet, CoreGroupMapping, IntelHybridProbeStrategy, LOGICAL_PROCESSOR_RELATIONSHIP (+29 more)

### Community 122 - "Integration status — P0 demo gate + feedback shell"
Cohesion: 0.50
Nodes (3): Dívidas não bloqueantes (próximo lote), Integration status — P0 demo gate + feedback shell, Verificação pós-merge em `main`

### Community 123 - "Primeiro teste seguro em 4 GB"
Cohesion: 0.22
Nodes (8): Criterio minimo, Evidencias e logs, Experimento real, Mods do primeiro baseline, Preparacao, Preset inicial obrigatorio, Primeiro teste seguro em 4 GB, Privilegio

### Community 124 - "ApexTweaker v3.3.1 - Minecraft Rapido"
Cohesion: 0.25
Nodes (7): ApexTweaker v3.3.1 - Minecraft Rapido, Assets locais, Novo fluxo, Objetivo, Seguranca preservada, Simplificacao visual, Validacao

### Community 126 - "ApexTweaker"
Cohesion: 0.12
Nodes (13): Contratos usados pelo perfil, Matriz Fabric 1.21.1, Pacote local auditado, Próximo lote (dívidas não bloqueantes), Arquitetura e produto, Build local, CLI útil, Dados locais (+5 more)

### Community 127 - "ProcessorIdleStatesTweakCommand"
Cohesion: 0.33
Nodes (5): Contract, Goal, Kind map, UI-OUTCOME-P1, Verify

### Community 128 - ".ExecuteAsync"
Cohesion: 0.25
Nodes (6): MasterRollbackService, BackupService, CancellationToken, IProgress, IReadOnlyList, Task

### Community 129 - "QuarantineList"
Cohesion: 0.13
Nodes (16): GROUP_AFFINITY, LOGICAL_PROCESSOR_RELATIONSHIP, SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER, int, CACHE_RELATIONSHIP, GROUP_AFFINITY, L3CacheDescriptor, LOGICAL_PROCESSOR_RELATIONSHIP (+8 more)

### Community 130 - "EasyFpsComboBox"
Cohesion: 0.22
Nodes (8): SelectedFps, SelectedHookMode, SelectedFps, SelectedHookMode, EasyFpsComboBox, EasyHookModeComboBox, SelectionChangedEventArgs, ComboBox

### Community 131 - "Distribuicao"
Cohesion: 0.25
Nodes (7): Artefato portatil (oficial), Como gerar o portatil, Como o cliente deve executar, Distribuicao, Fluxo de release recomendado, Instalador (opcional), Observacoes

### Community 132 - "Inspirações FE — maturidade “nível corporação”"
Cohesion: 0.29
Nodes (6): Anti-padrões (grandes marcas que a comunidade odeia), Copy / tom, Inspirações FE — maturidade “nível corporação”, Mapa para ApexTweaker (estado atual → maduro), Próximo sprint FE sugerido, Referências boas (copiar padrões)

### Community 133 - "Program"
Cohesion: 0.19
Nodes (8): MinecraftBenchmarkResult, MinecraftOperationalObservation, MinecraftPlayTargetKind, ScientificEvidence, MinecraftScientificMetricsService, ICollection, IEnumerable, string

### Community 134 - "TweakBackup"
Cohesion: 0.50
Nodes (4): BcdBackupEntry, TweakBackup, DateTime, IReadOnlyList

### Community 135 - "PowerReadACValueIndex"
Cohesion: 0.18
Nodes (8): WindowsOptimizationApplicationFacade, IWindowsOptimizationInventory, GamingPerformanceProbe, WindowsOptimizationPlan, WindowsUsageProfile, IReadOnlyList, WindowsOptimizationService, IReadOnlyList

### Community 136 - "ValorantProcessOptimizer.cs"
Cohesion: 0.22
Nodes (3): RegistryKey, RegistryService, RegistryKey

### Community 137 - "SystemMutationCommand"
Cohesion: 0.20
Nodes (4): ISystemMutationCommand, SystemMutationCommand, Action, BackupService

### Community 138 - "TestResultPanel"
Cohesion: 0.40
Nodes (5): BoolToVisibility, IsTestPanelVisible, IsTestPanelVisible, TestResultPanel, Border

### Community 140 - "Documentação ApexTweaker"
Cohesion: 0.40
Nodes (5): Arquitetura, Contratos e coordenação, Documentação ApexTweaker, Essencial, Minecraft / Cobblemon

### Community 142 - ".SendMessageTimeout"
Cohesion: 0.32
Nodes (3): ProcessorIdleStatesTweakCommand, BackupService, string

### Community 143 - "GameOpenedCheckBox"
Cohesion: 0.50
Nodes (4): GameOpened, GameOpened, GameOpenedCheckBox, CheckBox

### Community 144 - "Task P0.2.1 — resolução canônica + timeout/cancel em estágio"
Cohesion: 0.33
Nodes (5): Acceptance criteria, Bloqueadores desta rodada, Fora de escopo, Task P0.2.1 — resolução canônica + timeout/cancel em estágio, Verificação

### Community 146 - "P0.2-BLOCKERS — routing"
Cohesion: 0.50
Nodes (3): Gate antes de pedir re-revisão, Ownership, P0.2-BLOCKERS — routing

### Community 150 - "MinecraftBenchmarkService.cs"
Cohesion: 0.32
Nodes (7): nuint, DisplayDevice, MemoryStatusEx, PerformanceInformation, string, uint, ulong

### Community 157 - "RegistryKey"
Cohesion: 0.32
Nodes (3): OptimizationFreshness, OptimizationStateAudit, RegistryKey

### Community 159 - "CancellationTokenSource"
Cohesion: 0.40
Nodes (5): BenchmarkEvidence, IoCounters, MemoryStatusEx, uint, ulong

### Community 162 - "ApplicationWarmup"
Cohesion: 0.18
Nodes (6): ApexTweaker, AppInfo, string, ApplicationPaths, IReadOnlyList, AuditRow

### Community 164 - "SystemRestoreService"
Cohesion: 0.29
Nodes (4): SystemRestoreService, int, IReadOnlyList, string

### Community 167 - "AppInfo.cs"
Cohesion: 0.20
Nodes (5): ApexTweaker.UI.Wpf.Windows, ApexTweaker.UI.Wpf, GamingFpsProbeSelfTest, Program, STAThread

### Community 168 - "SystemPowerStatus"
Cohesion: 0.67
Nodes (3): SystemPowerStatus, byte, int

### Community 171 - ".AddHistoryPoint"
Cohesion: 0.67
Nodes (3): BiosChecklistCatalog, BiosChecklistItem, IReadOnlyList

### Community 173 - "FE-DISTILL-MINIMAL — briefing Claude FE"
Cohesion: 0.33
Nodes (5): Direção, Entregável, FE-DISTILL-MINIMAL — briefing Claude FE, Lote deste PR, Verify

### Community 174 - "Action"
Cohesion: 0.17
Nodes (8): Category, MutationPipelineScope, RequiresRollback, OperationStepResult, OperationStepStatus, Action, Exception, Status

### Community 176 - "DESIGN — ApexTweaker (WPF)"
Cohesion: 0.40
Nodes (4): DESIGN — ApexTweaker (WPF), Out of scope here, Rules, Tokens

### Community 177 - "PowerReadACValueIndex"
Cohesion: 0.60
Nodes (5): DllImport, Guid, IntPtr, PowerReadACValueIndex(), PowerReadDCValueIndex()

### Community 178 - "RoutedEventArgs"
Cohesion: 0.50
Nodes (3): PresetCombo, SelectionChangedEventArgs, ComboBox

### Community 180 - ".OnDxgKrnlEvent"
Cohesion: 0.67
Nodes (3): short, DevMode, int

### Community 191 - ".QuarantineList_OnSelectionChanged"
Cohesion: 0.40
Nodes (4): IssueList, QuarantineList, SelectionChangedEventArgs, ListBox

## Knowledge Gaps
- **505 isolated node(s):** `CommandPaletteItem`, `Grid`, `net10.0-windows`, `CommunityToolkit.Mvvm (8.4.2)`, `LibreHardwareMonitorLib (0.9.6)` (+500 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **35 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ApexTweaker.Services` connect `ApexTweaker.Models` to `.ExecuteAsync`, `QuarantineList`, `ValorantProcessOptimizer.cs`, `TelemetryPipeClient.cs`, `Market coverage matrix — ApexTweaker vs EXM / BoosterX`, `WindowsOptimizationInventoryService.cs`, `ApplicationWarmup`, `TweakService.cs`, `SystemRestoreService`, `AppInfo.cs`, `TelemetryView`, `ValorantProcessOptimizer.cs`, `BackupService.cs`, `MinecraftProfileService`, `HardwareTelemetryService.cs`, `TelemetryPipeServer`, `EtwFrameTracker`, `WindowsPowerModeService`, `MinecraftProfilePlan`, `OptimizationEngine`, `.EnsureAdministratorForWindowsOperation`, `.RunBcdEditSetting`?**
  _High betweenness centrality (0.195) - this node is a cross-community bridge._
- **Why does `ApexTweaker.Minecraft.Models` connect `ApexTweaker.Minecraft.Models` to `TweakService.cs`, `.SetCurrentStep`, `MinecraftAuditModels.cs`, `MinecraftScientificModels.cs`, `MinecraftBenchmarkService.cs`, `Market coverage matrix — ApexTweaker vs EXM / BoosterX`, `MinecraftWizardViewModel`, `CancellationTokenSource`?**
  _High betweenness centrality (0.098) - this node is a cross-community bridge._
- **Why does `MinecraftView` connect `MinecraftView` to `MinecraftEasyModeService`, `TweakService.cs`, `Program`, `UserControl`, `.TryReadOperationalObservation`, `Master plan — sprint FE-ALL + FPS-BE`, `.BuildRecommendations`, `CobblemonEasyViewModel`, `UtilitiesView`, `RoutedEventArgs`, `MinecraftScientificModels.cs`, `CreateEmergencyRestoreScript`, `Market coverage matrix — ApexTweaker vs EXM / BoosterX`, `MinecraftWizardViewModel`, `.QuarantineList_OnSelectionChanged`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **Are the 42 inferred relationships involving `CobblemonEasyViewModel` (e.g. with `ApproximateFps` and `AuditReady`) actually correct?**
  _`CobblemonEasyViewModel` has 42 INFERRED edges - model-reasoned connections that need verification._
- **What connects `CommandPaletteItem`, `Grid`, `net10.0-windows` to the rest of the system?**
  _505 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ApexTweaker.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.14039408866995073 - nodes in this community are weakly interconnected._
- **Should `CpuTopologyNative` be split into smaller, more focused modules?**
  _Cohesion score 0.06385964912280702 - nodes in this community are weakly interconnected._