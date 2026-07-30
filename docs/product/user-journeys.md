# Jornadas de usuário — ApexTweaker

**Data:** 2026-07-25  
**Fase:** 1  
**Legenda:** observado | gap | alvo

## Mapa de telas (shell)

Disclaimer → Loading → `MainWindow`  
Sidebar: Dashboard · Desempenho · Módulos · Catálogo · Telemetria · Utilidades (+ Minecraft)

## Jornada A — Otimizar Windows (núcleo)

```text
Detectar → Analisar → Recomendar → Confirmar → Backup → Aplicar → Verificar → [Reiniciar?] → Medir → Rollback?
```

| Etapa | Como é hoje | Gap |
|-------|-------------|-----|
| Detectar | `SystemDiagnosticsService` / inventário no Analyze | ok parcial |
| Analisar | Catálogo `Analyze()`; Auto chama `OptimizationEngine` | **dois motores** |
| Recomendar | Catálogo lista decisões; Dashboard Auto usa `PresetKind` | vocabulário divergente |
| Confirmar | MessageBox em VBS/FSO/Competitivo; Módulos quase direto | inconsistente |
| Backup | `CreateBackup` / sessão MutationExecutor | nem sempre visível na UI |
| Aplicar | Auto/Módulos → `TweakService`; Catálogo **não aplica** | **quebra da jornada** |
| Verificar | Desempenho refresca probe; Auto diz “concluído” | verify tipado ausente |
| Reinício | snackbar “Reinicie…” | sem estado RESTART_REQUIRED tipado |
| Rollback | Utilidades | ok, mas longe do resultado |

### Persona feliz (alvo)

1. Abre Dashboard → vê preset recomendado em linguagem humana.  
2. Confirma riscos Advanced/Dangerous.  
3. Vê progresso por etapa (backup → apply → verify).  
4. Resultado: o que mudou / o que falhou / reboot? / correlationId.  
5. Um botão “Desfazer última operação”.

## Jornada B — Desempenho pontual (estabilidade)

Entrada: página Desempenho.  
Probe: `CaptureGamingPerformanceProbe()`.  
Ações: VBS/HVCI (confirm), FSO por exe, quiet competitivo.

**Observado:** wiring OK após auditoria PERF.  
**Gap:** não substitui jornada A; riscos High ainda precisam de copy de impacto (anti-cheat, WSL).

## Jornada C — Minecraft rápido

`Encontrar → Preparar → Testar → Resolver/Restaurar` (README).  
Mais madura em feedback do que o núcleo Windows legado.

## Jornada D — Recuperação após falha / crash

| Cenário | Hoje | Gap |
|---------|------|-----|
| Crash UI | `UnhandledException` → MessageBox fatal | sem export diagnóstico automático |
| Fechar durante ETW | hide + teardown 2s + force exit | melhorou; segundo close força |
| Mutação interrompida | ledger MutationSession | UI não resume “operação incompleta” |
| Session hook MC | `WriteRecoveryState` / `RecoverPending` | modelo a espelhar no Windows |

## Jornada E — Atualização / instalação / desinstalação

Scripts em `scripts/` (Build-Release, Installer, Test-Release).  
Desinstalar: Utilidades.  
**Gap:** sem CI GitHub Actions; sem canal de update tipado na UI; sem jornada “atualização segura” documentada como máquina de estados.

## Estados de interface desejados (todas as jornadas críticas)

| Estado | Obrigatório |
|--------|-------------|
| Idle | sim |
| Loading (<500ms pode omitir chrome) | sim se ≥500ms |
| Partial | sim (lista incompleta) |
| Empty | sim |
| Error (açãoivel) | sim |
| Success + next step | sim |
| Restart required | sim |
| Rollback available | sim |
