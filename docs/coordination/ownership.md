# Matriz de propriedade (ownership)

**Data:** 2026-07-25  
**Papel do chat principal:** Chief Product Engineer / Architect / Quality Lead (Orquestrador)

## Agentes

| Agente | Papel | Escopo |
|--------|-------|--------|
| **Orquestrador** | produto, arquitetura, contratos, integração, qualidade, docs canônicos | abaixo |
| **Codex** | backend, Windows, segurança, confiabilidade, self-tests técnicos | Services/Core/Infrastructure/Minecraft/Application/Windows |
| **Claude Code** | frontend, UX, design system, a11y, testes visuais | `src/UI/**` |

## Matriz por caminho

| Caminho | Dono | Notas |
|---------|------|-------|
| `src/UI/**` | Claude Code | XAML, VM, Themes, Controls |
| `src/Services/**` | Codex | tweaks, backup, telemetria |
| `src/Core/**` | Codex | pipeline mutações |
| `src/Infrastructure/**` | Codex | CommandRunner, futuro RuntimeMode |
| `src/Minecraft/**` | Codex | motor; UI chama via contratos |
| `src/NativeInterop/**` | Codex | P/Invoke |
| `src/ApexTweaker.Windows/**` | Codex | inventário |
| `src/ApexTweaker.Application/**` | Codex | fachadas (sem breaking de contrato) |
| `src/ApexTweaker.Contracts/**` | **Orquestrador** | DTOs públicos |
| `src/Models/**` | **Orquestrador** | contratos legado host |
| `src/App/Program.cs` | **Orquestrador** | flags CLI globais |
| `src/App/WindowsOptimizationService.cs` | Codex | adapter host |
| `src/App/*SelfTest*.cs` | Codex | self-tests |
| `native/**` | Orquestrador (+ Codex só autorizado) | |
| `*.csproj`, `*.sln` | **Orquestrador** | |
| `scripts/**` | Orquestrador | |
| `docs/product/**`, `docs/architecture/**`, `docs/reliability/**`, `docs/quality/**`, `docs/ux/**` | Orquestrador | |
| `docs/contracts/**` | Orquestrador | |
| `docs/coordination/**` | Orquestrador | tasks/handoffs/prompts |
| `AGENTS.md`, `README.md` | Orquestrador | |
| `graphify-out/**` | gerado | atualizar via `graphify update .` |

## Arquivo misturado

| Arquivo | Regra |
|---------|-------|
| `MainWindow.xaml.cs` | Claude: navegação/UI/estados. **Nova chamada de serviço** exige contrato aprovado. Preferir não expandir domínio. |

## Proibições

- dois agentes no mesmo worktree/arquivos;
- alterar FE e BE na mesma task sem contrato;
- mutações reais na máquina de dev em testes estruturais;
- apagar “código morto” sem prova;
- bypass de UAC / `--dangerously-skip-permissions` em automação.

## Worktrees

| Agente | Path | Branch pattern |
|--------|------|----------------|
| Codex | `C:\projetos\Apextweaker-codex` | `agent/codex-<task>` |
| Claude | `C:\projetos\Apextweaker-claude` | `agent/claude-<task>` |
| Orquestrador | `C:\projetos\Apextweaker` | `main` |

## Revisão cruzada

- FE → Codex revisa contrato/risco técnico.
- BE → Claude revisa impacto UX/copy de erro.
- Decisão final → Orquestrador.
