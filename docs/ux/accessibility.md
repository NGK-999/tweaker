# Acessibilidade — ApexTweaker

**Data:** 2026-07-25  
**Status:** baseline + gaps

## Observado

- Focus visuals nos tokens.
- RiskBadge com texto + glyph (não só cor).
- Algumas telas com `AutomationProperties`.
- Modules/Catalog/Utilities: cobertura fraca.

## Requisitos mínimos (Fase 4+)

1. Nome acessível em toda ação primária e nav.  
2. Ordem de tab lógica no fluxo analyze→apply→result.  
3. Não travar teclado durante Busy sem anunciar estado.  
4. Snackbar/announces para leitores de tela (quando viável em WPF).  
5. Contraste AA em Error/Warning sobre surfaces.  
6. Hit target ≥ 40px em CTAs principais.

Primeira task FE: pelo menos empty/error Catalog com AutomationProperties no CTA de retry.
