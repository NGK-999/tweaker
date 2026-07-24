# Índice de skills (roteamento)

Data: 2026-07-24  
Status: índice + plano — **skills de projeto ainda não criadas** (aguardar aprovação do routing).

## Política

- Não criar skill para tarefa única.
- Criar skill só quando o procedimento se repetir e tiver entradas/saídas claras.
- Carregar **somente** skills da rota em `agent-routing.yaml`.
- Skill **não** concede permissão acima da rota (ex.: safety skill não autoriza GPO apply).

## Skills já instaladas na máquina (observadas)

### Codex / agents (usuário)

| Skill | Onde | Uso no ApexTweaker |
|-------|------|--------------------|
| `impeccable` | `~/.codex/skills` | UI visual (Claude/FE), não backend GPO |
| `find-skills` | `~/.codex/skills`, `~/.agents/skills` | descoberta |
| `computer-use` | `~/.agents/skills` | **evitar** para este projeto (risco) |
| `orca-cli` / `orchestration` | `~/.agents/skills` | só se orquestrar worktrees Orca |
| `graphify` | `~/.claude/skills` | auditoria de código — orquestrador |
| `context7-mcp` | `~/.claude/skills` | docs de libs (raro aqui) |
| `retune-visual-changes` | `~/.claude/skills` | FE visual |
| `using-superpowers` | `~/.claude/skills` | meta |

### Projeto ApexTweaker

| Caminho | Existe? |
|---------|---------|
| `.agents/skills/` | não |
| `.claude/skills/` | não |
| `.cursor/skills/` | não |

## Skills planejadas (criar depois da aprovação)

### Backend / orquestração — `.agents/skills/`

| Skill | Entrada | Saída | Rotas |
|-------|---------|-------|-------|
| `architecture-audit` | pergunta de arquitetura | mapa + riscos | architecture |
| `api-contract-review` | diff Models/Contracts | ok/bloqueio + campos | contract-change, integration |
| `backend-module-refactor` | escopo de pastas | diff limitado + build | backend-structural |
| `windows-optimization-safety` | regra GPO/plano | checklist risco; **bloqueia apply** | windows-* |
| `test-and-verify` | comandos | exit codes | quase todas |
| `git-handoff` | diff | `*-handoff.md` preenchido | Codex/Claude |
| `integration-review` | handoffs + diff | go/no-go | integration |

### Frontend — `.claude/skills/`

| Skill | Entrada | Saída | Rotas |
|-------|---------|-------|-------|
| `frontend-architecture` | tela/fluxo WPF | plano de Views/VMs | FE complex |
| `component-extraction` | XAML inchado | controles/VM menores | FE refactor |
| `design-system-consistency` | view nova | alinhamento MacTheme | FE |
| `accessibility-review` | view de riscos | checklist a11y | FE risk UX |
| `frontend-verification` | build | exit code + smoke `--demo` | FE |

**Não planejar** `api-client-refactor` HTTP — o app não tem cliente REST. Se necessário no futuro: skill `inprocess-facade-binding` (bind ViewModel → fachada C#).

## Como o orquestrador seleciona skills

1. Classificar `task_type` via `agent-routing.yaml`.
2. Carregar só a lista `skills:` da rota.
3. Se faltar procedimento repetível → instruir no prompt da tarefa, **não** inventar skill.
4. Registrar skills usadas em `execution-log.md`.
