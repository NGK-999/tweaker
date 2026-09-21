# Backend handoff

Status: **ARCH-REFACTOR-P1 implementado; validação final depende do ambiente Windows/NuGet**

## ARCH-REFACTOR-P1 — execução Codex (2026-09-21)

### Entregas

- Criados contratos internos em `src/ApexTweaker.Contracts/Services/`:
  - `ITweakService`
  - `IBackupService`
  - `IMutationExecutor`
  - `IOptimizationEngine`
- `TweakService`, `BackupService`, `MutationExecutor` e `OptimizationEngine` passaram a implementar os contratos sem remover a superfície concreta usada pela UI.
- Criada a projeção tipada de `OperationOutcome` em `src/ApexTweaker.Contracts/OperationOutcome.cs`.
  - `IMutationExecutor.LastOutcome` e `ITweakService.LastOutcome` expõem o contrato compartilhado.
  - `TweakService.LastMutationOutcome` permanece com o tipo legado para compatibilidade; `LastContractOutcome` fornece a projeção nova.
  - `OperationOutcomeContractAdapter` mantém os dois modelos coerentes sem alterar o ledger ou os logs existentes.
- Criados `OptimizationHardwareSnapshot` e `OptimizationRecommendation` para a interface do motor, com adaptadores para `HardwareInfo`, `PresetKind` e `PresetRecommendation` legados.
- `SystemDiagnosticsService` deixou de chamar PowerShell para Secure Boot/TPM. O novo `SystemSecurityInventoryService` expõe `bool?` e usa Registro (`UEFISecureBootEnabled`) e WMI (`Win32_Tpm`) somente leitura; o caminho é seguro em Demo e também em Standard.
- Self-tests passaram a verificar implementação dos quatro contratos, propagação de outcome tipado e presença do inventário Secure Boot/TPM sem texto de PowerShell.
- Corrigido um erro de compilação preexistente em `WindowsOptimizationPurpose`: o catálogo já usava `Performance`, mas o enum não possuía esse membro.

### Ownership

- Nenhum arquivo em `src/UI/**` foi alterado.
- Nenhum arquivo em `src/Models/**` foi alterado.
- Alterações de contratos e documentação foram feitas pelo orquestrador deste chat; implementações ficaram em `src/Services/**`, `src/ApexTweaker.Windows/**` e self-tests de backend.
- Nenhuma mutação real do Windows foi executada.

### Verificações

| Comando / verificação | Exit code | Resultado |
|---|---:|---|
| `dotnet build src/ApexTweaker.Contracts/ApexTweaker.Contracts.csproj -c Release --no-restore` | `0` | Contracts compilou sem warnings/erros |
| `dotnet build src/ApexTweaker.Application/ApexTweaker.Application.csproj -c Release --no-restore` | `0` | Application compilou sem warnings/erros após corrigir `Performance` |
| `dotnet build ApexTweaker.sln -c Release` | `1` | Ambiente Linux não completou a restauração do grafo; o SDK local não possui os workloads/artefatos Windows necessários |
| `dotnet run --project ApexTweaker.csproj -c Release --no-build --no-restore -- --demo-self-test` | `1` | Executável Release não foi gerado porque o build completo ficou bloqueado |
| `dotnet run --project ApexTweaker.csproj -c Release --no-build --no-restore -- --gaming-fps-probe-self-test` | `1` | Mesmo bloqueio: executável Release ausente |
| Restore de `src/ApexTweaker.Windows/ApexTweaker.Windows.csproj` | `1` | `NU1101`: pacote `System.Management` indisponível no cache e acesso a `api.nuget.org` negado |
| `git diff --check` (excluindo espaços Markdown preexistentes em `backend-task.md`) | `0` | Diff de código sem erro de whitespace |
| `graphify update .` | `127` | CLI `graphify` não está instalada neste ambiente; `graphify-out/` inicial só tinha `GRAPH_REPORT.md`, que não foi editado manualmente |

### Observado vs inferido

- **Observado:** Contracts e Application compilam em Release; o catálogo referenciava `WindowsOptimizationPurpose.Performance` ausente; não há chamadas `RunPowerShellScalar`, `Confirm-SecureBootUEFI` ou `Get-Tpm` em `SystemDiagnosticsService.cs`.
- **Observado:** a tentativa de build completo falha antes da compilação do host por restauração/workload/pacote no ambiente Linux, e os dois `dotnet run --no-build --no-restore` não encontram o executável.
- **Inferido:** a implementação dos serviços alterados precisa de uma execução posterior em Windows com o cache NuGet e o toolchain nativo disponíveis para confirmar o build da solução e os self-tests obrigatórios.

### Pendência de integração

- Reexecutar os três comandos obrigatórios em um ambiente Windows preparado (Visual Studio/MSBuild nativo, Windows SDK e pacote `System.Management` restaurado). O código e os self-tests estão prontos para essa validação; não há pendência de ownership.

## Histórico — BE-DEMO-OUTCOME-P0 (2026-07-25)

As notas abaixo registram a entrega anterior e permanecem para rastreabilidade.

- RuntimeMode fail-closed introduzido no backend, com default explicito nao-demo em `Program.cs` e bloqueio se o modo estiver incerto.
- Gate central de mutacao aplicado em duas fronteiras:
  - `src/Services/MutationExecutor.cs`
  - `src/Infrastructure/CommandRunner.cs`
- `OperationOutcome` interno implementado em `src/Infrastructure/**` com:
  - `OutcomeKind`
  - `correlationId`
  - `messages`
  - timestamps
  - flags de runtime/cancelamento/timeout/restart/rollback/bloqueio
  - resultados por etapa (`OperationStepResult`)
- Novo self-test backend `--demo-self-test` cobrindo:
  - demo bloqueando mutacao
  - modo incerto fail-closed
  - caminho legado `TweakService` nao contornando o gate
  - outcomes distintos `COMPLETED`, `PARTIALLY_COMPLETED`, `CANCELLED`, `TIMEDOUT`, `ROLLBACK_REQUIRED`, `ROLLED_BACK`, `RESTART_REQUIRED`
- `graphify update .` executado ao final.

## Arquivos alterados

- `src/App/Program.cs`
- `src/App/DemoSafetySelfTest.cs`
- `src/Infrastructure/CommandRunner.cs`
- `src/Infrastructure/OperationOutcome.cs`
- `src/Infrastructure/RuntimeModeContext.cs`
- `src/Services/MutationExecutor.cs`
- `docs/coordination/backend-handoff.md`

## Diff resumido

- `Program.cs`
  - passa a configurar `RuntimeMode.Standard` por default
  - reconhece `--demo`
  - adiciona `--demo-self-test`
  - executa self-tests antes de qualquer migracao incidental
- `RuntimeModeContext`
  - contexto global simples para `Unknown` / `Standard` / `Demo`
  - politica fail-closed central para mutacoes
- `CommandRunner`
  - classificador conservador de comandos mutadores vs leitura
  - bloqueio automatico de comandos mutadores em `Demo` ou `Unknown`
- `OperationOutcome`
  - tipos internos novos sob `Infrastructure`, sem tocar `Models` ou `Contracts`
- `MutationExecutor`
  - gate antes de abrir sessao mutadora
  - captura `OperationOutcome` interno por execucao
  - rastreio de etapas `Validate`, `Snapshot`, `Execute`, `Verify`
  - distincao entre `Failed`, `PartiallyCompleted`, `Cancelled`, `TimedOut`, `RollbackRequired`, `RolledBack`, `RestartRequired`
  - resumo de outcome anexado ao log legado para o caminho atual via `TweakService`
- `DemoSafetySelfTest`
  - prova que `powercfg /list` segue permitido em demo
  - prova que mutacao direta no `CommandRunner` e caminho legado via `TweakService` sao bloqueados
  - valida os kinds de outcome sem tocar Registro, GPO, servicos, BCD ou power plan reais

## Contratos / Models

- Nenhum arquivo em `src/Models/**` foi alterado.
- Nenhum arquivo em `src/ApexTweaker.Contracts/**` foi alterado.
- `OperationOutcome` ficou interno ao backend, conforme o routing.

```text
Tipo interno criado: OperationOutcome (+ OperationOutcomeKind, OperationStepResult, OperationStepStatus)
Localização: src/Infrastructure/OperationOutcome.cs
Consumidores: MutationExecutor (pipeline); resumo em logs legados via TweakService
Pretende virar contrato compartilhado: SIM (fase futura, se UI consumir resultado tipado)
Mudança proposta: promover OperationOutcome/StepResult/Kind (+ ErrorDescriptor) para Contracts/Models sob aprovação do orquestrador — não feito nesta task
```

Dívida temporária aceitável: tipos públicos *internos ao assembly* em `Infrastructure`, não em `Services` como contrato de produto.

## Propostas de contrato para fase futura

- Se a UI for consumir o resultado tipado, propor ao orquestrador um contrato publico compartilhado para:
  - `OperationOutcome`
  - `OperationStepResult`
  - `OutcomeKind`
  - `ErrorDescriptor`
- Esta task nao faz essa promocao para `Models/Contracts`.

## Verificacoes executadas

| Comando | Exit code | Resultado |
|---|---:|---|
| `dotnet build ApexTweaker.sln -c Release` | `0` | build Release ok |
| `dotnet run --project ApexTweaker.csproj -c Release -- --demo-self-test` | `1` | falhou na sandbox por `NU1301` ao consultar assinaturas do NuGet |
| `dotnet run --project ApexTweaker.csproj -c Release --no-build --no-restore -- --demo-self-test` | `0` | `Demo safety self-test: ALL PASS` |
| `dotnet run --project ApexTweaker.csproj -c Release -- --gaming-fps-probe-self-test` | `1` | falhou na sandbox por `NU1301` ao consultar assinaturas do NuGet |
| `dotnet run --project ApexTweaker.csproj -c Release --no-build --no-restore -- --gaming-fps-probe-self-test` | `0` | `Gaming FPS probe self-test: ALL PASS` |
| `graphify update .` | `0` | AST graph atualizado com `graph.json`, `graph.html` e `GRAPH_REPORT.md` regenerados |

## Observado vs inferido

- Observado:
  - `MutationExecutor` era a fronteira central do pipeline mutador usado por `TweakService`.
  - `CommandRunner` executava comandos sem contexto de runtime.
  - O caminho legado de `TweakService` continuou passando pelo gate novo sem edicao em UI.
  - Os self-tests novos e o self-test legado de gaming passaram quando executados com `--no-build --no-restore`.
- Inferido:
  - A falha dos `dotnet run` exatos decorre do ambiente/sandbox consultando metadata de assinatura do NuGet, nao de regressao funcional do app, porque o build Release e as execucoes `--no-build --no-restore` passaram na mesma revisao.

## Pendencias

- Nenhuma pendencia obrigatoria dentro do escopo aprovado.
- Se o orquestrador exigir consumo de outcome pela UI, isso depende de uma task separada de contrato + FE.

## Riscos / limites conhecidos

- O classificador de mutacao do `CommandRunner` e deliberadamente conservador. Isso reduz risco de bypass, mas novos comandos mutadores/read-only fora dos padroes atuais podem exigir ajuste futuro.
- O outcome tipado ainda nao e consumido pela UI; hoje ele fica disponivel no backend e resumido nas mensagens legadas.
- `ROLLED_BACK` hoje depende de marcacao explicita do executor; o fluxo de restore legado ainda nao foi migrado para produzir esse outcome automaticamente.
