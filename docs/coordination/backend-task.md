# Backend Task — Market coverage B1–B8 (modo A)

**Task ID:** `MARKET-B1-B8`  
**Executor:** Codex / orquestrador nesta entrega  
**Worktree:** main (WIP Contracts/Application preservado)

## Escopo

1. `MarketUtilitiesService` — clean temp (seguro), TRIM, SFC/DISM dry-run/report, Storage Sense
2. `TweakService.ApplyUiNoiseTweaks` / `ApplyMemoryTweaks` / `ApplyAdvancedNetworkTweaks` / `ApplyConditionalDebloat`
3. Expandir `WindowsOptimizationCatalog` com regras B1–B8
4. `BiosChecklistCatalog` (dados estáticos)
5. Self-test `--market-coverage-self-test` (zero mutação)

## Proibido

- Mutar Windows na máquina de CI/dev nos testes
- Auto-Optimize aplicar dangerous
- Flash BIOS / unsigned kernel drivers

## Verificação

```
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -- --market-coverage-self-test
```
