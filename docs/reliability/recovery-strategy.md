# Estratégia de recuperação — ApexTweaker

**Data:** 2026-07-25  
**Fase:** 1

## Objetivos

1. Sempre poder **iniciar o Windows**.  
2. Sempre poder **desfazer** a última sessão de mutação quando houver snapshot.  
3. Nunca deixar operação longa sem estado terminal.  
4. Após crash/fechamento, informar operação incompleta.

## Camadas de recuperação

```text
1. Evitar dano     → validação, risco, demo gate, confirm Dangerous
2. Limitar dano    → snapshot antes de execute (MutationExecutor)
3. Detectar dano   → Verify por comando
4. Reverter dano   → MasterRollback / RestoreLatestMutationSession
5. Recuperar app   → resume UI + correlationId + export diagnóstico
6. Recuperar SO    → restore point Windows + instruções manuais
```

## Snapshot / backup

| Mecanismo | Uso | Status |
|-----------|-----|--------|
| `BackupService.BeginMutationSession` | ledger por operação | observado |
| `CreateBackup()` | backup granular pré-Auto | observado |
| Restore point Windows | Dashboard | observado |
| Minecraft profile/quarantine backup | MC | observado |
| Session hook recovery file | MC hooks | observado — **modelo a copiar** |

**Regra:** se snapshot falhar → **não executar** mutação (`IO` / `ROLLBACK` preventivo).

## Rollback

- Entrada UX: Utilidades (hoje) → alvo: também no painel de resultado da operação.
- Escopo: última sessão vs master rollback (documentar diferença na UI).
- Outcome: `ROLLED_BACK` ou `FAILED` com detalhes.

## Reinício

- Detectar reboot pendente (VBS/HVCI, BCD, etc.).
- Outcome `RESTART_REQUIRED` + CTA “Reiniciar depois” / checklist pós-boot.
- Nunca forçar reboot sem confirmação.

## Fechamento / hang

Já mitigado parcialmente (`MainWindow_OnClosing`: hide, teardown 2s, segundo close force exit).  
Ainda necessário: garantir que **mutação em andamento** marque `PARTIALLY_COMPLETED` / `ROLLBACK_REQUIRED` no ledger antes do kill.

## Safe mode (alvo)

Flag CLI `--safe-ui` / setting:

- sem ETW automático;
- sem hooks Minecraft;
- só inventário + rollback + export.

## Diagnóstico exportável (alvo)

ZIP com: correlationIds, ledger da sessão, log UI, probe, inventário, versão app, build Windows.  
Minecraft já tem ZIP rico — espelhar para Windows ops.

## Recovery após falha — checklist operacional

1. Ler outcome + correlationId.  
2. Se `ROLLBACK_REQUIRED` → oferecer rollback imediato.  
3. Se `RESTART_REQUIRED` → não medir FPS antes do reboot.  
4. Se backup ausente → **não** tentar “reparar” com mais tweaks; guiar restore point / Support.
