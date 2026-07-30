# Prompt Codex — BE-DEMO-OUTCOME-P0

Você é o executor **backend** no worktree `C:\projetos\Apextweaker-codex`, branch `agent/codex-be-demo-outcome-p0`.

## Leia primeiro

- `docs/coordination/prompts/BE-DEMO-OUTCOME-P0-routing.md` (fonte de verdade)
- `docs/architecture/target-state.md`
- `docs/reliability/error-taxonomy.md`
- `docs/reliability/recovery-strategy.md`
- `docs/coordination/ownership.md`
- `docs/quality/quality-gates.md`

## Graphify (obrigatório)

```powershell
cd C:\projetos\Apextweaker-codex
graphify query "MutationExecutor CommandRunner TweakService BackupService mutacao"
graphify explain "MutationExecutor"
graphify path "TweakService" "CommandRunner"
```

## Condições obrigatórias (aprovadas)

1. **RuntimeMode fail-closed** — default não-demo; se modo incerto, **bloquear mutação**.
2. Gate na **fronteira central**: `CommandRunner` + `MutationExecutor` (não só UI).
3. Demo **permite** inventário, análise, plano e simulação (dry-run / leituras).
4. `OperationOutcome` rico: `correlationId`, mensagens, timestamps, flags, **resultados por etapa**.
5. Estados distintos: cancelamento, timeout, falha parcial, rollback, reinicialização (`CANCELLED`, timeout explícito, `PARTIALLY_COMPLETED`, `ROLLBACK_REQUIRED`/`ROLLED_BACK`, `RESTART_REQUIRED`, `COMPLETED`, `FAILED`).
6. **Nenhum teste** altera Registro, GPO, serviços, BCD ou plano de energia real.
7. Testar caminhos **legados** (`TweakService` → runner/executor) para provar que **não contornam** o gate.
8. **Não** refatorar `MainWindow`.

## Contratos congelados

Não edite `src/Models/**` nem `src/ApexTweaker.Contracts/**`.  
Tipos de outcome sob `src/Infrastructure/**` (preferencial) ou `src/Services/Operation*.cs`.  
Se precisar de contrato público compartilhado com UI: **proposta no handoff**, não alteração unilateral.

## Escopo de arquivos

Strict allowed/forbidden no routing. `Program.cs`: só `--demo` e `--demo-self-test`.

## Entrega

1. Implementar + build Release  
2. `--demo-self-test` PASS  
3. Regressão `--gaming-fps-probe-self-test`  
4. `docs/coordination/backend-handoff.md` com: diff resumido, arquivos, testes, pendências, propostas de contrato  
5. `graphify update .`  
6. Commit **apenas** se o orquestrador pedir — nesta task, deixe pronto para commit; o orquestrador fará commits independentes na integração. Pode commitar na branch do agent se facilitar o handoff (mensagem clara `BE-DEMO-OUTCOME-P0:`).

## Fora de escopo

UI, Apply de plano do Catálogo, redesign, correlationId na UI.

PARE ao cumprir acceptance do routing.
