# Master plan — status

**Atualizado:** 2026-07-28

| Task | Status |
|------|--------|
| FPS-P0-P1-BE | **DONE** |
| FE-ALL-P0 | **DONE** (Catalog rows + sidebar + Snackbar/Ctrl+K) |
| Performance probe UI | **DONE** (`CaptureGamingPerformanceProbe`) |
| Close hang fix | **DONE** |
| BE-DEMO-OUTCOME-P0 + FE-FEEDBACK-SHELL-P0 | **MERGED** via PR #2 (`de49cc3`) |
| P0.1 / P0.2 / P0.2.1 | **MERGED** (HEAD integrado `eacf9ec`) |
| Kernel | **fora** |
| FE-SHELL-FLUENCY-P1 | **DONE** — transition leve + Analyze/probe off UI thread |
| UI-OUTCOME-P1 | **DONE** (branch `integration/ui-outcome-polish`) |
| FE-SHELL-POLISH-P1 | **DONE** (Snackbar/headers/busy/motion) |
| CTT-WINUTIL-PARITY | **DONE** (branch `integration/ctt-winutil-parity`) |
| FE-DISTILL-MINIMAL | **DONE** (branch `integration/fe-distill-minimal`) |

**PR #2:** MERGED (merge commit, sem squash). Stash WIP **não** aplicado.

## Próximo lote (dívidas não bloqueantes)

1. **DEMO-INVENTORY-P1** — Secure Boot/TPM sem PowerShell em Demo (API tipada ou coletor dedicado).
2. **CI-REQUIRED-P1** — GitHub Actions obrigatório no HEAD (build + self-tests).
3. Merge PR #3 / #4 / distill conforme revisão.

Opcional depois: WPF-UI NuGet incremental; `$impeccable init`; restaurar stash `wip-pre-p0-integration-*` só com cuidado consciente.
