# ROUTING DECISION — BE-DEMO-OUTCOME-P0 (aprovado 2026-07-25)

```
Task ID: BE-DEMO-OUTCOME-P0
Goal: RuntimeMode fail-closed + gate na fronteira central de mutação + OperationOutcome rico (correlationId, mensagens, timestamps, flags, resultados por etapa) + self-tests sem mutar SO real; provar que legado não contorna o gate.
Executor: Codex
Model: gpt-5.4
Effort: high
Skills: graphify (obrigatório antes de explorar), ponytail (diff mínimo)
Worktree: C:\projetos\Apextweaker-codex
Branch: agent/codex-be-demo-outcome-p0

Allowed files:
  - src/Infrastructure/**          (RuntimeMode, OperationOutcome types, CommandRunner gate)
  - src/Services/MutationExecutor.cs
  - src/Services/Operation*.cs     (somente tipos/helpers de outcome se não couberem em Infrastructure)
  - src/App/DemoSafetySelfTest.cs  (novo) e/ou src/App/*Demo*SelfTest*.cs
  - src/App/Program.cs             (APENAS flags --demo / --demo-self-test; diff mínimo)
  - docs/coordination/backend-handoff.md
  - docs/coordination/backend-task.md

Forbidden files (congelados / outro agente):
  - src/UI/**
  - src/Models/**
  - src/ApexTweaker.Contracts/**
  - src/ApexTweaker.Application/** (salvo se Orquestrador autorizar — default NÃO)
  - native/**, *.sln, ApexTweaker.csproj
  - docs/product/**, docs/ux/**, docs/architecture/**
  - src/UI/Wpf/MainWindow.xaml.cs
  - docs/coordination/frontend-*

Contratos: CONGELADOS. Qualquer tipo público novo em Models/Contracts = PROPOSTA no handoff, não merge unilateral.
OperationOutcome: implementar sob Infrastructure (ou Services/Operation*.cs) nesta task.

Acceptance criteria:
  1. RuntimeMode fail-closed (default seguro; demo explícito; mutação bloqueada se modo incerto)
  2. Gate em CommandRunner + MutationExecutor (não só UI)
  3. Demo permite inventário, Analyze, plano e simulação (leituras / dry-run)
  4. OperationOutcome com: OutcomeKind, correlationId, messages, timestamps, flags, step results
  5. CANCELLED, TIMEOUT (ou flag TimedOut com kind distinto), PARTIALLY_COMPLETED, ROLLBACK_*, RESTART_REQUIRED distintos
  6. Self-tests NÃO alteram Registro, GPO, serviços, BCD, power plan reais
  7. Testes cobrem caminho legado (TweakService/CommandRunner) não contorna gate
  8. Sem refatorar MainWindow
  9. Build Release + handoff com diff, testes, pendências, propostas de contrato

Verification commands:
  - dotnet build ApexTweaker.sln -c Release
  - dotnet run --project ApexTweaker.csproj -c Release -- --demo-self-test
  - dotnet run --project ApexTweaker.csproj -c Release -- --gaming-fps-probe-self-test

Fallback timeout:
  - Entregar RuntimeMode + gate CommandRunner + self-test mínimo
  - OperationOutcome parcial com TODO no handoff
  - Não tocar UI; não expandir escopo

Reviewer: Orquestrador (depois Claude cross-check UX de mensagens se expostas)
```
