# UI-OUTCOME-P1

**Status:** in progress  
**Branch (integration):** `integration/ui-outcome-polish`  
**Worktree Claude:** `C:\projetos\Apextweaker-claude` / `agent/claude-fe-outcome-polish`

## Goal

Timeout / soft-fail / partial outcomes must not look like Success in the snackbar.

## Contract

1. Orchestrator exposes `TweakService.LastMutationOutcome` → `mutationExecutor.LastOutcome`.
2. FE maps `OperationOutcomeKind` → `SnackbarKind` + PT copy in:
   - `MainWindow.RunTweakAsync`
   - `MainWindow.RunAutoOptimizeAsync`
3. Do **not** infer success from returned log lines alone.
4. Do **not** classify snackbar by log substring.
5. Do **not** change MutationExecutor timeout rethrow behavior in this task.

## Kind map

| Kind | SnackbarKind | Message hint |
|------|--------------|--------------|
| Completed | Success | completionStatus or "concluído" |
| PartiallyCompleted | Warning | parcialmente concluído |
| Failed | Error | falhou |
| RollbackRequired | Error | rollback necessário |
| Cancelled | Warning | cancelado |
| TimedOut | Error | tempo esgotado |
| RestartRequired | Warning | reinicie o PC |
| RolledBack | Warning | revertido |

## Verify

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -c Release -- --demo-self-test
```
