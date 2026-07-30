# Design tokens — ApexTweaker

**Data:** 2026-07-25  
**Status:** inventário + proposta (fonte atual: `src/UI/Wpf/Themes/MacTheme.xaml`)

## Tokens existentes (observado)

Cores semânticas: Accent, Success, Warning, Error, TextPrimary/Secondary, surfaces Card/Elevated, borders, focus.

Light/Dark via `AppThemeManager`.

## Gaps a adicionar (proposta)

| Token | Uso |
|-------|-----|
| `SnackbarErrorBorder` | falhas (hoje colapsam em Warning) |
| `ProgressTrack` / `ProgressFill` | operações longas |
| `RiskSafe/Advanced/Dangerous` | alinhados ao badge |
| `FlowTitleFontSize` / `FlowBodyFontSize` | hierarquia de jornada |

## Regra

Claude pode **usar** tokens; criar tokens novos só com ok do orquestrador se afetar tema global.
