# Integration status — P0 demo gate + feedback shell

**Branch:** `integration/p0-demo-outcome-feedback`  
**Atualizado:** 2026-07-25  
**Base:** `8706126`  
**Não mergeado em `main` ainda**

## Commits nesta branch

| Papel | Commit | Origem |
|-------|--------|--------|
| Backend | `bf03db7` (cherry-pick de `0a1c68f`) | BE-DEMO-OUTCOME-P0 |
| Frontend | `787e649` (cherry-pick de `97b38e5`) | FE-FEEDBACK-SHELL-P0 |
| Harness | `70411ae` | `test: wire catalog feedback self-test` |
| Docs | `eb25c82` | consolidate P0 status |
| P0.1 fail-closed | *(este commit)* | `CommandIntent` ReadOnly\|Mutation\|Unknown |

## Resultados

```text
Backend commit integrado: bf03db7 (0a1c68f)
Frontend commit integrado: 787e649 (97b38e5)
Harness commit: 70411ae
P0.1 commit: (após commit local)
Build: PASS
demo-self-test: PASS (exit 0) — inclui unknown/ambiguous/PowerShell/cmd
gaming-fps-probe-self-test: PASS (exit 0)
catalog-feedback-self-test: PASS (exit 0) — COMPILADO / EXECUTADO / PASS
Teste manual: GUI smoke OK; sem órfãos
Problemas encontrados: bypass fail-open CORRIGIDO na P0.1
Dívidas aceitas: RegistryService direto ainda fora do CommandRunner (protegido via MutationExecutor); allowlist ReadOnly pode precisar expansão pontual
Próxima tarefa: PR para main (sem squash); não aplicar stash wip-pre-p0-integration-*
```

## Revisão formal backend `0a1c68f` (orquestrador)

### Escopo
- PASS: só `Program.cs`, `DemoSafetySelfTest`, `CommandRunner`, `OperationOutcome`, `RuntimeModeContext`, `MutationExecutor`, handoff/task.
- Sem UI/Models/Contracts/csproj/sln/logs/agent artifacts no commit.
- `git diff --check`: trailing whitespace só em `backend-task.md` (docs).

### Fail-closed
- PASS: `EvaluateMutation` permite **somente** `Standard`; `Demo` e `Unknown` bloqueiam.
- `Program` configura Demo ou Standard explicitamente (nunca deixa Unknown no GUI).
- MutationExecutor bloqueia **antes** de `BeginMutationSession`.
- CommandRunner bloqueia mutações classificadas **antes** de `Process.Start`.
- Não usa padrão perigoso `if (!isDemo) mutate`.

### OperationOutcome
- PASS interno rico: CorrelationId, Kind, timestamps, flags (Demo/Cancelled/TimedOut/Restart/Rollback/RolledBack/MutationBlocked), Messages, Steps.
- BLOCKED: `Kind=Failed` + `MutationBlocked=true` + `OperationStepStatus.Blocked` (aceitável; não parece erro genérico no log `[BLOQUEADO]`).
- Cancelled / TimedOut tratados **antes** de `catch (Exception)`.

### Self-test efetividade
- PASS: tenta `powercfg /setactive` e recebe bloqueio; legado `TweakService` sob Demo/Unknown retorna `[BLOQUEADO]`.
- Outcomes via comandos stub (sem SetValue real) sob Standard **abrem sessão BackupService** (I/O de ledger app) — dívida menor, não é reg/GPO/BCD/serviço.

### P0.1 — classificação fail-closed (`CommandIntent`)

- `ReadOnly` = allowlist explícita; `Mutation` / `Unknown` = bloqueados fora de `Standard`.
- Fallback **nunca** retorna leitura: executável desconhecido → `Unknown`.
- PowerShell/cmd sempre `Mutation` (não confirmáveis como read-only).
- Códigos: `COMMAND_NOT_CONFIRMED_READ_ONLY` / `COMMAND_MUTATION_BLOCKED`.

### Inventário de bypass residual

| Caminho | Classificação |
|---------|----------------|
| `CommandRunner` + `CommandClassifier` | Fail-closed em Demo/Unknown (P0.1) |
| `RegistryService.Set*` via `MutationExecutor` | Protegido pelo gate do executor |
| `RegistryService` fora do pipeline | Dívida futura (migração) |
| `Inventory` OpenSubKey | Leitura |
| `MainWindow` Process.Start (URLs) | Fora do pipeline tweak |
| Minecraft `new Process` | Domínio MC paralelo |

## Frontend self-test

```text
Status: COMPILADO
Execução: EXECUTADO
Resultado: PASS
Harness: --catalog-feedback-self-test (70411ae)
```

## WIP preservado

`git stash` criado antes da integração: `wip-pre-p0-integration-*` (docs Fase 1 + fixes PERF locais não commitados). Restaurar com cuidado após merge.

## Gate final checklist

```text
[x] Diff formal do backend revisado
[x] Nenhum arquivo fora do escopo (no commit BE)
[x] Gate fail-closed confirmado (Standard-only allow)
[x] Configuração ausente/Unknown bloqueia mutação
[x] Demo não altera Windows via pipeline principal / CommandRunner classificado
[x] Caminhos diretos de mutação inventariados (dívidas registradas)
[x] OperationOutcome interno coerente
[x] Build Release PASS após BE
[x] Build Release PASS após FE
[x] demo-self-test PASS
[x] gaming-fps-probe-self-test PASS
[x] catalog-feedback-self-test PASS
[x] Inicialização normal testada (smoke)
[x] Fechamento normal testado (Stop-Process; sem órfão)
[x] Sem processo órfão
[ ] Git limpo (graphify-out dirty esperado)
[x] Documentação de integração atualizada (este arquivo)
```

**Merge em `main`:** aguarda aprovação humana explícita.
