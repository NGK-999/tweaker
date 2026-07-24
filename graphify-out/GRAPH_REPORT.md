# Graph Report - Apextweaker  (2026-07-24)

## Corpus Check
- 156 files · ~125,361 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 741 nodes · 1437 edges · 36 communities (33 shown, 3 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 33 edges (avg confidence: 0.77)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `be75095c`
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

## God Nodes (most connected - your core abstractions)
1. `MainWindow` - 114 edges
2. `TweakService` - 81 edges
3. `Funcionalidades do ApexTweaker` - 26 edges
4. `WindowsOptimizationInventoryService` - 20 edges
5. `Window` - 19 edges
6. `UserControl` - 17 edges
7. `ApexTweaker.Models` - 16 edges
8. `CatalogViewModel` - 15 edges
9. `Frontend handoff — L2 (Otimização Windows / Presets Gamer)` - 13 edges
10. `CpuTopologyNative` - 13 edges

## Surprising Connections (you probably didn't know these)
- `UserControl` --references--> `CatalogViewModel`  [INFERRED]
  src/UI/Wpf/Views/CatalogView.xaml → src/UI/Wpf/ViewModels/CatalogViewModel.cs
- `WindowsOptimizationApplicationFacade` --references--> `WindowsOptimizationRecommendationService`  [EXTRACTED]
  src/ApexTweaker.Application/Optimizations/WindowsOptimizationApplicationFacade.cs → src/ApexTweaker.Application/Optimizations/WindowsOptimizationRecommendationService.cs
- `WindowsOptimizationInventoryService` --implements--> `IWindowsOptimizationInventory`  [EXTRACTED]
  src/ApexTweaker.Windows/Inventory/WindowsOptimizationInventoryService.cs → src/ApexTweaker.Contracts/Inventory/IWindowsOptimizationInventory.cs
- `CatalogViewModel` --references--> `WindowsOptimizationPreset`  [EXTRACTED]
  src/UI/Wpf/ViewModels/CatalogViewModel.cs → src/ApexTweaker.Contracts/Optimizations/WindowsOptimizationModels.cs
- `CatalogViewModel` --references--> `WindowsOptimizationService`  [EXTRACTED]
  src/UI/Wpf/ViewModels/CatalogViewModel.cs → src/App/WindowsOptimizationService.cs

## Import Cycles
- None detected.

## Communities (36 total, 3 thin omitted)

### Community 0 - "MainWindow"
Cohesion: 0.06
Nodes (44): ApplicationOperation, bool, CancelEventArgs, CancellationTokenSource, DashboardView, EtwFrameTracker, HardwareTelemetryService, MasterRollbackService (+36 more)

### Community 1 - "TweakService"
Cohesion: 0.08
Nodes (20): GpuMutationPlan, GpuOptimizationService, HardwareInfo, ISystemMutationCommand, MutationExecutor, MutationSession, PowercfgValueIndexCommand, SendMessageTimeoutFlags (+12 more)

### Community 2 - "ApexTweaker.Models"
Cohesion: 0.26
Nodes (6): ApexTweaker.Application.Optimizations, ApexTweaker.Contracts.Inventory, ApexTweaker.Models, WindowsOptimizationApplicationFacade, IWindowsOptimizationInventory, WindowsOptimizationService

### Community 3 - "CpuTopologyNative"
Cohesion: 0.07
Nodes (32): CoreGroupMapping, ApexTweaker.NativeInterop, LOGICAL_PROCESSOR_RELATIONSHIP, HardwareEnvironmentDetectionResult, HardwareEnvironmentDetector, HashSet, CoreGroupMapping, IntelHybridProbeStrategy (+24 more)

### Community 4 - "WindowsOptimizationModels.cs"
Cohesion: 0.11
Nodes (26): IReadOnlySet, WindowsOptimizationCatalog, IReadOnlyList, WindowsOptimizationRecommendationService, WindowsOptimizationSelfTest, IReadOnlyList, AdmxPolicyReference, EvidenceLevel (+18 more)

### Community 5 - "Contexto completo do ApexTweaker"
Cohesion: 0.17
Nodes (10): ObservableCollection, ObservableObject, BiosChecklistCatalog, BiosChecklistItem, IReadOnlyList, CatalogViewModel, SelectedPreset, StatusText (+2 more)

### Community 6 - "Window"
Cohesion: 0.09
Nodes (23): MouseButtonEventArgs, AppVersionText, CatalogButton, CloseButton, DashboardButton, HeaderSubtitleText, HeaderTitleText, MaximizeButton (+15 more)

### Community 7 - "UserControl"
Cohesion: 0.06
Nodes (31): Category, Guidance, KindLabel, Name, Reason, RiskLabel, RiskNote, Title (+23 more)

### Community 8 - "Funcionalidades do ApexTweaker"
Cohesion: 0.06
Nodes (32): Background, Backup e rollback, Catalogo, Cobblemon / Minecraft, Comandos estruturados adicionais, CPU/Scheduler, Criar restore point, Dashboard (+24 more)

### Community 9 - "WindowsOptimizationInventoryService"
Cohesion: 0.11
Nodes (12): ComputerInventory, DeviceGuardInventory, OperatingSystemInventory, ProcessorInventory, GpuInfo, WindowsOptimizationInventoryService, DllImport, IReadOnlyList (+4 more)

### Community 10 - "Plano mestre de coordenação"
Cohesion: 0.07
Nodes (24): 2026-07-24 — discovery only (sem execução de agentes), 2026-07-24 — L2-SHELL-2 (parcial), 2026-07-24 — L2-SHELL despachado (Claude), 2026-07-24 — MARKET B1–B9 (modo A), Entradas, Execution log, Comandos oficiais de verificação, Definição de pronto da coordenação (fase integração) (+16 more)

### Community 11 - ".Run"
Cohesion: 0.14
Nodes (13): Action, Color, IMinecraftSessionHookPlatform, IReadOnlyDictionary, MinecraftEnvironmentSnapshot, MinecraftSessionHookAction, MinecraftSessionHookMode, MinecraftSessionPlatformSnapshot (+5 more)

### Community 12 - "UtilitiesView"
Cohesion: 0.19
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
Cohesion: 0.24
Nodes (4): ApexTweaker.Services, ApexTweaker.UI.Wpf.Views, ApexTweaker.UI.Wpf.ViewModels, ApexTweaker.Minecraft

### Community 17 - "Arquitetura do backend"
Cohesion: 0.14
Nodes (13): ApexTweaker, ApexTweaker.Application, ApexTweaker.Contracts, ApexTweaker.Native, ApexTweaker.Windows, Arquitetura do backend, Compatibilidade, Estado atual (+5 more)

### Community 18 - "Auditoria estrutural do backend"
Cohesion: 0.14
Nodes (13): Acoplamento, Arquivos da primeira etapa, Auditoria estrutural do backend, Codigo grande e duplicacao, Contratos, Escopo e restricoes, Estrutura observada, Estrutura proposta (+5 more)

### Community 19 - "Frontend handoff — L2 (Otimização Windows / Presets Gamer)"
Cohesion: 0.14
Nodes (13): Arquivos criados, Arquivos modificados, Arquivos removidos, Commit, Contratos afetados, Decisões de UX, Erros restantes, Frontend handoff — L2 (Otimização Windows / Presets Gamer) (+5 more)

### Community 20 - "Estado atual da arquitetura — ApexTweaker"
Cohesion: 0.06
Nodes (32): Acoplamentos e riscos, Build / lint / testes (confirmados), Estado atual da arquitetura — ApexTweaker, Estado Git (no momento da auditoria / atualização), Fluxo UI → backend (in-process), Legado (em produção na UI), Novo (WIP Codex, ainda não ligado à UI), O que NÃO existe (confirmado ausente) (+24 more)

### Community 21 - "Backend handoff (template)"
Cohesion: 0.15
Nodes (12): Arquivos criados, Arquivos modificados, Arquivos removidos, Backend handoff (template), Commit, Contratos afetados, Decisões tomadas, Erros restantes (+4 more)

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
Cohesion: 0.40
Nodes (4): Backend Task — Market coverage B1–B8 (modo A), Escopo, Proibido, Verificação

### Community 26 - "Frontend Task — Catalogo B9 (modo A)"
Cohesion: 0.50
Nodes (3): Entregue, Follow-up visual, Frontend Task — Catalogo B9 (modo A)

### Community 32 - "MarketUtilitiesService"
Cohesion: 0.29
Nodes (5): IEnumerable, MarketCoverageSelfTest, MarketUtilitiesService, CommandRunner, IReadOnlyList

### Community 33 - "WindowsOptimizationInventoryService.cs"
Cohesion: 0.22
Nodes (8): ApexTweaker.Windows.Inventory, ComputerInventory, DeviceGuardInventory, OperatingSystemInventory, ProcessorInventory, SystemPowerStatus, byte, uint

### Community 34 - "MainWindow.xaml.cs"
Cohesion: 0.32
Nodes (4): ApexTweaker, ApexTweaker.UI.Wpf, Program, STAThread

## Knowledge Gaps
- **203 isolated node(s):** `Minecraft One-Click Mode`, `Minecraft Scientific Optimization Engine`, `Interface`, `Linha de comando`, `Distribuicao` (+198 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainWindow` connect `MainWindow` to `MarketUtilitiesService`, `TweakService`, `MainWindow.xaml.cs`, `Window`, `UserControl`, `UtilitiesView`?**
  _High betweenness centrality (0.262) - this node is a cross-community bridge._
- **Why does `TweakService` connect `TweakService` to `MainWindow`, `TweakService.cs`?**
  _High betweenness centrality (0.131) - this node is a cross-community bridge._
- **Why does `ApexTweaker.Services` connect `UserControl` to `WindowsOptimizationInventoryService.cs`, `ApexTweaker.Models`, `CpuTopologyNative`, `MainWindow.xaml.cs`, `TweakService.cs`?**
  _High betweenness centrality (0.100) - this node is a cross-community bridge._
- **What connects `Minecraft One-Click Mode`, `Minecraft Scientific Optimization Engine`, `Interface` to the rest of the system?**
  _203 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MainWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.061211340206185565 - nodes in this community are weakly interconnected._
- **Should `TweakService` be split into smaller, more focused modules?**
  _Cohesion score 0.07668418537983755 - nodes in this community are weakly interconnected._
- **Should `CpuTopologyNative` be split into smaller, more focused modules?**
  _Cohesion score 0.06972789115646258 - nodes in this community are weakly interconnected._