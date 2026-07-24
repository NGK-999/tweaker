# Frontend Handoff — FE-ALL-P0 (+ integração orquestrador)

**Branch / worktree origem:** `agent/claude-fps-p0` (`Apextweaker-claude`) + merge orquestrador em `main`  
**Data:** 2026-07-24

## Resumo

Painel **Desempenho**, maturidade corporativa (Snackbar, RiskBadge, Ctrl+K, badges) e Minecraft opção A, integrados no `main` com Catalog + market utilities + APIs FPS do backend.

## Arquivos principais (UI)

| Arquivo | Mudança |
|---------|---------|
| `Controls/Snackbar.cs`, `Controls/RiskBadge.cs` | toast + badge tipado |
| `Views/PerformanceView.*` | status VBS/HVCI/HAGS/GameMode/ReBAR + ações Advanced |
| `MainWindow.xaml(.cs)` | nav Desempenho + Catalog; Snackbar shell; Ctrl+K; wiring BE FPS |
| `ModulesView.*` | rows + RiskBadge + Minecraft módulo + Mercado |
| `DashboardView.*` | CTA Auto-Optimize |
| `MacTheme.xaml` / `AppThemeManager` | tokens quietos |
| Animações | race fix L2 |

## Wiring BE (orquestrador)

- `DisableVbsHvci` → `WindowsOptimizationService.ApplyVbsMemoryIntegrityDisable(true)`
- Fullscreen → `ApplyGameFullscreenOptimizationsOff`
- Competitivo → `ApplyCompetitiveCaptureQuiet`
- Probe UI ainda lê registro localmente; upgrade futuro: `CaptureGamingPerformanceProbe()`

## Como testar

```
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -c Release -- --gaming-fps-probe-self-test
dotnet run --project ApexTweaker.csproj -c Release -- --demo
```

Manual: Ctrl+K, Desempenho (confirms), Catalogo, Módulos→Minecraft one-click, Snackbar após Auto-Optimize.

## Pendências leves

- Catalog SettingsCard visual mais próximo do Fluent (layout atual do CatalogView mantido)
- Performance status via probe tipado em vez de leitura registry duplicada
