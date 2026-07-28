# Frontend handoff — FE-DISTILL-MINIMAL

**Status:** PASS  
**Branch:** `integration/fe-distill-minimal`  
**Data:** 2026-07-28  

## O que mudou

1. **Tokens** (`MacTheme.xaml` + `AppThemeManager`) — bordas mais sutis, section labels sentence-case, nav active bar fina, motion 120–180 ms, cards padding 16 / radius 8.
2. **Shell** (`MainWindow.xaml`) — uma marca (sidebar); title bar só chrome; sidebar 188px; sem eyebrows INICIO/AJUSTES/SISTEMA; status flat; removido CachingHint na PageHost.
3. **Dashboard** — 1 CTA + hardware/segurança em layout flat (sem hero card aninhado).
4. **Módulos** — listas densas; Mercado densificado; CTT em Expander colapsado; `SetBusy` cobre todos os botões (incl. CTT).
5. **Catalogo / Performance** — menos nesting; banners busy discretos; rows flat; status cards radius 8.
6. **Motion** — `UiMotion.Standard` 180 ms; `PageTransitionAnimator` 180 ms opacity-only.
7. Docs: `docs/DESIGN.md`, `docs/coordination/prompts/FE-DISTILL-MINIMAL-claude.md`.

## Fora de escopo (ok)

- Minecraft wizard / Telemetria / Utilidades (só herança de tokens)
- WPF-UI NuGet, contratos BE, Demo gate

## Como testar

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -c Release -- --demo
dotnet run --project ApexTweaker.csproj -c Release -- --demo-self-test
dotnet run --project ApexTweaker.csproj -c Release -- --catalog-feedback-self-test
```

Smoke: 7 abas, Catalog Analyze busy, Auto-Tuning copy, snackbar kinds.

## Riscos

- Labels CAPS em Minecraft/Telemetria ainda existem no markup (herdam estilo mais quieto).
- Expander CTT usa chrome nativo WPF (aceitável neste lote).

## Resultado

```text
PASS
```
