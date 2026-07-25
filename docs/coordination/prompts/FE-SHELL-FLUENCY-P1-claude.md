# FE-SHELL-FLUENCY-P1 — prompt Claude (poder total)

Você é o **executor de frontend** do ApexTweaker (WPF/.NET). Use o máximo de qualidade visual e de fluidez.

## Contexto

- Repo: `C:\projetos\Apextweaker-claude`
- Branch: `agent/claude-fe-shell-fluency-p1` (base `main` pós PR #2)
- Orquestrador já deixou um **esqueleto mecânico** de fluency (opacity ~200ms, AnalyzeAsync, probe async). Seu trabalho: **elevar** isso — motion premium, sem travar, sem redesign de produto.

## Objetivo

A shell ainda “parece travada” e as animações “não estão boas”. Entregue fluidez perceptível:

1. Transições de página e header **curtas, limpas, fluidas** (ease-out; sem bounce/elastic; sem BitmapCache em páginas inteiras).
2. **Zero trabalho pesado no UI thread** em Loaded/navegação (Catalog Analyze, probe Desempenho, e qualquer outro hot path que você achar no shell).
3. Feedback imediato (“Analisando…”, busy states) sem congelar chrome/nav.
4. Respeitar tema existente (`MacTheme` / AppThemeManager) — polish, não rebrand.
5. Preferir reduced-motion / skip quando `skipAnimation` já existir.

## Permitido

- `src/UI/Wpf/**` (Animations, Views, ViewModels, Controls, Themes, MainWindow.xaml(.cs) **só shell/navegação/motion**)
- `docs/coordination/frontend-handoff.md` + `frontend-task.md`

## Proibido

- `src/Services/**`, Application, Windows, Contracts (exceto leitura)
- Mutar Windows / sair de Demo sem necessidade
- Squash destrutivo / merge em main
- Aplicar stash `wip-pre-p0-integration-*`
- Restaurar classificação Snackbar por substring
- Rewrite god-object MainWindow inteiro

## Método

1. `graphify query "PageTransitionAnimator UiMotion CatalogView ShowPageAsync"` antes de explorar em massa.
2. Leia o esqueleto atual em Animations + Catalog + ShowPageAsync.
3. Melhore motion (timing, easing, stagger mínimo se ajudar, cancelamento de transição).
4. Caçe outros freezes no shell (startup, snackbar, nav rapid-fire).
5. Rode:
   ```powershell
   dotnet build ApexTweaker.sln -c Release
   dotnet run --project ApexTweaker.csproj -c Release --no-build -- --catalog-feedback-self-test
   ```
6. Smoke mental: Dashboard → Catálogo → Desempenho → Dashboard rápido.
7. `graphify update .` após editar.
8. Escreva handoff em `docs/coordination/frontend-handoff.md` com: o que mudou, como testar, riscos, PASS/FAIL.

## Modelo / esforço

Use o melhor julgamento visual WPF. Prioridade: **clareza + fluidez > ornamentação**.

## Entrega

Commit na branch do worktree (mensagem clara). Não faça merge em main. Handoff pronto para o orquestrador revisar.
