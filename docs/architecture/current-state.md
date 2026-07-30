# Estado atual da arquitetura — ApexTweaker

**Data da auditoria:** 2026-07-25  
**HEAD observado:** `8706126` (+ alterações locais da auditoria PERF ainda não commitadas)  
**Método:** graphify query/path/explain + confirmação em fonte  
**Fase:** 1 — somente leitura (este doc atualiza o de 2026-07-24)

## Resumo

Host **.NET 10 + WPF** monólito in-process com projetos auxiliares:

| Projeto | Papel |
|---------|-------|
| `ApexTweaker.csproj` | WinExe / shell |
| `src/ApexTweaker.Contracts` | DTOs Windows Optimization + probe |
| `src/ApexTweaker.Application` | fachada Analyze / recommendation |
| `src/ApexTweaker.Windows` | inventário |
| `src/Services`, `Core`, `Infrastructure` | mutação legado + CommandRunner |
| `src/Minecraft` | motor paralelo |
| `native/ApexTweaker.Native` | C++ opcional |
| `src/UI/Wpf` | Views / Themes / Controls |

**Não há** HTTP, OpenAPI, Electron, projeto de testes xUnit/NUnit, nem `.github/workflows`.

## Entrada

`src/App/Program.cs`:

- `--market-coverage-self-test`
- `--gaming-fps-probe-self-test`
- CLI Minecraft
- GUI: Disclaimer → Loading → `MainWindow`
- Handlers: `AppDomain.UnhandledException`, `DispatcherUnhandledException` → MessageBox “CRASH FATAL”

**Observado:** não há `RuntimeMode.IsDemo` / `--demo` no tree atual (docs antigos descreviam WIP que **não está no HEAD**).

## Fluxo UI → domínio

```text
MainWindow (god-object ~102 KB)
  ├─ TweakService (~75 KB) → MutationExecutor → BackupService
  ├─ WindowsOptimizationService → Analyze + probe + 3 applies pontuais
  ├─ OptimizationEngine (PresetKind legado)
  ├─ BackupService / MasterRollbackService
  ├─ EtwFrameTracker / HardwareTelemetryService
  └─ Minecraft* services
```

Graphify: `MainWindow → TweakService → MutationExecutor` (2 hops).

## Duas gerações de otimização Windows

| Geração | UI | Apply |
|---------|----|-------|
| Legado `PresetKind` + `TweakService` | Dashboard Auto, Módulos | sim |
| Novo `WindowsOptimization*` | Catálogo Analyze, Desempenho probe | plano **sem** Apply; 3 ações pontuais só |

Contrato: `docs/contracts/api-contract.md` §3.4 — Apply/LGPO **ausente**.

## Confiabilidade — o que já existe

- `CommandRunner` com timeout padrão **2 min**, cancelamento, kill best-effort.
- `MutationExecutor`: Validate → Snapshot → Execute → Verify → Commit ledger.
- `TryBeginTweaking` / `EndTweaking` — mutex UI booleano.
- Fechamento: hide + teardown ≤2s + force exit (fix `488adb2`).
- FSO AppCompat: merge de flags (auditoria PERF local).

## Confiabilidade — lacunas confirmadas

| Lacuna | Evidência | Impacto |
|--------|-----------|---------|
| Sem enum oficial de outcome | UI só `isTweaking` + strings | estados inconsistentes |
| Sem correlationId | logs são `IReadOnlyList<string>` | suporte/diagnóstico frágil |
| Snackbar sem `Error` | `SnackbarKind` = Info/Success/Warning | falha parece warning |
| Classify por substring | `ClassifySnackbarKind` | frágil |
| Catálogo usage hardcoded | `WindowsUsageProfile.Unknown` | recomendações conservadoras cegas |
| Sem CI | sem `.github/workflows` | regressão só local |
| God files | MainWindow ~102KB, TweakService ~75KB, HardwareTelemetry ~77KB | ownership e risco de hang |
| Demo gate ausente | sem `IsDemo` no código | risco de mutar máquina de dev |

## Persistência

- Backups / mutation sessions via `BackupService` sob ApplicationPaths.
- Minecraft recovery state escrito em disco (`WriteRecoveryState`).
- Sem store tipado de “última operação Windows” para resume na UI.

## Empacotamento

Scripts PowerShell em `scripts/` (quando presentes): build release / installer / test-release.  
Manifest: elevação sob demanda (`asInvoker` — docs/scripts).  
Update channel de produto: **não verificado** como jornada tipada.

## Testes atuais

Self-tests CLI (não pirâmide):

- `--gaming-fps-probe-self-test`
- `--market-coverage-self-test`
- Minecraft self-test / CLI

Build Release local = evidência principal hoje. **Insuficiente** como único gate (ver `docs/quality/*`).
