# Plano mestre de coordenação

Data: 2026-07-24  
Status: Fase 1 concluída (auditoria + docs). Código ainda não reorganizado.

## Premissa corrigida

Não há API HTTP entre FE e BE. Coordenação = **contratos C# + ownership de pastas + tarefas pequenas**.

## Estado atual do trabalho paralelo

| Item | Estado |
|------|--------|
| Codex WIP Windows Optimization (análise) | presente no working tree, não commitado |
| Demo mode (`RuntimeMode`, `--demo`) | presente no working tree |
| UI ligada ao novo catálogo GPO | **não** |
| LGPO apply | **não** |
| Commits dos agentes | nenhum nesta fase |

**Ordem:** preservar WIP; não resetar; integrar via tarefas.

## Lotes (um por vez)

| ID | Lote | Dono | Dependência | Objetivo |
|----|------|------|-------------|----------|
| L0 | Auditoria + ownership + contratos | Orquestrador | — | **FEITO** nesta entrega |
| L1 | Backend: enriquecer inventário de uso + catálogo Gamer Seguro (análise only) | Codex | L0 | ver `backend-task.md` |
| L2 | Frontend: shell Apple/Hermes/Cursor + Minecraft opção A (módulo, não sidebar) + fix animações | Claude Code | L0 + aprovação usuário 2026-07-24 | ver `frontend-task.md` (L2-SHELL) — **EM EXECUÇÃO** |
| L3 | Orquestrador: revisar handoffs, consolidar Models se necessário | Orquestrador | L1 (+ L2 se houver) | |
| L4 | Backend: fachada estável `Analyze` + self-test ampliado | Codex | L3 | |
| L5 | Frontend: tela Presets Gamer consumindo fachada (demo) | Claude Code | L4 | |
| L6 | Backend: LGPO apply/backup/rollback (ambiente de teste) | Codex | L5 ou em paralelo com mock | |
| L7 | Integração + desacoplar `ApplyPolicyAndServiceTweaks` da UI padrão | Orquestrador + ambos | L6 | |
| MARKET | Cobertura mercado modo A (B1–B9) | Orquestrador + Codex/Claude | Aprovado 2026-07-24 | ver `docs/research/market-coverage-matrix.md` — **EM EXECUÇÃO** |

## Seleção de agente / modelo / esforço

Fonte de verdade: [agent-routing.yaml](agent-routing.yaml)  
Índice de skills: [skills-index.md](skills-index.md)  
Log: [execution-log.md](execution-log.md)

Antes de qualquer L1/L2, o orquestrador deve emitir um bloco `ROUTING DECISION`
e só então preparar o prompt ou invocar CLI.

Bindings atuais:

| Task | Rota |
|------|------|
| L1 | `windows-optimization-analysis` → Codex `gpt-5.6-terra` effort `high` |
| L2-SHELL | `frontend-refactor` → Claude `sonnet` effort `high` (Minecraft opção A + redesign) |
| L6 | `windows-safety-critical` → Codex effort `xhigh` + revisão orquestrador |
| L7 | `integration-review` → orquestrador `high` |

1. **L2-SHELL (Claude)** — aprovado: redesign + Minecraft opção A (prioridade atual).
2. **L1 (Codex)** — standby até shell estabilizar *ou* worktree Codex isolado com WIP; não compete com L2-SHELL.
3. Aguardar `frontend-handoff.md`.
4. Orquestrador revisa diff UI, build, atualiza `integration-status.md`.
5. Depois: L1 / L5 bind WindowsOptimization.

## Comandos oficiais de verificação

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -- --demo-self-test
dotnet run --project ApexTweaker.csproj -- --minecraft-self-test
```

Publicação (quando necessário, orquestrador):

```powershell
dotnet publish ApexTweaker.csproj -c Release -r win-x64 --self-contained true -o release-v2
.\scripts\Test-Release.ps1
```

## Modo de execução dos agentes

CLIs disponíveis nesta máquina (confirmado):

- `codex --version` → `codex-cli 0.139.0`
- `claude --version` → `2.1.218`

Nesta primeira entrega: **modo manual** (prompts em `backend-task.md` / `frontend-task.md`).
Automação por worktree fica para depois da aprovação do usuário.

## Regras anti-timeout

- Uma tarefa = uma categoria (catálogo **ou** inventário **ou** UI shell).
- Proibido “auditar + reorganizar + aplicar LGPO + redesenhar UI” no mesmo prompt.
- Se o agente extrapolar: parar, handoff, nova tarefa.

## Definição de pronto da coordenação (fase integração)

- Build Release OK
- Self-tests OK
- Ownership respeitada no diff
- Models só mudaram com aprovação do orquestrador
- Nenhuma mutação Windows executada nos testes dos agentes
- Handoffs com evidência (comandos + exit codes)
