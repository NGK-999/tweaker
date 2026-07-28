# FE-DISTILL-MINIMAL — briefing Claude FE

**Branch:** `integration/fe-distill-minimal` (ou `agent/claude-fe-distill-minimal`)  
**Escopo:** apenas `src/UI/Wpf/**` + docs de coordination  
**Proibido:** `src/Services/**`, WPF-UI NuGet, redesign Minecraft wizard, mudar contratos BE / Demo gate / outcome mapping

## Direção

Product tool minimalista (densidade Linear/Raycast): dark cool-neutral existente, accent teal ≤10%, tipografia Segoe UI Variable + mono só para dados.

- Sem cream / purple / glow / glass decorativo
- Sem cards aninhados
- Motion: opacity-only 150–200 ms, ease-out; sem scale/`BitmapCache` em páginas

## Lote deste PR

| Área | Arquivo | Ação |
|------|---------|------|
| Tokens | `Themes/MacTheme.xaml` | cards flat/bordas sutis; section labels sentence case; nav active discreto; radius ≤10 |
| Shell | `MainWindow.xaml` | uma marca; sidebar ~180–200; sem eyebrows INICIO/AJUSTES; header limpo |
| Dashboard | `DashboardView.xaml` | 1 CTA + hardware; sem hero ruidoso |
| Módulos | `ModulesView.xaml` | listas densas nome\|desc\|risk; Mercado densificado; CTT colapsável |
| Catalogo | `CatalogView.xaml` | Empty/Partial/Error intactos; menos nesting; busy discreto |
| Performance | `PerformanceView.xaml` | status flat; banner alinhado |
| Motion | `UiMotion.cs` / `PageTransitionAnimator.cs` | 150–200 ms opacity |

Minecraft / Telemetria / Utilidades: só herdam tokens (sem redesign de fluxo).

## Verify

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -c Release -- --demo
dotnet run --project ApexTweaker.csproj -c Release -- --demo-self-test
dotnet run --project ApexTweaker.csproj -c Release -- --catalog-feedback-self-test
```

Smoke: 7 abas, Catalog Analyze busy, Auto-Tuning copy, snackbar kinds.

## Entregável

`frontend-handoff.md` PASS/FAIL + riscos; atualizar `master-plan.md` com FE-DISTILL-MINIMAL.
