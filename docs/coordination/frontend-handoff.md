# Frontend Handoff — FE-ALL-P0 (+ integração orquestrador)

**Branch / worktree origem:** `agent/claude-fps-p0` + merge orquestrador em `main`  
**Atualizado:** 2026-07-25

## Resumo

Painel **Desempenho**, maturidade corporativa (Snackbar, RiskBadge, Ctrl+K, badges) e Minecraft opção A, com Catalog + market + APIs FPS.

## Wiring BE

- `DisableVbsHvci` → `ApplyVbsMemoryIntegrityDisable(true)`
- Fullscreen → `ApplyGameFullscreenOptimizationsOff`
- Competitivo → `ApplyCompetitiveCaptureQuiet`
- Status Desempenho → `CaptureGamingPerformanceProbe()` (refresh ao abrir pagina e apos acoes)

## Como testar

```
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -c Release -- --gaming-fps-probe-self-test
dotnet run --project ApexTweaker.csproj -c Release -- --demo
```

## Fechar janela

Close esconde imediatamente, teardown com timeout; segundo clique força `Environment.Exit` se travar.

## Pendencias restantes (fora deste sprint)

- WPF-UI NuGet incremental (P2 opcional)
- Kernel drivers: **nao**
