# Graph Report - Apextweaker  (2026-07-24)

## Corpus Check
- 169 files · ~134,599 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 760 nodes · 1454 edges · 32 communities (30 shown, 2 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 33 edges (avg confidence: 0.77)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c9c30bc8`
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

## God Nodes (most connected - your core abstractions)
1. `MainWindow` - 114 edges
2. `TweakService` - 81 edges
3. `Funcionalidades do ApexTweaker` - 26 edges
4. `WindowsOptimizationInventoryService` - 20 edges
5. `Contexto completo do ApexTweaker` - 19 edges
6. `Window` - 19 edges
7. `UserControl` - 17 edges
8. `ApexTweaker.Models` - 16 edges
9. `CatalogViewModel` - 15 edges
10. `Frontend handoff — L2 (Otimização Windows / Presets Gamer)` - 13 edges

## Surprising Connections (you probably didn't know these)
- `WindowsOptimizationApplicationFacade` --references--> `WindowsOptimizationRecommendationService`  [EXTRACTED]
  src/ApexTweaker.Application/Optimizations/WindowsOptimizationApplicationFacade.cs → src/ApexTweaker.Application/Optimizations/WindowsOptimizationRecommendationService.cs
- `WindowsOptimizationInventoryService` --implements--> `IWindowsOptimizationInventory`  [EXTRACTED]
  src/ApexTweaker.Windows/Inventory/WindowsOptimizationInventoryService.cs → src/ApexTweaker.Contracts/Inventory/IWindowsOptimizationInventory.cs
- `CatalogViewModel` --references--> `WindowsOptimizationPreset`  [EXTRACTED]
  src/UI/Wpf/ViewModels/CatalogViewModel.cs → src/ApexTweaker.Contracts/Optimizations/WindowsOptimizationModels.cs
- `CatalogViewModel` --references--> `WindowsOptimizationService`  [EXTRACTED]
  src/UI/Wpf/ViewModels/CatalogViewModel.cs → src/App/WindowsOptimizationService.cs
- `MainWindow` --references--> `MarketUtilitiesService`  [EXTRACTED]
  src/UI/Wpf/MainWindow.xaml.cs → src/Services/MarketUtilitiesService.cs

## Import Cycles
- None detected.

## Communities (32 total, 2 thin omitted)

### Community 0 - "MainWindow"
Cohesion: 0.06
Nodes (44): ApplicationOperation, bool, CancelEventArgs, CancellationTokenSource, DashboardView, EtwFrameTracker, HardwareTelemetryService, MasterRollbackService (+36 more)

### Community 1 - "TweakService"
Cohesion: 0.08
Nodes (20): GpuMutationPlan, GpuOptimizationService, HardwareInfo, ISystemMutationCommand, MutationExecutor, MutationSession, PowercfgValueIndexCommand, SendMessageTimeoutFlags (+12 more)

### Community 2 - "ApexTweaker.Models"
Cohesion: 0.06
Nodes (29): ApexTweaker.Services, ApexTweaker, ApexTweaker.Application.Optimizations, ApexTweaker.UI.Wpf.Views, ApexTweaker.UI.Wpf.ViewModels, ApexTweaker.Contracts.Inventory, ApexTweaker.Minecraft, ApexTweaker.Models (+21 more)

### Community 3 - "CpuTopologyNative"
Cohesion: 0.07
Nodes (32): CoreGroupMapping, ApexTweaker.NativeInterop, LOGICAL_PROCESSOR_RELATIONSHIP, HardwareEnvironmentDetectionResult, HardwareEnvironmentDetector, HashSet, CoreGroupMapping, IntelHybridProbeStrategy (+24 more)

### Community 4 - "WindowsOptimizationModels.cs"
Cohesion: 0.11
Nodes (26): IReadOnlySet, WindowsOptimizationCatalog, IReadOnlyList, WindowsOptimizationRecommendationService, WindowsOptimizationSelfTest, IReadOnlyList, AdmxPolicyReference, EvidenceLevel (+18 more)

### Community 5 - "Contexto completo do ApexTweaker"
Cohesion: 0.05
Nodes (41): 10. Estado de verificacao conhecido, 11. Ordem de trabalho recomendada, 12. Matriz minima de testes antes da proxima release, 13. Criterios para declarar uma otimizacao valida, 14. Regras de colaboracao para proximos agentes, 15. Prompt curto para retomar o trabalho, 16. Minecraft/Cobblemon 3.1.0, 17. Cobblemon Facil 3.2.0 (+33 more)

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
Cohesion: 0.18
Nodes (10): CoreButtonsPanel, GpuButtonsPanel, MarketButtonsPanel, PeripheralButtonsPanel, UserControl, ModulesView, RoutedEventArgs, WpfButton (+2 more)

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
Cohesion: 0.15
Nodes (12): Acoplamentos e riscos, Build / lint / testes (confirmados), Estado atual da arquitetura — ApexTweaker, Estado Git (no momento da auditoria / atualização), Fluxo UI → backend (in-process), Legado (em produção na UI), Novo (WIP Codex, ainda não ligado à UI), O que NÃO existe (confirmado ausente) (+4 more)

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

## Knowledge Gaps
- **221 isolated node(s):** `graphify`, `Memoria e verificacao no Hermes`, `net10.0-windows`, `CommunityToolkit.Mvvm (8.4.2)`, `LibreHardwareMonitorLib (0.9.6)` (+216 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainWindow` connect `MainWindow` to `TweakService`, `ApexTweaker.Models`, `Window`, `UserControl`, `UtilitiesView`, `UserControl`?**
  _High betweenness centrality (0.249) - this node is a cross-community bridge._
- **Why does `TweakService` connect `TweakService` to `MainWindow`, `ApexTweaker.Models`?**
  _High betweenness centrality (0.125) - this node is a cross-community bridge._
- **Why does `ApexTweaker.Services` connect `ApexTweaker.Models` to `CpuTopologyNative`?**
  _High betweenness centrality (0.095) - this node is a cross-community bridge._
- **What connects `graphify`, `Memoria e verificacao no Hermes`, `net10.0-windows` to the rest of the system?**
  _221 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MainWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.061211340206185565 - nodes in this community are weakly interconnected._
- **Should `TweakService` be split into smaller, more focused modules?**
  _Cohesion score 0.07668418537983755 - nodes in this community are weakly interconnected._
- **Should `ApexTweaker.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.061495457721872815 - nodes in this community are weakly interconnected._