# Master plan — status

**Atualizado:** 2026-07-25

| Task | Status |
|------|--------|
| FPS-P0-P1-BE | **DONE** |
| FE-ALL-P0 | **DONE** (Catalog rows + sidebar + Snackbar/Ctrl+K) |
| Performance probe UI | **DONE** (`CaptureGamingPerformanceProbe`) |
| Close hang fix | **DONE** |
| BE-DEMO-OUTCOME-P0 + FE-FEEDBACK-SHELL-P0 | **MERGED** via PR #2 (`de49cc3`) |
| P0.1 / P0.2 / P0.2.1 | **MERGED** (HEAD integrado `eacf9ec`) |
| Kernel | **fora** |

**PR #2:** MERGED (merge commit, sem squash). Stash WIP **não** aplicado.

## Próximo lote (dívidas não bloqueantes)

1. **UI-OUTCOME-P1** — consumir `OperationOutcome` na UI (`RunTweakAsync` / Auto-Tuning); timeout/cancel não podem parecer sucesso.
2. **DEMO-INVENTORY-P1** — Secure Boot/TPM sem PowerShell em Demo (API tipada ou coletor dedicado).
3. **CI-REQUIRED-P1** — GitHub Actions obrigatório no HEAD (build + self-tests).

Opcional depois: WPF-UI NuGet incremental / polish; restaurar stash `wip-pre-p0-integration-*` só com cuidado consciente.
