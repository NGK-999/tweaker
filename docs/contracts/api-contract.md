# Contrato FE ↔ BE (in-process)

Data: 2026-07-24  
Formato: **não há OpenAPI** — o host é WPF no mesmo processo.  
Arquivo OpenAPI: **não aplicável** (marcado abaixo).

Legenda de confiança:

- **confirmado** — lido no código atual
- **parcialmente confirmado** — existe, mas incompleto / WIP
- **inconsistente** — duas implementações divergem
- **ausente** — desejado pelo produto, não encontrado

---

## 1. Transporte

| Item | Valor | Confiança |
|------|-------|-----------|
| Protocolo principal | Chamada de método C# in-process | confirmado |
| HTTP REST | — | ausente |
| OpenAPI | — | ausente / não aplicável |
| Named pipe telemetria | `TelemetryPipeServer` / `TelemetryPipeClient` | parcialmente confirmado (IPC auxiliar, não é API de tweaks) |
| CLI | `Program` → `MinecraftCommandLine` / flags `--demo*` | confirmado |

Conclusão: `docs/contracts/api.openapi.yaml` **não deve ser inventado** nesta fase.

---

## 2. Superfície legada usada pela UI (`MainWindow`)

Confiança: **confirmado** (leituras em `MainWindow.xaml.cs`).

### 2.1 Diagnóstico

| Operação | Callee | Request | Response |
|----------|--------|---------|----------|
| Hardware | `SystemDiagnosticsService.GetHardwareInfo()` | — | `HardwareInfo` |
| Relatório | `BuildDiagnosticReport()` | — | `IReadOnlyList<string>` |
| Já otimizado? | `OptimizationEngine.CheckIfAlreadyOptimized()` | — | `bool` |
| Recomendação preset | `OptimizationEngine.Analyze(HardwareInfo)` | hardware | `PresetRecommendation` |

Enums relacionados (`src/Models`):

- `PresetKind`: `Safe`, `Competitive`, `Extreme` — **confirmado**
- `HardwareTier` — **confirmado**

### 2.2 Mutações Windows (legado)

Padrão: `Task.Run(() => tweakService.<Method>(...))` → `IReadOnlyList<string>` (linhas de log).

| Intenção UI | Método | Confiança |
|-------------|--------|-----------|
| Auto-Tuning | `ApplyAutonomousOptimization(exe?)` | confirmado |
| Restore point | `CreateRestorePoint()` | confirmado |
| Energia | `ApplyPowerTweaks()` | confirmado |
| CPU | `ApplyCpuSchedulerTweaks()` / architecture | confirmado |
| GPU display | `ApplyGpuDisplayTweaks(exe?)` | confirmado |
| Input | `ApplyInputTweaks()` | confirmado |
| Rede | `ApplyNetworkTweaks()` | confirmado |
| Políticas/Serviços | `ApplyPolicyAndServiceTweaks()` | confirmado (**inconsistente** com catálogo GPO novo) |
| Background | `ApplyBackgroundTweaks()` | confirmado |
| Latência extrema | `ApplyExtremeLatencyTweaks(hardware?)` | confirmado |
| Rollback | `RevertLastAppliedState()` / MasterRollback | confirmado |

Estados de execução na UI: flags internas de “tweaking” (`TryBeginTweaking` / `EndTweaking`) — **parcialmente confirmado** (estado UI, não enum de domínio).

### 2.3 Backup / rollback

| Operação | Callee | Confiança |
|----------|--------|-----------|
| Backup | `BackupService.CreateBackup()` | confirmado |
| Sessão mutação | `BeginMutationSession` + captures | confirmado |
| Restore latest | `RestoreLatestMutationSession` / MasterRollback | confirmado |

### 2.4 Minecraft

Contratos ricos em `src/Minecraft/Models/*` — **confirmado**.
UI: `CobblemonEasyViewModel`, `MinecraftWizardViewModel`, wiring em `MainWindow`.
Fora do escopo imediato do lote GPO Windows, mas devem ser preservados.

---

## 3. Superfície nova Windows Optimization (WIP Codex)

Confiança: **parcialmente confirmado** (código untracked; UI ainda não consome).

### 3.1 Entrada

```csharp
WindowsOptimizationPlan Analyze(
    WindowsOptimizationPreset preset,
    WindowsUsageProfile? usage = null);
```

Localização WIP (em movimento durante a auditoria):

- host adapter: `src/App/WindowsOptimizationService.cs`
- fachada/catálogo: `src/ApexTweaker.Application/Optimizations/*`
- inventário: `src/ApexTweaker.Windows/Inventory/*`
- models: `src/ApexTweaker.Contracts/Optimizations/WindowsOptimizationModels.cs`

Legado anterior (pode ter sido movido): `src/Services/WindowsOptimizationService.cs`.

### 3.2 Enums (confirmados no models WIP)

| Enum | Valores |
|------|---------|
| `WindowsOptimizationPreset` | GamerSafe, Competitive, StreamerGamePass, GamingLaptop, ExperimentalBenchmark |
| `WindowsOptimizationRisk` | Safe, Conditional, Experimental, Dangerous |
| `WindowsOptimizationPurpose` | Fps, FrameTime, Latency, Network, Privacy, UserInterface, Stability |
| `PerformanceEvidence` | Measured, Plausible, None, Conflicting |
| `OptimizationDecisionKind` | Recommended, RequiresConfirmation, ExperimentalOnly, AlreadyConfigured, NotApplicable, Blocked |
| `UsageAnswer` | Unknown, No, Yes |

### 3.3 Objetos

- `WindowsUsageProfile` — flags de uso (Game Pass, Game Bar, OBS, OneDrive, …)
- `WindowsOptimizationContext` — inventário SO/hardware/domínio/MDM/VBS/…
- `WindowsOptimizationRule` — regra de catálogo (+ `AdmxPolicyReference?`)
- `WindowsOptimizationDecision` — regra + kind + reason
- `WindowsOptimizationPlan` — preset + context + decisions (+ helpers Recommended/Blocked/…)

### 3.4 Ausente no contrato novo

| Capacidade | Status |
|------------|--------|
| Apply / LGPO | ausente |
| Progresso de apply (%) | ausente |
| Resultado de benchmark acoplado ao plano | ausente |
| Códigos de erro HTTP | N/A |
| `AlreadyConfigured` preenchido de fato | parcialmente confirmado (enum existe; avaliação pode não setar) |
| Ponte com `PresetKind` legado | ausente / inconsistente |

---

## 4. Segurança / demo gate

| Mecanismo | Comportamento | Confiança |
|-----------|---------------|-----------|
| `RuntimeMode.IsDemo` | bloqueia mutações no `CommandRunner` (fail-safe) | confirmado (WIP) |
| `--demo-self-test` | valida classificação leitura vs escrita | confirmado (WIP) |
| Manifest `asInvoker` | elevação sob demanda | confirmado (`Test-Release.ps1`) |

Regra de contrato: UI em desenvolvimento deve preferir `--demo` até L6.

---

## 5. Inconsistências a resolver (orquestrador)

1. `PresetKind` vs `WindowsOptimizationPreset` — nomes parecidos, sem mapeamento.
2. `ApplyPolicyAndServiceTweaks` vs catálogo novo — políticas/serviços conflitantes.
3. `MainWindow` acoplado a implementações concretas — impede FE paralelo seguro.
4. Docs antigas citando WinForms — desatualizadas.

---

## 6. Evolução do contrato (somente orquestrador)

Próximos campos só entram em `src/Models` após aprovação:

- `WindowsOptimizationApplyRequest` (ids de regras + confirmações)
- `WindowsOptimizationApplyResult` (ok/fail, rebootRequired, backupId)
- `OptimizationProgress` (phase, percent, message)

Até lá, FE não deve inventar DTOs paralelos.
