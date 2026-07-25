# FE-SHELL-POLISH-P1 — prompt Claude

Você é o **executor de frontend** do ApexTweaker (WPF/.NET). Eleve polish do shell **depois** de UI-OUTCOME-P1.

## Contexto

- Repo worktree: `C:\projetos\Apextweaker-claude`
- Branch: `agent/claude-fe-outcome-polish` (base `origin/main` + commits de UI-OUTCOME se já mergeados nesta branch)
- Fluency P1 já entregue (opacity ~200ms, AnalyzeAsync, probe async).
- Impeccable product register: motion 150–250ms, estado semântico, sem motion decorativa.

## Objetivo

1. **Snackbar** (`src/UI/Wpf/Controls/Snackbar.cs`): coalesce / cancelar storyboard anterior; surface por kind (não só borda); usar `UiMotion.ConfigureStoryboard` se existir.
2. **Headers**: reduzir título duplicado shell (`HeaderTitleText`) vs `MacPageTitle` em Dashboard / Catalog / Performance; animar subtitle com o título.
3. **Busy**: chrome “Analisando…” no Catalog; loading no Performance enquanto probe async aplica.
4. **Motion**: elevate leve opacity-only (stagger subtitle/primeiro bloco). **Proibido** scale / BitmapCache em páginas. Sem `ClientAnimation`.

## Permitido

- `src/UI/Wpf/**`
- `docs/coordination/frontend-handoff.md`, `frontend-task.md`

## Proibido

- `src/Services/**`, Application, Windows, Contracts (exceto leitura)
- Mutar Windows / sair de Demo sem necessidade
- Merge em main / squash destrutivo
- Classificação Snackbar por substring
- Rewrite god-object MainWindow inteiro
- Quebrar mapping UI-OUTCOME-P1 (Kind → SnackbarKind)

## Método

1. `graphify query "Snackbar UiMotion CatalogView PerformanceView HeaderTitle"` antes de explorar em massa.
2. Implementar polish incremental.
3. `dotnet build ApexTweaker.sln -c Release`
4. Atualizar `docs/coordination/frontend-handoff.md` com PASS/FAIL e riscos.

## Resultado esperado

```text
PASS | FAIL
```
