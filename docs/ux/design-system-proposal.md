# Proposta de sistema visual — ApexTweaker

**Data:** 2026-07-25  
**Fase:** 1 (proposta; sem alterar componentes ainda)  
**Detalhamento Fase 4:** expandir em `design-principles.md`, `design-tokens.md`, etc.

## Auditoria visual (estado)

**Presente**

- `MacTheme.xaml` + `AppThemeManager` (light/dark, tokens de cor/superfície).
- Controles: `RiskBadge`, `Snackbar`, Command Palette.
- Desempenho com tom maduro (sem “+FPS”).
- Catalog rows densas (Settings-like).

**Problemas**

- Modules = lista longa de botões (muitas decisões sem hierarquia).
- Snackbar sem severidade Error; falhas viram Warning.
- Nome “MacTheme” vs linguagem desejada (minimal corporativo / Windows Settings).
- Empty/partial states ausentes no Catálogo.
- A11y irregular (Modules/Catalog/Utilities fracos).

## Direção (inspiração, não cópia)

Princípios Apple-like: clareza, hierarquia, respiro, uma ação principal — **sem** copiar componentes/identidade Apple.  
Alinhar a `docs/research/frontend-maturity-inspirations.md` (Fluent/Settings).

## Princípios (resumo)

1. Uma ação principal por tela de fluxo.  
2. Detalhes técnicos sob progressive disclosure.  
3. Riscos sempre visíveis antes de mutar.  
4. Motion curto e funcional (<200–300ms).  
5. Nunca animação que atrase a tarefa.  
6. Cores restritas; Error/Warning/Success distintos.  
7. Espaço em branco > cards decorativos.

## Tokens (evoluir a partir do existente)

Manter chaves atuais (`AccentBrush`, `Card*`, `TextPrimary`, `Error*`, `Warning*`, `Success*`).  
Adicionar semanticamente:

- `OperationProgressBrush`
- `RiskConditionalBrush` (se necessário além do badge)
- tipografia de título de fluxo vs body

Renomear tema “Mac” → “ApexTheme” em task FE dedicada (não na primeira).

## Estados de interação obrigatórios

Idle · Hover · Pressed · Focus visible · Disabled · Loading · Success · Error · Partial · Empty · RestartRequired.

## Fluxos a redesenhar primeiro (não tudo)

1. análise  
2. preset recomendado  
3. confirmação  
4. execução (progresso)  
5. resultado  
6. rollback  

Primeira task FE **não** redesenha esses fluxos inteiros — só feedback mínimo + empty/error no Catálogo (ver master-plan).

## Acessibilidade mínima

- `AutomationProperties.Name` em ações primárias.
- Foco teclado visível (já há tokens de focus).
- Não depender só de cor no RiskBadge (já usa texto/glyph — manter).
- Contraste AA nos textos de erro.
