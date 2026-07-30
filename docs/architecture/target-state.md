# Arquitetura-alvo — ApexTweaker

**Data:** 2026-07-25  
**Status:** proposta do Chief Product Engineer (Fase 1) — **não implementar nesta fase**

## Princípio

Manter monólito .NET + WPF. Separar FE/BE por **contratos C# + ownership + fachadas**, sem HTTP.

Prioridade: integridade → recuperação → confiabilidade → clareza.

## Visão estrutural

```text
UI (Claude)                    Contratos (Orquestrador)              Domínio (Codex)
───────────                    ────────────────────────              ────────────────
Views / ViewModels      →      IOperationEnvelope                    Inventory
Interaction states      →      OperationOutcome                      Recommendation
Design tokens           →      OperationProgress                     Mutation pipeline
                        →      ErrorDescriptor                       Backup / Rollback
                               CorrelationId                         Verify / Probe
                               WindowsOptimization*                  CommandRunner
```

UI **nunca** monta `reg`/`powercfg`/`bcdedit`.  
UI envia intenções tipadas; BE devolve plano + progresso + outcome.

## Máquina de estados oficial (operações longas)

Toda operação mutadora ou >500ms termina em **exatamente um** outcome:

| Outcome | Significado |
|---------|-------------|
| `COMPLETED` | tudo aplicado e verificado |
| `PARTIALLY_COMPLETED` | subset ok; falhas registradas |
| `FAILED` | nada útil concluído / falha fatal |
| `CANCELLED` | usuário ou timeout cooperativo |
| `ROLLBACK_REQUIRED` | estado inseguro; pedir rollback |
| `ROLLED_BACK` | restore concluído |
| `RESTART_REQUIRED` | mutação ok; reboot necessário |

Estados transitórios UI: `Idle → Validating → BackingUp → Applying → Verifying → Finalizing → <Outcome>`.

Persistir progresso + correlationId para recovery após crash/fechamento.

## Fronteiras de migração (sem big-bang)

| Milestone | Conteúdo | Não fazer |
|-----------|----------|-----------|
| **M0** | Docs Fase 1 + ownership + gates (esta entrega) | código de produto |
| **M1** | Demo gate + `OperationOutcome` no pipeline (BE) | redesenhar UI |
| **M2** | Feedback tipado no shell (FE consome envelope) | Apply catálogo completo |
| **M3** | Unificar jornada: Catálogo recomenda → bridge Auto | fundir PresetKind |
| **M4** | Apply seletivo do plano + verify tipado | LGPO massivo sem backup |
| **M5** | Desacoplar `ApplyPolicyAndServiceTweaks` do caminho padrão | apagar legado sem prova |
| **M6** | Extrair assemblies (opcional) | reescrita |

## Contratos a estabilizar (Orquestrador)

1. `OperationRequest` / `OperationEnvelope` / `OperationOutcome`
2. `ErrorDescriptor` (título, causa, impacto, applied[], skipped[], action, correlationId)
3. `WindowsOptimizationPlan` + futuro `ApplyPlanRequest` (só após M3)
4. Ponte documentada `PresetKind` ↔ `WindowsOptimizationPreset` (sem merge forçado)

## Sistema visual alvo (resumo — detalhe em `docs/ux/`)

- Minimalista, hierarquia forte, **uma ação principal** por tela de fluxo.
- Tokens já em `MacTheme.xaml` → evoluir para design system nomeado (sem copiar Apple).
- Progresso e erro como cidadãos de primeira classe (não só snackbar heurístico).
- Acessibilidade: AutomationProperties em Modules/Catalog/Utilities.

## Critérios de sucesso arquitetural

- Codex e Claude **não** tocam os mesmos arquivos.
- Dev pode validar fluxos com **zero mutação** (demo gate).
- God-object `MainWindow` para de crescer: novas features via fachada + VM.
- Self-tests + gates de qualidade além de “build passou”.
- Rollback e RESTART_REQUIRED sempre explícitos na UI.
