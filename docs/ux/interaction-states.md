# Interaction states — ApexTweaker

**Data:** 2026-07-25

| Estado | UI | Notas |
|--------|----|-------|
| Idle | CTA habilitado | |
| Busy/Tweaking | SetBusy nas views | hoje `isTweaking` global |
| Loading content | skeleton/spinner local | Catalog Analyze |
| Empty | mensagem + retry/CTA | **gap Catalog** |
| Partial | lista + aviso | usage Unknown |
| Error | Error snackbar + painel | **gap kind Error** |
| Success | Success + próximo passo | reboot? |
| RestartRequired | Warning persistente | não sumir em 3s |
| RollbackAvailable | CTA secundário | |

Operações longas devem espelhar a máquina de estados de `docs/architecture/target-state.md`.
