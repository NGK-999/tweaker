# Prompt Claude Code — FE-FEEDBACK-SHELL-P0

Você é o executor **frontend** no worktree `C:\projetos\Apextweaker-claude`, branch `agent/claude-fe-feedback-shell-p0`.

## Leia primeiro

- `docs/coordination/prompts/FE-FEEDBACK-SHELL-P0-routing.md`
- `docs/ux/design-system-proposal.md`
- `docs/ux/interaction-states.md`
- `docs/ux/error-messages.md`
- `docs/product/user-journeys.md`
- `docs/coordination/ownership.md`

## Graphify (obrigatório)

```powershell
cd C:\projetos\Apextweaker-claude
graphify query "CatalogView CatalogViewModel Snackbar MainWindow SetStatus"
graphify explain "CatalogViewModel"
```

## Condições obrigatórias (aprovadas)

1. `SnackbarKind.Error` **explícito**; **remover** classificação por substring (`ClassifySnackbarKind`).
2. **Empty**, **Partial** e **Error** = estados **diferentes** (enum UI local, ex. `CatalogFeedbackState`).
3. CTA Auto = **navegação** (“Ir ao Dashboard para usar Auto-Optimize”), **não** fingir apply imediato.
4. **Não** criar contrato definitivo de `OperationOutcome` no FE (BE ainda estabilizando).
5. Testes: empty, falha, partial, Snackbar Error, foco/`AutomationProperties` → `CatalogFeedbackSelfTest.Run()`.
6. **Não** redesign amplo.

## Contratos / arquivos congelados

- Não tocar `Program.cs`, Services, Infrastructure, Models, Contracts.
- Wire do self-test no `Program` = **proposta** no handoff.
- `MainWindow.xaml.cs`: só overload `SetStatus(string, SnackbarKind)`, remoção do classifier; sem refatoração ampla.

## Entrega

1. Build Release  
2. Self-test de estados (invocável; documentar como rodar sem Program se preciso)  
3. `docs/coordination/frontend-handoff.md` com diff, testes, pendências  
4. `graphify update .`  
5. Commit na branch do agent com prefixo `FE-FEEDBACK-SHELL-P0:` se o fluxo local exigir; integração no main é do orquestrador.

PARE ao cumprir acceptance do routing.
