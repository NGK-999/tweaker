# Frontend Task — FE-ALL-P0 (FPS painel + maturidade corporativa)

**Task ID:** `FE-ALL-P0`  
**Executor:** Claude Code (`sonnet`, effort `high`)  
**Worktree:** `C:\projetos\Apextweaker-claude` branch `agent/claude-fps-p0`  
**Escopo:** só `src/UI/**` + `docs/coordination/frontend-handoff.md`  
**Refs:** `docs/research/frontend-maturity-inspirations.md`, `docs/research/fps-reference-filter.md`

## Já parcial neste branch

- `PerformanceView` + nav **Desempenho** existem — **completar** (wiring mock → eventos, build OK).
- Minecraft opção A (módulo) + theme Mac — **manter**.

## Parte A — Painel Desempenho / FPS

| Status | Fonte |
|--------|--------|
| VBS / Memory Integrity / HAGS / Game Mode / GameDVR / ReBAR | mock no VM se BE não mergeado; TODO wiring no handoff |

Ações Advanced com MessageBox de risco + eventos para orquestrador:
- Desativar VBS/HVCI (restart)
- Fullscreen Optimizations off (Valorant se detectado)
- Modo competitivo (overlays)

Copy: **nunca** prometer “+FPS”; falar stutter / 1% low / estabilidade.

## Parte B — Maturidade P0 (obrigatório)

1. **Home (Dashboard):** status resumido + **1 CTA principal** Auto-Optimize; secundários discretos.
2. **Catalog / listas de tweak:** estilo SettingsCard (row: título, descrição, badge risco, ação) — menos “grade de botões”.
3. **Badges tipados:** `Safe` / `Advanced` / `Restart` (não só cor; texto+glyph).
4. **Snackbar / toast corporativo:** “Aplicado.” / “Já aplicado (SKIP).” / “Reinício necessário.” — componente reutilizável no shell.
5. **Sidebar mais quieta** que o content (ajuste tokens/opacity se preciso; Linear-style).
6. **Ctrl+K** command palette leve: buscar páginas + tweaks conhecidos; Esc fecha.

## Parte C — Maturidade P1 (se tempo)

- Empty / error / “precisa Admin” states com copy séria.
- Progress por passos no Auto-Optimize (sem spinner infinito sem texto).

## Fora de escopo

- Não editar Services/Contracts/csproj.
- Não adicionar WPF-UI NuGet (P2; orquestrador decide).
- Sem RGB / glow / promo tiles.

## Verificação

```
dotnet build ApexTweaker.sln -c Release
```

## Handoff

Atualizar `docs/coordination/frontend-handoff.md` com arquivos, como testar, mocks, TODO wiring BE.
