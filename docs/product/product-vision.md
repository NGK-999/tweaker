# Visão de produto — ApexTweaker

**Data:** 2026-07-25  
**Fase:** 1 (auditoria somente leitura)  
**Confiança:** observado no código + README + contratos; inferências marcadas.

## O que é

Utilitário **Windows desktop** (.NET 10 + WPF) que:

1. coleta inventário de hardware e estado do Windows;
2. analisa e recomenda presets de otimização (estabilidade / frametime, não “+FPS mágico”);
3. cria backup / ponto de restauração quando aplicável;
4. aplica alterações rastreáveis;
5. permite verificar, reiniciar com consciência e fazer rollback;
6. inclui módulo Minecraft/Cobblemon paralelo (preparação segura em hardware limitado).

Não é SaaS, não é painel web, não há API HTTP. Host único in-process.

## Para quem

| Persona | Necessidade | Risco |
|---------|-------------|-------|
| Gamer competitivo (Valorant etc.) | menos ruído do SO, latência previsível, rollback fácil | anti-cheat, VBS, overlays |
| Gamer casual / notebook | preset conservador, baterias, não quebrar Update/Defender | over-tweak |
| Minecraft low-end | perfil leve, backup de configs, diagnóstico exportável | mods server-side |
| Power user | Catálogo + checklist BIOS (sem flash), telemetria A/B | complexidade cognitiva |

## Promessa de produto

> O ApexTweaker deixa o PC **mais previsível para jogar**, com **recuperação sempre possível**, riscos **explícitos** e feedback **nunca silencioso**.

Não promete ganho fixo de FPS. Desempenho (página) já reforça isso.

## Princípios de prioridade (obrigatórios)

1. integridade do SO  
2. recuperação  
3. confiabilidade  
4. clareza  
5. compatibilidade  
6. segurança  
7. desempenho  
8. aparência  

Nenhuma otimização pode comprometer boot, recovery ou Windows Update sem classificação de risco explícita.

## Proposta de valor vs estado atual

| Capacidade | Alvo | Estado (2026-07-25 @ `8706126` + WIP local) |
|------------|------|-----------------------------------------------|
| Inventário + Analyze | sólido | Catálogo + probe Desempenho **ok** |
| Recomendação acionável | um caminho | **partido:** Catálogo analisa; Auto/Módulos aplicam legado |
| Backup antes de mutar | obrigatório | MutationExecutor + BackupService **existem** |
| Apply tipado (plano→resultado) | oficial | **ausente** no contrato novo |
| Verify pós-apply | tipado | fraco no Auto/Módulos (log + snackbar) |
| Rollback óbvio | 1 clique | Utilidades / MasterRollback **existe** |
| Demo sem mutar máquina | obrigatório em dev | **ausente** no HEAD (`RuntimeMode`/`--demo` não encontrados) |
| Estados de operação oficiais | COMPLETED… | **ausentes** (só flags UI `isTweaking`) |

## Não-objetivos (agora)

- reescrita Electron/React;
- API REST só para “separar FE/BE”;
- driver/kernel;
- flash de BIOS;
- desligar Defender/Update/SmartScreen em Auto;
- redesign visual completo de todas as telas.

## Critérios de sucesso de produto (norte)

- Usuário completa analyze → confirmar → aplicar → ver resultado → rollback sem abrir o log bruto.
- Toda mutação termina em estado explícito com correlationId.
- Dev/QA rodam fluxos estruturais em modo demo (zero mutação).
- UI mostra no máximo **uma ação principal** por fluxo crítico.
