# Integration status — P0 demo gate + feedback shell

**Branch:** `integration/p0-demo-outcome-feedback`  
**Atualizado:** 2026-07-25  
**Base:** `8706126`  
**Não mergeado em `main` ainda**  
**PR:** https://github.com/NGK-999/tweaker/pull/2 — **REQUEST CHANGES** (revisão humana); P0.2 em andamento/corrigido nesta branch

## Commits nesta branch

| Papel | Commit | Origem |
|-------|--------|--------|
| Backend | `bf03db7` (cherry-pick de `0a1c68f`) | BE-DEMO-OUTCOME-P0 |
| Frontend | `787e649` (cherry-pick de `97b38e5`) | FE-FEEDBACK-SHELL-P0 |
| Harness | `70411ae` | `test: wire catalog feedback self-test` |
| Docs | `eb25c82` | consolidate P0 status |
| P0.1 fail-closed | `6fc38c7` | `CommandIntent` ReadOnly\|Mutation\|Unknown |
| P0.2 blockers | *(este commit)* | LastOutcome / cancel / ledger / classifier / Snackbar |

## Resultados

```text
Backend commit integrado: bf03db7 (0a1c68f)
Frontend commit integrado: 787e649 (97b38e5)
Harness commit: 70411ae
P0.1 commit: 6fc38c7
P0.2: corrige 5 bloqueadores da revisao do PR #2
Build: PASS
demo-self-test: PASS (exit 0) — adversarial + LastOutcome isolation + cancel rethrow
gaming-fps-probe-self-test: PASS (exit 0)
catalog-feedback-self-test: PASS (exit 0)
Teste manual: GUI smoke pendente apos push
Problemas encontrados: revisao humana REQUEST CHANGES → P0.2
Dívidas aceitas: RegistryService direto ainda fora do CommandRunner; progress SetStatus ainda Info
Próxima tarefa: re-revisao humana do PR #2; merge so apos aprovacao; nao aplicar stash wip-pre-p0-integration-*
```

## P0.2 — bloqueadores fechados

1. **LastOutcome isolado:** `LastOutcome = null` no inicio; atribuicao forcada apos ledger (sem `??=`).
2. **Cancelamento:** `OperationCanceledException` registrada, outcome montado, depois **relancada**; `Execute` nao engole OCE; UI trata OCE antes de `Exception`.
3. **Ledger:** outcome final sempre reconstruido **depois** de `CommitMutationSession` (rollback do commit aparece no outcome).
4. **Classificador adversarial:** so System32/SysWOW64 (ou nome bare PATH); misturas read+mutation → `Unknown`; `C:\Temp\powercfg.exe` bloqueado.
5. **Snackbar:** call sites criticos migrados para `SnackbarKind` explicito; Catalog analisa apos wiring do evento; sem `ex.Message` no copy principal.

## Gate final checklist

```text
[x] Diff formal do backend revisado
[x] Gate fail-closed confirmado (Standard-only allow)
[x] Demo nao altera Windows via pipeline principal / CommandRunner classificado
[x] OperationOutcome interno coerente (P0.2: por execucao + apos ledger)
[x] Cancelamento propaga
[x] Classificador adversarial coberto no self-test
[x] SetStatus criticos tipados
[x] Build Release PASS
[x] demo-self-test PASS
[x] gaming-fps-probe-self-test PASS
[x] catalog-feedback-self-test PASS
[ ] Re-revisao humana / aprovacao merge
[ ] Git limpo (graphify-out dirty esperado)
```

**Merge em `main`:** aguarda aprovação humana explícita após re-revisão do P0.2.
