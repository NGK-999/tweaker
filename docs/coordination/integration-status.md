# Integration status

Data: 2026-07-24  
Fase: **MARKET B1–B9 implementado (modo A)**

## Entregue nesta rodada

- `docs/research/market-coverage-matrix.md`
- `MarketUtilitiesService` (clean/TRIM/SFC dry-run/Storage Sense/bufferbloat guide)
- `TweakService`: UI noise, Memory, Rede avancada, Debloat condicional
- Catalogo expandido + dangerous extras (Edge, SmartScreen)
- `BiosChecklistCatalog`
- UI: `CatalogView` + nav Catalogo; Utilidades market; Modulos market
- Self-test: `--market-coverage-self-test`

## Verificacao

```
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -c Release -- --market-coverage-self-test
```

## Pendencias conhecidas

- Redesign visual Hermes/Cursor (Claude L2-SHELL-2) ainda paralelo no worktree claude
- Minecraft opcao A (one-click) esta no worktree claude; main ainda tem Minecraft na sidebar
- LGPO apply real ainda nao
- Inventario de uso na UI do Debloat ainda usa `WindowsUsageProfile.Unknown` (conservador)

## Commits

Nenhum commit automatico (aguardar pedido do usuario).
