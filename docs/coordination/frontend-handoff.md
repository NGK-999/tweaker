# Frontend handoff — FE-FEEDBACK-SHELL-P0

**Status:** DONE (worktree) — aguarda revisão do orquestrador
**Branch:** `agent/claude-fe-feedback-shell-p0`
**Worktree:** `C:\projetos\Apextweaker-claude`
**Executor:** Orquestrador (recuperação após falha do subagent Claude por limite de API)
**Data:** 2026-07-25

## Escopo entregue

1. `SnackbarKind.Error` + borda `ErrorBrush`
2. Removido `ClassifySnackbarKind` (sem substring no shell SetStatus)
3. Estados distintos Empty / Partial / Error / Ready via `CatalogFeedbackKind`
4. CTA: “Ir ao Dashboard para usar Auto-Optimize” (navegação; tooltip deixa claro que não inicia apply)
5. `AutomationProperties.Name` em Analisar / Retry / GoToAuto
6. Sem contrato `OperationOutcome` no FE

## Arquivos alterados (allowed)

| Arquivo | Mudança |
|---------|---------|
| `src/UI/Wpf/Controls/Snackbar.cs` | +Error |
| `src/UI/Wpf/ViewModels/CatalogFeedbackState.cs` | **novo** máquina de estado UI |
| `src/UI/Wpf/ViewModels/CatalogViewModel.cs` | feedback + catch Analyze |
| `src/UI/Wpf/Views/CatalogView.xaml` | painéis Empty/Partial/Error + CTA |
| `src/UI/Wpf/Views/CatalogView.xaml.cs` | eventos shell + focus retry |
| `src/UI/Wpf/Testing/CatalogFeedbackSelfTest.cs` | **novo** asserts |
| `src/UI/Wpf/MainWindow.xaml.cs` | Catalog wiring; `SetStatus(msg, kind)`; remove classifier |

## Testes

| Item | Status |
|------|--------|
| Build Release | **EXECUTADO** — PASS (0 erros) |
| `CatalogFeedbackSelfTest` compile | **COMPILADO** |
| `CatalogFeedbackSelfTest.Run()` via Program | **NÃO EXECUTADO — AGUARDA HARNESS** (`Program.cs` congelado nesta rodada) |
| Anti-substring `ClassifySnackbarKind` | **EXECUTADO** — removido de MainWindow |
| rg Contains erro no escopo FE desta task | MainWindow limpo; `TelemetryView.xaml.cs` ainda tem heurística **fora do escopo** |

### Proposta (não aplicada)

```text
Program.cs: if args --catalog-feedback-self-test → Environment.Exit(CatalogFeedbackSelfTest.Run());
```

## Evidências git (UI)

```text
git diff --stat (UI + handoff): ver worktree
git diff --check: limpo nos arquivos UI
```

## Pendências

- Wire `--catalog-feedback-self-test` no integrate (orquestrador)
- Migrar call sites legados de `SetStatus` que hoje caem em Info (antes Warning via substring) — task futura
- TelemetryView Contains("erro") — dívida fora desta task

## Não feito / respeitado

- Sem redesign amplo
- Sem Models/Contracts/Services/App/Program
- Sem OperationOutcome compartilhado
