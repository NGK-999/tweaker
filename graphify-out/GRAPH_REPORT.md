# Graph Report - Apextweaker-codex  (2026-07-25)

## Corpus Check
- 182 files · ~136,965 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2932 nodes · 5985 edges · 178 communities (147 shown, 31 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 334 edges (avg confidence: 0.74)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `0a1c68fe`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Win32MinecraftSessionHookPlatform
- MinecraftProfileService
- MinecraftBenchmarkService
- CpuTopologyNative
- PageTransitionAnimator
- UserControl
- TelemetryView
- UserControl
- AT_CPU_TOPOLOGY
- CobblemonEasyViewModel
- MinecraftAuditResult
- MinecraftEnvironmentService
- BackupService.cs
- Window
- TweakService
- MinecraftView
- WindowsOptimizationInventoryService
- Funcionalidades do ApexTweaker
- CobblemonEasyView
- RoutedEventArgs
- .WriteLine
- MinecraftScientificModels.cs
- MinecraftScientificExperimentService
- HardwareTelemetryService
- ApexTweaker.Models
- MinecraftScientificReportService
- ApexTweaker.Minecraft.Models
- MinecraftWizardViewModel
- SystemDiagnosticsService
- Plano mestre de coordenação
- ExtremeMutationCommands.cs
- MinecraftCommandLine
- ValorantProcessOptimizer
- .Run
- ApexTweaker
- ModJarScanner
- .BuildPlan
- AppThemeManager
- UtilitiesView
- HardwareTelemetryService.cs
- TelemetrySnapshot
- MutationSession
- .TrySetDword
- TelemetryPipeServer
- GpuOptimizationService
- WindowsOptimizationModels.cs
- CommandRunner
- EtwFrameTracker
- Pastas principais
- MinecraftAuditModels.cs
- .SetCurrentStep
- MainWindow
- Minecraft Scientific Optimization Engine
- KernelLatencyTracker
- AffinityIsolationCommand
- MinecraftEasyModeService
- WindowsPowerModeService
- EdgeRemovalTweakCommand
- UserControl
- FabricVersionConstraint
- WindowsOptimizationService
- MinecraftModDescriptor
- CpuTopologyProfile
- LightweightBenchmarkChart
- Contrato FE ↔ BE (in-process)
- .MonitorLoopAsync
- .TryReadDword
- .Audit
- OptimizationEngine
- DashboardView
- Arquitetura-alvo — ApexTweaker
- TelemetryPipeClient
- MinecraftBenchmarkResult
- HardwareInfo
- Arquitetura do backend
- Auditoria estrutural do backend
- README.md
- Cobblemon Low-End Lab v3.3.1
- Frontend handoff — L2 (Otimização Windows / Presets Gamer)
- Homologacao operacional Cobblemon em 4 GB
- .TryResolve
- .Apply
- ISystemMutationCommand
- UserControl
- MutationExecutor
- Backend handoff (template)
- WindowsOptimizationRule
- TextBox
- MainWindow.xaml.cs
- Estado atual da arquitetura — ApexTweaker
- Minecraft geral e hooks de sessao - v3.3.1
- CatalogViewModel
- .Run
- MarketUtilitiesService
- NetworkInterruptModerationTweakCommand
- MinecraftScientificExperimentStore
- WindowsOptimizationInventoryService.cs
- MinecraftSelfTest.cs
- MemoryCompressionTweakCommand
- Matriz de propriedade (ownership)
- Primeiro teste seguro em 4 GB
- ApexTweaker v3.3.1 - Minecraft Rapido
- ApexTweaker
- .Run
- ProcessorIdleStatesTweakCommand
- .ExecuteAsync
- CatalogView
- EasyFpsComboBox
- Backend Task — FPS P0/P1 (modo A)
- Distribuicao
- MinecraftEasyModSummary
- SystemRestoreService
- .RunTweakAsync
- Integration status
- Market coverage matrix — ApexTweaker vs EXM / BoosterX
- .EnsureAdministratorForWindowsOperation
- MinecraftBenchmarkService.cs
- StartupDisclaimerWindow
- ValorantLocator
- TestResultPanel
- Documentação ApexTweaker
- MinecraftBenchmarkSample
- MinecraftEasyCorrectionPlan
- GameOpenedCheckBox
- Frontend Task — Catalogo B9 (modo A)
- PageTransitionAnimator
- PerformanceView
- AGENTS.md
- TelemetryPipeClient.cs
- Research note — referências do usuário vs FPS
- DevMode
- Pirâmide alvo
- QuarantineList
- README.md
- MsiModeTweakCommand
- TextBox
- WindowsOptimizationInventoryService.cs
- Estratégia de recuperação — ApexTweaker
- SystemRestoreService
- Jornadas de usuário — ApexTweaker
- Prompt Codex — BE-DEMO-OUTCOME-P0
- Visão de produto — ApexTweaker
- Proposta de sistema visual — ApexTweaker
- ApexTweaker.UI.Wpf.Animations
- Taxonomia de erros — ApexTweaker
- MinecraftEnvironmentService.cs
- Prompt Claude Code — FE-FEEDBACK-SHELL-P0
- .EnsureAdministratorForWindowsOperation
- MinecraftBenchmarkService.cs
- Quality gates — ApexTweaker
- PowerReadACValueIndex
- Acessibilidade — ApexTweaker
- ValorantProcessOptimizer.cs
- Error messages — ApexTweaker (UX)
- ApplicationPaths.cs
- AppInfo.cs
- PresetKind
- BE-DEMO-OUTCOME-P0-routing.md
- FE-FEEDBACK-SHELL-P0-routing.md
- design-principles.md
- interaction-states.md
- int
- Process
- TimeSpan
- BackupService
- DateTimeOffset
- string

## God Nodes (most connected - your core abstractions)
1. `MainWindow` - 136 edges
2. `CobblemonEasyViewModel` - 89 edges
3. `TweakService` - 88 edges
4. `UserControl` - 80 edges
5. `HardwareTelemetryService` - 79 edges
6. `MinecraftView` - 64 edges
7. `UserControl` - 58 edges
8. `MinecraftProfileService` - 55 edges
9. `ApexTweaker.Models` - 38 edges
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

## Communities (178 total, 31 thin omitted)

### Community 0 - "Win32MinecraftSessionHookPlatform"
Cohesion: 0.06
Nodes (39): EventHandler, ProcessIds, ProcessPowerThrottlingState, FakeMinecraftSessionHookPlatform, IReadOnlyList, MinecraftSessionHookAction, MinecraftSessionHookMode, MinecraftSessionHookReport (+31 more)

### Community 1 - "MinecraftProfileService"
Cohesion: 0.09
Nodes (20): FileMutation, JsonNode, JsonObject, MinecraftBackupFileEntry, MinecraftProfileChangeKind, MinecraftProfileDefinition, MinecraftProfileKind, MinecraftProfileSettingChange (+12 more)

### Community 2 - "MinecraftBenchmarkService"
Cohesion: 0.05
Nodes (33): AvailableMemoryGb, BenchmarkEvidence, CommitUsedMb, IoCounters, ReadBytes, MinecraftDiagnosticPackageContext, MinecraftDiagnosticPackageResult, MinecraftInstanceDescriptor (+25 more)

### Community 3 - "CpuTopologyNative"
Cohesion: 0.05
Nodes (37): CoreGroupMapping, ApexTweaker.NativeInterop, HardwareEnvironmentDetectionResult, HardwareEnvironmentDetector, HashSet, CoreGroupMapping, IntelHybridProbeStrategy, LOGICAL_PROCESSOR_RELATIONSHIP (+29 more)

### Community 4 - "PageTransitionAnimator"
Cohesion: 0.20
Nodes (11): UiMotion, CancellationToken, DependencyObject, DependencyProperty, DoubleAnimation, FrameworkElement, IEasingFunction, Storyboard (+3 more)

### Community 5 - "UserControl"
Cohesion: 0.05
Nodes (57): BackCommand, BenchmarkPoints, Color, ComparisonSummary, CurrentStep.Description, CurrentStep.Detail, CurrentStep.StateColor, CurrentStep.StateLabel (+49 more)

### Community 6 - "TelemetryView"
Cohesion: 0.05
Nodes (34): Foreground, Text, ConsoleLineViewModel, PointCollection, Queue, SizeChangedEventArgs, TelemetryMetricsSnapshot, BenchmarkButton (+26 more)

### Community 7 - "UserControl"
Cohesion: 0.04
Nodes (48): ApproximateFps, BenchmarkSummary, ClosedAlone, CorrectionDetails, DestinationQuestion, DetectStep.IsCurrent, DetectStep.StateLabel, FixStep.IsCurrent (+40 more)

### Community 8 - "AT_CPU_TOPOLOGY"
Cohesion: 0.08
Nodes (46): DWORD, KAFFINITY, AT_BuildPreferredGameAffinityMask(), CopyEntry(), AT_API, uint32_t, AddOrMergeEntry(), AT_GetCpuTopology() (+38 more)

### Community 9 - "CobblemonEasyViewModel"
Cohesion: 0.05
Nodes (28): MinecraftEasyState, CobblemonEasyViewModel, AuditReady, CorrectionDetails, DuplicateMods, EssentialMods, HasBackup, HeavyMods (+20 more)

### Community 10 - "MinecraftAuditResult"
Cohesion: 0.12
Nodes (11): MinecraftOperationalChecklist, MinecraftQuarantinePlan, MinecraftReportPaths, MinecraftSurvivalPlan, MinecraftReportService, ICollection, IEnumerable, JsonSerializerOptions (+3 more)

### Community 11 - "MinecraftEnvironmentService"
Cohesion: 0.09
Nodes (16): AllocatedMb, AvailableGb, DisplayDevice, InUseMb, PerformanceInformation, JavaMemoryRecommendation, JavaRuntimeInfo, MinecraftEnvironmentService (+8 more)

### Community 12 - "BackupService.cs"
Cohesion: 0.09
Nodes (37): IGrouping, RegistryBackupEntry, BeginMutationSession(), BuildUniqueMutationLedgerPath(), CaptureActivePowerScheme(), CaptureBcdEntries(), CaptureCommandState(), CaptureDisplayDriverEntries() (+29 more)

### Community 13 - "Window"
Cohesion: 0.08
Nodes (26): Subtitle, AppVersionText, CatalogButton, CloseButton, DashboardButton, HeaderSubtitleText, HeaderTitleText, MaximizeButton (+18 more)

### Community 15 - "MinecraftView"
Cohesion: 0.08
Nodes (7): CustomExperimentCheckBox, SaveHomologationButton, ScientificAdvanceButton, MinecraftView, bool, int, SelectionChangedEventArgs

### Community 16 - "WindowsOptimizationInventoryService"
Cohesion: 0.11
Nodes (12): ComputerInventory, DeviceGuardInventory, OperatingSystemInventory, ProcessorInventory, FeatureState, RequestedFeatureState, ResizableBarProbe, WindowsOptimizationInventoryService (+4 more)

### Community 17 - "Funcionalidades do ApexTweaker"
Cohesion: 0.06
Nodes (32): Background, Backup e rollback, Catalogo, Cobblemon / Minecraft, Comandos estruturados adicionais, CPU/Scheduler, Criar restore point, Dashboard (+24 more)

### Community 18 - "CobblemonEasyView"
Cohesion: 0.12
Nodes (9): EasyPrimaryAction, ExportButton, PrimaryActionButton, RestoreButton, SaveTestButton, CobblemonEasyView, RoutedEventArgs, Task (+1 more)

### Community 19 - "RoutedEventArgs"
Cohesion: 0.09
Nodes (17): AdvancedModeTabButton, ApplyProfileButton, ApplyQuarantineButton, AuditButton, BenchmarkButton, BrowseButton, CancelBenchmarkButton, EasyModeTabButton (+9 more)

### Community 21 - "MinecraftScientificModels.cs"
Cohesion: 0.11
Nodes (20): MinecraftBottleneckDiagnosis, MinecraftExperimentMeasurement, MinecraftScientificComparison, ModConfigAutomationStatus, ScientificActionKind, ScientificActionRisk, ScientificBenchmarkOutcome, ScientificConfidence (+12 more)

### Community 22 - "MinecraftScientificExperimentService"
Cohesion: 0.13
Nodes (14): MinecraftInstanceEvidence, MinecraftScientificExperiment, MinecraftScientificOperationResult, ScientificHypothesis, MinecraftScientificExperimentService, DateTimeOffset, HashSet, IReadOnlyDictionary (+6 more)

### Community 23 - "HardwareTelemetryService"
Cohesion: 0.12
Nodes (9): Computer, float, KernelLatencyTracker, BenchmarkState, HardwareTelemetryService, double, JsonSerializerOptions, Task (+1 more)

### Community 24 - "ApexTweaker.Models"
Cohesion: 0.12
Nodes (12): ApexTweaker.Services, ApexTweaker, ApexTweaker.Application.Optimizations, ApexTweaker.Infrastructure, ApexTweaker.Models, ApexTweaker.Core.Pipeline, ApexTweaker.UI.Wpf, UninstallTarget (+4 more)

### Community 25 - "MinecraftScientificReportService"
Cohesion: 0.11
Nodes (17): Description, StatusGlyph, StatusKey, StatusLabel, CompetitiveModeButton, DisableVbsButton, OptimizeFullscreenButton, StatusItemsControl (+9 more)

### Community 26 - "ApexTweaker.Minecraft.Models"
Cohesion: 0.12
Nodes (7): ApexTweaker.Minecraft.Models, ApexTweaker.Minecraft, ApexTweaker.Minecraft.Services, BottleneckCandidate, Contract, FileMutation, ProfileOperation

### Community 27 - "MinecraftWizardViewModel"
Cohesion: 0.12
Nodes (12): IRelayCommand, ObservableObject, MinecraftVisualStateItem, MinecraftWizardStepState, MinecraftWizardStepViewModel, MinecraftWizardViewModel, bool, double (+4 more)

### Community 28 - "SystemDiagnosticsService"
Cohesion: 0.12
Nodes (7): DevMode, GamingFpsProbeSelfTest, SystemDiagnosticsService, DllImport, IReadOnlyList, RegistryKey, string

### Community 29 - "Plano mestre de coordenação"
Cohesion: 0.20
Nodes (9): Backend / orquestração — `.agents/skills/`, Codex / agents (usuário), Como o orquestrador seleciona skills, Frontend — `.claude/skills/`, Política, Projeto ApexTweaker, Skills já instaladas na máquina (observadas), Skills planejadas (criar depois da aprovação) (+1 more)

### Community 30 - "ExtremeMutationCommands.cs"
Cohesion: 0.13
Nodes (16): GROUP_AFFINITY, LOGICAL_PROCESSOR_RELATIONSHIP, SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER, int, CACHE_RELATIONSHIP, GROUP_AFFINITY, L3CacheDescriptor, LOGICAL_PROCESSOR_RELATIONSHIP (+8 more)

### Community 32 - "ValorantProcessOptimizer"
Cohesion: 0.16
Nodes (14): AffinityPlan, NativeProcessOptimizationResult, ValorantProcessOptimizer, Action, CancellationToken, DllImport, HashSet, int (+6 more)

### Community 33 - ".Run"
Cohesion: 0.10
Nodes (9): PowercfgValueIndexCommand, SendMessageTimeoutFlags, CaptureBcdValue(), TweakService, BackupService, DllImport, int, IntPtr (+1 more)

### Community 34 - "ApexTweaker"
Cohesion: 0.14
Nodes (14): ApexTweaker, net10.0-windows, System.Management (10.0.2), Microsoft.NET.Sdk, CommunityToolkit.Mvvm (8.4.2), LibreHardwareMonitorLib (0.9.6), Microsoft.Diagnostics.Tracing.TraceEvent (3.1.21), net10.0 (+6 more)

### Community 35 - "ModJarScanner"
Cohesion: 0.19
Nodes (10): FileInfo, JsonDocument, ModJarScanner, Dictionary, HashSet, JsonElement, List, long (+2 more)

### Community 36 - ".BuildPlan"
Cohesion: 0.15
Nodes (11): Border, RiskBadge, RiskLevel, DependencyObject, DependencyProperty, DependencyPropertyChangedEventArgs, TextBlock, Snackbar (+3 more)

### Community 37 - "AppThemeManager"
Cohesion: 0.14
Nodes (16): ISet, ActiveBar, ActiveOverlay, HoverOverlay, PressOverlay, ResourceDictionary, RootBorder, RootGrid (+8 more)

### Community 38 - "UtilitiesView"
Cohesion: 0.17
Nodes (12): AboutButton, CleanTempButton, RepairButton, RevertButton, RiotSupportButton, StorageSenseButton, TrimButton, UninstallButton (+4 more)

### Community 39 - "HardwareTelemetryService.cs"
Cohesion: 0.13
Nodes (16): EventArgs, CpuTelemetryKind, FrametimeCorrelationEvent, GameProcessInfo, LOGICAL_PROCESSOR_RELATIONSHIP, ProcessorCoreDescriptor, SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER, TelemetryDiagnosticEventArgs (+8 more)

### Community 40 - "TelemetrySnapshot"
Cohesion: 0.17
Nodes (4): FrametimeCorrelationEvent, IHardware, ISensor, TelemetrySnapshot

### Community 41 - "MutationSession"
Cohesion: 0.14
Nodes (15): BcdValueSnapshot, CommandStateSnapshot, MutationSession, PowerSchemeSnapshot, PowerSettingSnapshot, ProcessStateSnapshot, RegistryValueSnapshot, ServiceStateSnapshot (+7 more)

### Community 42 - ".TrySetDword"
Cohesion: 0.08
Nodes (17): CancelEventArgs, KeyEventArgs, MouseButtonEventArgs, CommandPaletteInput, CommandPaletteList, CommandPaletteOverlay, MainWindow, BackupService (+9 more)

### Community 43 - "TelemetryPipeServer"
Cohesion: 0.13
Nodes (13): Channel, NamedPipeServerStream, TelemetryPipeServer, bool, byte, CancellationToken, CancellationTokenSource, ConcurrentDictionary (+5 more)

### Community 44 - "GpuOptimizationService"
Cohesion: 0.16
Nodes (10): DisplayAdapterDevice, GpuInfo, DisplayAdapterDevice, GpuMutationPlan, GpuOptimizationService, IReadOnlyList, JsonElement, List (+2 more)

### Community 45 - "WindowsOptimizationModels.cs"
Cohesion: 0.19
Nodes (15): WindowsOptimizationCatalog, IReadOnlyList, IReadOnlySet, AdmxPolicyReference, EvidenceLevel, OptimizationDecisionKind, OptimizationRequirement, PerformanceEvidence (+7 more)

### Community 46 - "CommandRunner"
Cohesion: 0.16
Nodes (9): CommandResult, int, Process, CommandRunner, CancellationToken, Task, HypervisorTweakCommand, TimerResolutionTweakCommand (+1 more)

### Community 47 - "EtwFrameTracker"
Cohesion: 0.12
Nodes (12): EtwFrameTracker, bool, CancellationToken, CancellationTokenSource, ConcurrentDictionary, int, long, object (+4 more)

### Community 48 - "Pastas principais"
Cohesion: 0.10
Nodes (20): Dados em disco, Estrutura do projeto, `installer`, `native/ApexTweaker.Native`, Pastas geradas pelo .NET, Pastas principais, Pipeline de mutacao, `release-installer` (+12 more)

### Community 49 - "MinecraftAuditModels.cs"
Cohesion: 0.09
Nodes (27): AuditSeverity, BenchmarkStatus, DiskInfo, JavaMemoryTier, MinecraftAuditIssue, MinecraftAuditSummary, MinecraftHomologationCriterion, MinecraftLoader (+19 more)

### Community 50 - ".SetCurrentStep"
Cohesion: 0.16
Nodes (10): Plan, Reports, MinecraftScientificOptimizationPlan, MinecraftScientificReportPaths, MinecraftScientificReportService, IEnumerable, IReadOnlyDictionary, JsonSerializerOptions (+2 more)

### Community 51 - "MainWindow"
Cohesion: 0.14
Nodes (5): OperationalHomologationStatus, EasyStepState, EasyStepViewModel, bool, IEnumerable

### Community 52 - "Minecraft Scientific Optimization Engine"
Cohesion: 0.11
Nodes (19): CLI completa, Contratos de configuracao de mods, Decisao de linguagem, Diagnostico de gargalo, Fato, inferencia e recomendacao, Fluxo GUI para uma instancia Prism real, Fontes tecnicas fixadas, Limitacoes restantes (+11 more)

### Community 53 - "KernelLatencyTracker"
Cohesion: 0.14
Nodes (9): DPCTraceData, ISRTraceData, KernelLatencyTracker, Action, CancellationTokenSource, long, object, string (+1 more)

### Community 54 - "AffinityIsolationCommand"
Cohesion: 0.15
Nodes (11): L3CacheDescriptor, ProcessorIsolationTopology, AffinityIsolationCommand, ProcessorIsolationTopology, Dictionary, DllImport, HashSet, IntPtr (+3 more)

### Community 55 - "MinecraftEasyModeService"
Cohesion: 0.17
Nodes (7): MinecraftContentProfileKind, MinecraftEasyModSummary, MinecraftEasyModeService, Func, IReadOnlyCollection, IReadOnlyList, string

### Community 56 - "WindowsPowerModeService"
Cohesion: 0.28
Nodes (4): WindowsPowerModeService, DllImport, Guid, string

### Community 57 - "EdgeRemovalTweakCommand"
Cohesion: 0.18
Nodes (7): Arguments, FileName, EdgeRemovalTweakCommand, BackupService, bool, string, UninstallTarget

### Community 58 - "UserControl"
Cohesion: 0.11
Nodes (18): BadgeLevel, Category, BoolToVis, Guidance, Impact, KindLabel, Reason, RequiresRestart (+10 more)

### Community 59 - "FabricVersionConstraint"
Cohesion: 0.30
Nodes (6): IComparable, FabricVersionConstraint, VersionNumber, GeneratedRegex, Regex, VersionNumber

### Community 60 - "WindowsOptimizationService"
Cohesion: 0.23
Nodes (6): WindowsOptimizationApplicationFacade, IWindowsOptimizationInventory, GamingPerformanceProbe, WindowsUsageProfile, WindowsOptimizationService, IReadOnlyList

### Community 61 - "MinecraftModDescriptor"
Cohesion: 0.11
Nodes (16): MinecraftBackupManifest, MinecraftQuarantineApplyResult, MinecraftQuarantineCandidate, MinecraftQuarantineConfirmation, MinecraftQuarantineFileEntry, MinecraftQuarantineManifest, MinecraftQuarantineRollbackResult, QuarantineRisk (+8 more)

### Community 62 - "CpuTopologyProfile"
Cohesion: 0.18
Nodes (9): CpuTelemetryKind, CpuTopologyProfile, ProcessorCoreDescriptor, CpuTopologyProfile, HashSet, IntPtr, List, LOGICAL_PROCESSOR_RELATIONSHIP (+1 more)

### Community 63 - "LightweightBenchmarkChart"
Cohesion: 0.14
Nodes (12): DrawingContext, FrameworkElement, INotifyCollectionChanged, NotifyCollectionChangedEventArgs, LightweightBenchmarkChart, Color, DependencyObject, DependencyProperty (+4 more)

### Community 64 - "Contrato FE ↔ BE (in-process)"
Cohesion: 0.12
Nodes (15): 1. Transporte, 2.1 Diagnóstico, 2.2 Mutações Windows (legado), 2.3 Backup / rollback, 2.4 Minecraft, 2. Superfície legada usada pela UI (`MainWindow`), 3.1 Entrada, 3.2 Enums (confirmados no models WIP) (+7 more)

### Community 65 - ".MonitorLoopAsync"
Cohesion: 0.17
Nodes (6): GameProcessInfo, WindowNativeMethods, CancellationToken, DllImport, IReadOnlyList, Process

### Community 66 - ".TryReadDword"
Cohesion: 0.17
Nodes (11): 1. OperationOutcome com Models/Contracts congelados, 2. Gate CommandRunner ≠ única barreira, 3. CatalogFeedbackSelfTest sem harness, 4. Captura de agentes, Gate aceitar BACKEND, Gate aceitar FRONTEND, Gates de revisão — rodada BE-DEMO-OUTCOME-P0 + FE-FEEDBACK-SHELL-P0, Ordem de integração (+3 more)

### Community 68 - "OptimizationEngine"
Cohesion: 0.14
Nodes (10): HardwareInfo, PresetRecommendation, CpuArchitectureProfile, OptimizationEngine, ProcessorBoostDecision, Action, double, string (+2 more)

### Community 69 - "DashboardView"
Cohesion: 0.16
Nodes (8): AutoOptimizeButton, RestorePointButton, SummaryText, UserControl, DashboardView, RoutedEventArgs, Button, TextBlock

### Community 70 - "Arquitetura-alvo — ApexTweaker"
Cohesion: 0.22
Nodes (8): Arquitetura-alvo — ApexTweaker, Contratos a estabilizar (Orquestrador), Critérios de sucesso arquitetural, Fronteiras de migração (sem big-bang), Máquina de estados oficial (operações longas), Princípio, Sistema visual alvo (resumo — detalhe em `docs/ux/`), Visão estrutural

### Community 71 - "TelemetryPipeClient"
Cohesion: 0.15
Nodes (12): IDisposable, NamedPipeClientStream, TelemetryPipeClient, bool, CancellationToken, CancellationTokenSource, int, JsonSerializerOptions (+4 more)

### Community 72 - "MinecraftBenchmarkResult"
Cohesion: 0.17
Nodes (9): MinecraftBenchmarkResult, MinecraftOperationalHomologationResult, MinecraftOperationalObservation, ScientificEvidence, MinecraftOperationalHomologationService, MinecraftScientificMetricsService, ICollection, IEnumerable (+1 more)

### Community 74 - "Arquitetura do backend"
Cohesion: 0.14
Nodes (13): ApexTweaker, ApexTweaker.Application, ApexTweaker.Contracts, ApexTweaker.Native, ApexTweaker.Windows, Arquitetura do backend, Compatibilidade, Estado atual (+5 more)

### Community 75 - "Auditoria estrutural do backend"
Cohesion: 0.14
Nodes (13): Acoplamento, Arquivos da primeira etapa, Auditoria estrutural do backend, Codigo grande e duplicacao, Contratos, Escopo e restricoes, Estrutura observada, Estrutura proposta (+5 more)

### Community 76 - "README.md"
Cohesion: 0.26
Nodes (3): Contratos usados pelo perfil, Matriz Fabric 1.21.1, Pacote local auditado

### Community 77 - "Cobblemon Low-End Lab v3.3.1"
Cohesion: 0.14
Nodes (14): Auditoria real do pacote local, Backup e rollback do perfil, Benchmark, CLI, Cobblemon Low-End Lab v3.3.1, EXTREME_4GB, Fluxos, Instancias reconhecidas (+6 more)

### Community 78 - "Frontend handoff — L2 (Otimização Windows / Presets Gamer)"
Cohesion: 0.25
Nodes (7): Achados, Arquivos alterados nesta auditoria, Baixa / polish, Escopo executado, Frontend handoff, Pendencias, Sem bug confirmado na UI

### Community 79 - "Homologacao operacional Cobblemon em 4 GB"
Cohesion: 0.14
Nodes (14): Adicionar ImmediatelyFast, Alternativa Modrinth App, Aplicar EXTREME_4GB, Benchmark operacional, Checklist do ZIP portatil, Comandos CLI equivalentes, Criar a instancia real no Prism Launcher, Decisao sobre Indium (+6 more)

### Community 80 - ".TryResolve"
Cohesion: 0.17
Nodes (11): 2026-07-24 14:32 — Orquestracao FPS-P0-P1, 2026-07-24 14:38 � Orquestracao FE-ALL + FPS-BE, 2026-07-24 14:47 � BE PASS; FE finish relaunch, 2026-07-24 14:57 � Integracao no main, 2026-07-24 — discovery only (sem execução de agentes), 2026-07-24 — FPS P0/P1 orquestrado, 2026-07-24 — L2-SHELL-2 (parcial), 2026-07-24 — L2-SHELL despachado (Claude) (+3 more)

### Community 81 - ".Apply"
Cohesion: 0.19
Nodes (12): BottleneckCandidate, MinecraftAuditResult, MinecraftBottleneckKind, ModConfigContractAssessment, ScientificDerivedMetrics, MinecraftBottleneckDiagnosticService, ICollection, IReadOnlyList (+4 more)

### Community 82 - "ISystemMutationCommand"
Cohesion: 0.18
Nodes (4): ISystemMutationCommand, SystemMutationCommand, Action, BackupService

### Community 83 - "UserControl"
Cohesion: 0.27
Nodes (5): ModulesView, DependencyObject, IEnumerable, RoutedEventArgs, WpfButton

### Community 84 - "MutationExecutor"
Cohesion: 0.06
Nodes (33): Action, AsyncLocal, BackupService, Category, DateTimeOffset, Exception, MutationPipelineScope, MutationSession (+25 more)

### Community 85 - "Backend handoff (template)"
Cohesion: 0.18
Nodes (10): Arquivos alterados, Backend handoff, Contratos / Models, Diff resumido, Escopo executado, Observado vs inferido, Pendencias, Propostas de contrato para fase futura (+2 more)

### Community 86 - "WindowsOptimizationRule"
Cohesion: 0.34
Nodes (7): WindowsOptimizationRecommendationService, WindowsOptimizationContext, WindowsOptimizationDecision, WindowsOptimizationPlan, WindowsOptimizationRule, IReadOnlyList, CatalogRowViewModel

### Community 87 - "TextBox"
Cohesion: 0.20
Nodes (7): ApexTweaker.UI.Wpf.Controls, ApexTweaker.UI.Wpf.Theming, ApexTweaker.UI.Wpf.Views, ApexTweaker.UI.Wpf.ViewModels, BenchmarkChartPoint, CommandPaletteItem, EasyPrimaryAction

### Community 88 - "MainWindow.xaml.cs"
Cohesion: 0.33
Nodes (4): MarketCoverageSelfTest, MarketUtilitiesService, IEnumerable, IReadOnlyList

### Community 89 - "Estado atual da arquitetura — ApexTweaker"
Cohesion: 0.20
Nodes (10): Confiabilidade — lacunas confirmadas, Confiabilidade — o que já existe, Duas gerações de otimização Windows, Empacotamento, Entrada, Estado atual da arquitetura — ApexTweaker, Fluxo UI → domínio, Persistência (+2 more)

### Community 90 - "Minecraft geral e hooks de sessao - v3.3.1"
Cohesion: 0.17
Nodes (12): CLI, Desativado, Destino do teste, Extremo, Hooks de sessao, Limites reais, Minecraft geral e hooks de sessao - v3.3.1, O que nao foi implementado (+4 more)

### Community 91 - "CatalogViewModel"
Cohesion: 0.18
Nodes (9): BiosChecklistCatalog, BiosChecklistItem, IReadOnlyList, CatalogViewModel, SelectedPreset, StatusText, IReadOnlyList, ObservableCollection (+1 more)

### Community 92 - ".Run"
Cohesion: 0.31
Nodes (8): CoreButtonsPanel, GamesButtonsPanel, GpuButtonsPanel, MarketButtonsPanel, PeripheralButtonsPanel, UserControl, StackPanel, WrapPanel

### Community 93 - "MarketUtilitiesService"
Cohesion: 0.24
Nodes (5): MinecraftSelfTest, Action, Color, IReadOnlyDictionary, MinecraftEnvironmentSnapshot

### Community 94 - "NetworkInterruptModerationTweakCommand"
Cohesion: 0.22
Nodes (6): NetworkInterruptModerationTweakCommand, BackupService, bool, List, RegistryKey, string

### Community 95 - "MinecraftScientificExperimentStore"
Cohesion: 0.29
Nodes (6): Anti-padrões (grandes marcas que a comunidade odeia), Copy / tom, Inspirações FE — maturidade “nível corporação”, Mapa para ApexTweaker (estado atual → maduro), Próximo sprint FE sugerido, Referências boas (copiar padrões)

### Community 96 - "WindowsOptimizationInventoryService.cs"
Cohesion: 0.42
Nodes (4): MinecraftInstanceEvidenceService, IEnumerable, IReadOnlyDictionary, IReadOnlyList

### Community 98 - "MinecraftSelfTest.cs"
Cohesion: 0.15
Nodes (4): RegistryService, RegistryKey, List, RegistryKey

### Community 99 - "MemoryCompressionTweakCommand"
Cohesion: 0.36
Nodes (3): MemoryCompressionTweakCommand, BackupService, string

### Community 100 - "Matriz de propriedade (ownership)"
Cohesion: 0.25
Nodes (7): Agentes, Arquivo misturado, Matriz de propriedade (ownership), Matriz por caminho, Proibições, Revisão cruzada, Worktrees

### Community 101 - "Primeiro teste seguro em 4 GB"
Cohesion: 0.25
Nodes (8): Criterio minimo, Evidencias e logs, Experimento real, Mods do primeiro baseline, Preparacao, Preset inicial obrigatorio, Primeiro teste seguro em 4 GB, Privilegio

### Community 102 - "ApexTweaker v3.3.1 - Minecraft Rapido"
Cohesion: 0.25
Nodes (7): ApexTweaker v3.3.1 - Minecraft Rapido, Assets locais, Novo fluxo, Objetivo, Seguranca preservada, Simplificacao visual, Validacao

### Community 104 - "ApexTweaker"
Cohesion: 0.25
Nodes (8): ApexTweaker, Build local, Dados e seguranca, Distribuicao, Interface, Linha de comando, Minecraft One-Click Mode, Minecraft Scientific Optimization Engine

### Community 105 - ".Run"
Cohesion: 0.19
Nodes (7): WindowsOptimizationSelfTest, IReadOnlyList, WindowsDeviceKind, WindowsPowerSource, DllImport, MarshalAs, SystemPowerStatus

### Community 106 - "ProcessorIdleStatesTweakCommand"
Cohesion: 0.32
Nodes (3): ProcessorIdleStatesTweakCommand, BackupService, string

### Community 107 - ".ExecuteAsync"
Cohesion: 0.25
Nodes (6): MasterRollbackService, BackupService, CancellationToken, IProgress, IReadOnlyList, Task

### Community 108 - "CatalogView"
Cohesion: 0.25
Nodes (6): PresetCombo, CatalogView, RoutedEventArgs, SelectionChangedEventArgs, ComboBox, WpfUserControl

### Community 109 - "EasyFpsComboBox"
Cohesion: 0.29
Nodes (7): SelectedFps, SelectedHookMode, SelectedFps, SelectedHookMode, EasyFpsComboBox, EasyHookModeComboBox, ComboBox

### Community 111 - "Distribuicao"
Cohesion: 0.29
Nodes (7): Artefato portatil (oficial), Como gerar o portatil, Como o cliente deve executar, Distribuicao, Fluxo de release recomendado, Instalador (opcional), Observacoes

### Community 112 - "MinecraftEasyModSummary"
Cohesion: 0.15
Nodes (8): ProfileOperation, MinecraftExperimentDefinition, MinecraftExperimentVariable, MinecraftProfileApplyResult, MinecraftProfilePlan, MinecraftExtremeExperimentCatalog, int, IReadOnlyList

### Community 115 - ".RunTweakAsync"
Cohesion: 0.20
Nodes (9): Achados Fase 1 (resumo), Backlog priorizado, Entregas recentes (histórico curto), Norte, Não fazer agora, Próximo passo após revisão humana, Top 5 causas prováveis de travamento, Top 5 problemas arquiteturais (+1 more)

### Community 116 - "Integration status"
Cohesion: 0.33
Nodes (5): Commits, Entregue nesta rodada, Integration status, Pendencias conhecidas, Verificacao

### Community 117 - "Market coverage matrix — ApexTweaker vs EXM / BoosterX"
Cohesion: 0.33
Nodes (5): Categorias cobertas (menu EXM Free), Decisões, Explicitamente fora, Mapa de lotes, Market coverage matrix — ApexTweaker vs EXM / BoosterX

### Community 118 - ".EnsureAdministratorForWindowsOperation"
Cohesion: 0.28
Nodes (5): ContentControl, CancellationToken, FrameworkElement, Task, TransformGroup

### Community 119 - "MinecraftBenchmarkService.cs"
Cohesion: 0.50
Nodes (4): BcdBackupEntry, TweakBackup, DateTime, IReadOnlyList

### Community 120 - "StartupDisclaimerWindow"
Cohesion: 0.24
Nodes (8): AcceptCheckBox, ConfirmButton, Window, StartupDisclaimerWindow, RoutedEventArgs, Button, CheckBox, Window

### Community 121 - "ValorantLocator"
Cohesion: 0.16
Nodes (5): ValorantLocator, IEnumerable, string, Func, IReadOnlyList

### Community 122 - "TestResultPanel"
Cohesion: 0.40
Nodes (5): BoolToVisibility, IsTestPanelVisible, IsTestPanelVisible, TestResultPanel, Border

### Community 123 - "Documentação ApexTweaker"
Cohesion: 0.33
Nodes (6): Arquitetura, Contratos e coordenação, Documentação ApexTweaker, Essencial, Minecraft / Cobblemon, Produto, confiabilidade, qualidade, UX (Fase 1 — 2026-07-25)

### Community 125 - "MinecraftEasyCorrectionPlan"
Cohesion: 0.18
Nodes (3): MinecraftEasyCorrectionPlan, MinecraftEasyInstanceStatus, MinecraftEasyServerReadiness

### Community 126 - "GameOpenedCheckBox"
Cohesion: 0.50
Nodes (4): GameOpened, GameOpened, GameOpenedCheckBox, CheckBox

### Community 128 - "PageTransitionAnimator"
Cohesion: 0.27
Nodes (9): PageTransitionAnimator, DependencyObject, DependencyProperty, DoubleAnimation, IEasingFunction, object, Storyboard, TimeSpan (+1 more)

### Community 129 - "PerformanceView"
Cohesion: 0.28
Nodes (6): RootCard, Window, LoadingWindow, int, RoutedEventArgs, Border

### Community 133 - "DevMode"
Cohesion: 0.67
Nodes (3): short, DevMode, int

### Community 134 - "Pirâmide alvo"
Cohesion: 0.15
Nodes (12): A11y / visual, Contratos, E2E (máquina dedicada ou VM), Estratégia de testes — ApexTweaker, Falha / recuperação, Integração, Pirâmide alvo, Plataforma (+4 more)

### Community 135 - "QuarantineList"
Cohesion: 0.67
Nodes (3): IssueList, QuarantineList, ListBox

### Community 145 - "MsiModeTweakCommand"
Cohesion: 0.23
Nodes (4): ResolvedGpuInterruptTarget, MpoTweakCommand, MsiModeTweakCommand, string

### Community 146 - "TextBox"
Cohesion: 0.17
Nodes (9): AverageFpsTextBox, JavaArgumentsTextBox, JoinSecondsTextBox, MenuSecondsTextBox, MinimumFpsTextBox, OperationalNotesTextBox, PathTextBox, TextChangedEventArgs (+1 more)

### Community 147 - "WindowsOptimizationInventoryService.cs"
Cohesion: 0.18
Nodes (9): ApexTweaker.Contracts.Inventory, ApexTweaker.Windows.Inventory, ComputerInventory, DeviceGuardInventory, OperatingSystemInventory, ProcessorInventory, SystemPowerStatus, byte (+1 more)

### Community 148 - "Estratégia de recuperação — ApexTweaker"
Cohesion: 0.18
Nodes (10): Camadas de recuperação, Diagnóstico exportável (alvo), Estratégia de recuperação — ApexTweaker, Fechamento / hang, Objetivos, Recovery após falha — checklist operacional, Reinício, Rollback (+2 more)

### Community 149 - "SystemRestoreService"
Cohesion: 0.29
Nodes (4): SystemRestoreService, int, IReadOnlyList, string

### Community 150 - "Jornadas de usuário — ApexTweaker"
Cohesion: 0.20
Nodes (9): Estados de interface desejados (todas as jornadas críticas), Jornada A — Otimizar Windows (núcleo), Jornada B — Desempenho pontual (estabilidade), Jornada C — Minecraft rápido, Jornada D — Recuperação após falha / crash, Jornada E — Atualização / instalação / desinstalação, Jornadas de usuário — ApexTweaker, Mapa de telas (shell) (+1 more)

### Community 151 - "Prompt Codex — BE-DEMO-OUTCOME-P0"
Cohesion: 0.22
Nodes (8): Condições obrigatórias (aprovadas), Contratos congelados, Entrega, Escopo de arquivos, Fora de escopo, Graphify (obrigatório), Leia primeiro, Prompt Codex — BE-DEMO-OUTCOME-P0

### Community 152 - "Visão de produto — ApexTweaker"
Cohesion: 0.22
Nodes (8): Critérios de sucesso de produto (norte), Não-objetivos (agora), O que é, Para quem, Princípios de prioridade (obrigatórios), Promessa de produto, Proposta de valor vs estado atual, Visão de produto — ApexTweaker

### Community 153 - "Proposta de sistema visual — ApexTweaker"
Cohesion: 0.22
Nodes (8): Acessibilidade mínima, Auditoria visual (estado), Direção (inspiração, não cópia), Estados de interação obrigatórios, Fluxos a redesenhar primeiro (não tudo), Princípios (resumo), Proposta de sistema visual — ApexTweaker, Tokens (evoluir a partir do existente)

### Community 155 - "Taxonomia de erros — ApexTweaker"
Cohesion: 0.25
Nodes (7): Anti-padrões proibidos, Categorias, Contrato `ErrorDescriptor` (alvo), Mapeamento de exceções .NET (inicial), Princípio, Severidade de produto, Taxonomia de erros — ApexTweaker

### Community 156 - "MinecraftEnvironmentService.cs"
Cohesion: 0.32
Nodes (7): nuint, DisplayDevice, MemoryStatusEx, PerformanceInformation, string, uint, ulong

### Community 157 - "Prompt Claude Code — FE-FEEDBACK-SHELL-P0"
Cohesion: 0.29
Nodes (6): Condições obrigatórias (aprovadas), Contratos / arquivos congelados, Entrega, Graphify (obrigatório), Leia primeiro, Prompt Claude Code — FE-FEEDBACK-SHELL-P0

### Community 159 - "MinecraftBenchmarkService.cs"
Cohesion: 0.40
Nodes (5): BenchmarkEvidence, IoCounters, MemoryStatusEx, uint, ulong

### Community 160 - "Quality gates — ApexTweaker"
Cohesion: 0.40
Nodes (4): Bloqueadores absolutos, Gate checklist (orquestrador), Integração, Quality gates — ApexTweaker

### Community 162 - "Acessibilidade — ApexTweaker"
Cohesion: 0.50
Nodes (3): Acessibilidade — ApexTweaker, Observado, Requisitos mínimos (Fase 4+)

## Knowledge Gaps
- **520 isolated node(s):** `Escopo executado`, `Arquivos alterados`, `Diff resumido`, `Contratos / Models`, `Propostas de contrato para fase futura` (+515 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **31 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainWindow` connect `.TrySetDword` to `Win32MinecraftSessionHookPlatform`, `MinecraftProfileService`, `MinecraftBenchmarkService`, `TelemetryView`, `MinecraftAuditResult`, `MinecraftEnvironmentService`, `Window`, `TweakService`, `MinecraftView`, `.WriteLine`, `MinecraftScientificExperimentService`, `HardwareTelemetryService`, `MinecraftScientificReportService`, `SystemDiagnosticsService`, `.EnsureAdministratorForWindowsOperation`, `.Run`, `UtilitiesView`, `EtwFrameTracker`, `MinecraftAuditModels.cs`, `MinecraftEasyModeService`, `WindowsOptimizationService`, `MinecraftModDescriptor`, `OptimizationEngine`, `DashboardView`, `MinecraftBenchmarkResult`, `.Apply`, `UserControl`, `TextBox`, `MainWindow.xaml.cs`, `MinecraftEnvironmentService.cs`, `.ExecuteAsync`, `CatalogView`, `MinecraftEasyModSummary`, `StartupDisclaimerWindow`, `ValorantLocator`, `MinecraftEasyCorrectionPlan`?**
  _High betweenness centrality (0.387) - this node is a cross-community bridge._
- **Why does `MinecraftView` connect `MinecraftView` to `Win32MinecraftSessionHookPlatform`, `MinecraftBenchmarkService`, `UserControl`, `MinecraftBenchmarkResult`, `.TrySetDword`, `MinecraftAuditResult`, `CatalogView`, `MinecraftAuditModels.cs`, `TextBox`, `RoutedEventArgs`, `MainWindow`, `MinecraftScientificModels.cs`, `MinecraftEasyModeService`, `TextBox`, `MinecraftWizardViewModel`, `MinecraftBenchmarkSample`, `MinecraftEasyCorrectionPlan`?**
  _High betweenness centrality (0.098) - this node is a cross-community bridge._
- **Why does `HardwareTelemetryService` connect `HardwareTelemetryService` to `.StartMonitoringGame`, `.MonitorLoopAsync`, `TelemetryPipeClient`, `TelemetrySnapshot`, `HardwareTelemetryService.cs`, `.TrySetDword`, `TelemetryPipeServer`, `EtwFrameTracker`, `KernelLatencyTracker`, `CpuTopologyProfile`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **Are the 42 inferred relationships involving `CobblemonEasyViewModel` (e.g. with `ApproximateFps` and `AuditReady`) actually correct?**
  _`CobblemonEasyViewModel` has 42 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Escopo executado`, `Arquivos alterados`, `Diff resumido` to the rest of the system?**
  _520 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Win32MinecraftSessionHookPlatform` be split into smaller, more focused modules?**
  _Cohesion score 0.06295715778474399 - nodes in this community are weakly interconnected._
- **Should `MinecraftProfileService` be split into smaller, more focused modules?**
  _Cohesion score 0.09049773755656108 - nodes in this community are weakly interconnected._