# Master plan — ApexTweaker (produto corporativo)

**Atualizado:** 2026-07-25  
**Fase ativa:** **1 concluída (docs)** → aguardando revisão humana antes de implementar  
**HEAD base:** `8706126` (+ WIP local auditoria PERF não commitado)

## Norte

Transformar o ApexTweaker em produto **sólido, confiável, observável, minimalista**, sem reescrita.

Ordem de prioridade: integridade → recuperação → confiabilidade → clareza → compatibilidade → segurança → desempenho → aparência.

## Achados Fase 1 (resumo)

### Top 5 causas prováveis de travamento

1. Teardown ETW/telemetria no close (mitigado, ainda sensível).  
2. `CommandRunner` / processos externos longos se caller ignora cancel.  
3. Mutex UI `isTweaking` sem timeout de operação → sensação de app preso.  
4. God-object `MainWindow` + telemetria/ETW concorrente.  
5. Fechar durante mutação sem outcome persistido → estado fantasma.

### Top 5 problemas de feedback

1. Jornada Catálogo analisa / Auto aplica (usuário “preso” após Analyze).  
2. Snackbar sem Error + classificação por substring.  
3. Pós-apply sem verify tipado (“veja o log”).  
4. Confirmação de risco inconsistente (Performance vs Modules).  
5. Operações >500ms sem progresso por etapa (backup/apply/verify).

### Top 5 problemas arquiteturais

1. Duas gerações de otimização sem ponte (`PresetKind` vs `WindowsOptimization*`).  
2. Apply de plano ausente no contrato novo.  
3. Sem `OperationOutcome` / correlationId.  
4. Demo gate documentado historicamente mas **ausente** no código atual.  
5. God files + sem CI + sem pirâmide de testes.

## Backlog priorizado

| ID | Fase | Título | Owner | Status |
|----|------|--------|-------|--------|
| DOC-P1 | 1 | Auditoria + docs canônicos | Orquestrador | **DONE** |
| **BE-DEMO-OUTCOME-P0** | 3 | Demo gate + `OperationOutcome` no MutationExecutor | Codex | **NEXT** |
| **FE-FEEDBACK-SHELL-P0** | 4/5 | Empty/error Catalog + snackbar Error + progresso mínimo | Claude | **NEXT** |
| BE-CORR-LOG-P1 | 3 | correlationId + ErrorDescriptor nas mutações | Codex | queued |
| FE-OP-PANEL-P1 | 5 | Painel de resultado (applied/failed/reboot) | Claude | queued |
| BE-VERIFY-P1 | 2/3 | Verify tipado pós-Auto | Codex | queued |
| FE-JOURNEY-BRIDGE-P1 | 4 | Bridge Catálogo → Auto com copy clara | Claude | queued |
| BE-APPLY-PLAN-P2 | 2/3 | Apply seletivo do plano (contrato novo) | Codex | later |
| FE-FLOW-RESTYLE-P2 | 4 | Fluxo analyze→result sem redesign total | Claude | later |
| INFRA-CI-P2 | 6/8 | CI build + self-tests | Orquestrador | later |
| LEGACY-POLICY-P3 | 3 | Desacoplar ApplyPolicyAndServiceTweaks | Codex | later |

## Entregas recentes (histórico curto)

| Item | Status |
|------|--------|
| FPS probe + Desempenho | DONE |
| Catalog rows + sidebar | DONE |
| Close hang fix | DONE |
| AUDIT-PERF AppCompat merge | DONE local (commit pendente) |

## Próximo passo após revisão humana

1. Aprovar docs Fase 1.  
2. Disparar **somente** `BE-DEMO-OUTCOME-P0` e `FE-FEEDBACK-SHELL-P0` (prompts em `docs/coordination/prompts/`).  
3. Orquestrador integra com quality gates.  
4. Só então Fase 2 detalhada por fluxo (se necessário expandir).

## Não fazer agora

- redesign completo;
- Apply LGPO massivo;
- fundir presets numa tacada;
- alterar FE e BE nos mesmos arquivos.
