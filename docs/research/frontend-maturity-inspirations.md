# Inspirações FE — maturidade “nível corporação”

Objetivo: ApexTweaker sentir-se utilitário confiável (tipo NVIDIA App / Windows Settings), não launcher gamer inchado (Armoury Crate / G HUB).

## Referências boas (copiar padrões)

| Fonte | O que roubar | Evitar |
|-------|--------------|--------|
| [Windows 11 Design principles](https://learn.microsoft.com/en-us/windows/apps/design/design-principles) | Calm, Effortless, Familiar; hierarquia por layering; accent só no essencial | Glow RGB, glass excessivo |
| [App settings guidelines](https://learn.microsoft.com/en-us/windows/apps/design/app-settings/guidelines-for-app-settings) | SettingsCard / Expander; apply imediato; Advanced 1 nível; About no rodapé | Página com 40 botões sem grupo |
| [NVIDIA App](https://www.techpowerup.com/review/nvidia-app-v1-0/) | Hub leve, seções claras (Home / Optimize / Status), overlay/metrics secundários, sem login obrigatório | Home cheia de promo |
| [Razer Cortex case](https://www.ashleyfolkman.com/razer-cortex) | 3 modos (Power Save / Default / Performance); impacto previsto; REVERT | Rewards / deals no core |
| [Linear UI refresh](https://www.linear.app/now/how-we-redesigned-the-linear-ui) | Chrome mais quieto que o conteúdo; densidade por alinhamento; bordas suaves | Card soup |
| [Raycast patterns](https://blakecrosley.com/guides/design/raycast) | Search-first (Ctrl+K), atalhos inline, Action Panel, Esc = volta | Paleta só decorativa |
| [WPF-UI (lepoco)](https://github.com/lepoco/wpfui/) | Fluent nativo em WPF (Navigation, Snackbar, Dialog) | Reescrever tudo de uma vez |
| [SystemManager](https://github.com/laurentiu021/SystemManager) | Sidebar em grupos colapsáveis; dashboard + 55 features sem flat list | Placeholders sem status |

## Anti-padrões (grandes marcas que a comunidade odeia)

- **Armoury Crate / Logitech G HUB**: feature dump, pesado, RGB no caminho crítico.
- **GeForce Experience antigo**: lento, conta forçada, UI datada → NVIDIA App corrigiu isso.
- **Winaero-style**: árvore enorme de toggles sem “modo seguro / auto / avançado”.

## Mapa para ApexTweaker (estado atual → maduro)

Já existe: shell sidebar + views (Dashboard, Modules, Catalog, Utilities), tokens em `MacTheme.xaml`, modo A (Auto vs Advanced).

| Prioridade | Mudança | Inspiração |
|------------|---------|------------|
| P0 | **IA clara**: Home = status + 1 CTA Auto; Catalog = SettingsCard list; Advanced separado | Windows Settings + NVIDIA |
| P0 | **Feedback corporativo**: Snackbar “Aplicado / Requer restart / SKIP já aplicado”; progress com passos | Fluent + Linear |
| P0 | **Risco tipado**: badge Safe / Advanced / Restart; confirm só em Advanced | Cortex modes |
| P1 | **Ctrl+K** busca tweaks + navega páginas | Raycast + Winaero search |
| P1 | **Sidebar quieta** (mais escura que content); menos cards; mais rows | Linear |
| P1 | **Empty / error / offline-admin** states com copy séria | Fluent |
| P2 | Avaliar WPF-UI Navigation/Snackbar **incremental** (não big-bang) | WPF-UI |
| P2 | Dashboard: métricas úteis (VBS/HVCI/HAGS/GameMode) sem “+FPS” | NVIDIA status + nosso FPS probe |

## Copy / tom

- Corporação: “Aplicado. Reinício necessário para Memory Integrity.”
- Não: “+47% FPS GUARANTEED”, RGB slogans, emojis em massa.

## Próximo sprint FE sugerido

`FE-MATURITY-P0`: SettingsCard catalog rows + Snackbar pipeline + risk badges + Home CTA único. Só `src/UI/**`.
