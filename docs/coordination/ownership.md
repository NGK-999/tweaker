# Matriz de propriedade (ownership)

Data: 2026-07-24  
Orquestrador: único agente autorizado a alterar contratos compartilhados e docs de coordenação.

## Agentes

| Agente | Papel | Escopo |
|--------|-------|--------|
| **Orquestrador** (este chat) | arquitetura, contratos, integração, docs de coordenação | ver abaixo |
| **Codex** | executor backend | Services / Core / Infrastructure / Minecraft / NativeInterop (com cuidado) |
| **Claude Code** | executor frontend | `src/UI/**` apenas |

## Matriz por caminho

| Caminho | Dono | Notas |
|---------|------|-------|
| `src/UI/**` | Claude Code | XAML, ViewModels, Themes, Windows, animações |
| `src/Services/**` | Codex | tweaks/backup/telemetria legados |
| `src/Core/**` | Codex | pipeline de mutações |
| `src/Infrastructure/**` | Codex | `CommandRunner`, `RuntimeMode` |
| `src/Minecraft/**` | Codex | motor Minecraft; UI Minecraft chama via MainWindow/VM |
| `src/NativeInterop/**` | Codex | P/Invoke; mudanças exigem build nativo |
| `src/ApexTweaker.Windows/**` | Codex | inventário Windows (implementação) |
| `src/ApexTweaker.Application/**` | Codex | catálogo/recomendação/fachada/self-test (sem mudar contratos públicos sem ok) |
| `src/ApexTweaker.Contracts/**` | **Orquestrador** | DTOs/enums públicos entre assemblies; Codex propõe, orquestrador aprova |
| `native/**` | Orquestrador (+ Codex só com autorização explícita) | C++ / vcxproj |
| `src/Models/**` | **Orquestrador** | contratos legados do host; mudanças coordenada |
| `src/App/Program.cs` | **Orquestrador** | flags CLI globais; mudanças coordenadas |
| `src/App/DemoSafetySelfTest.cs` | Codex | self-test de demo (backend) |
| `src/App/WindowsOptimizationService.cs` | Codex | adapter/host do Analyze (WIP) |
| `src/App/ApplicationPaths.cs`, `AppInfo.cs` | Orquestrador | caminhos/versão |
| `ApexTweaker.csproj`, `ApexTweaker.sln`, `*.csproj` novos | **Orquestrador** | TFM, ProjectReferences, pacotes — Codex não altera sem autorização |
| `scripts/**` | Orquestrador | build/release/test |
| `docs/architecture/**` | Orquestrador | |
| `docs/contracts/**` | Orquestrador | |
| `docs/coordination/**` | Orquestrador | prompts e handoffs |
| `docs/*` produto (Minecraft, releases) | Orquestrador (conteúdo técnico pode ser redigido por agentes sob pedido) | |
| `AGENTS.md`, `README.md` | Orquestrador | |
| `graphify-out/**` | gerado | não é fonte; não “corrigir” à mão |

## Arquivos atualmente misturados (atenção)

| Arquivo | Problema | Regra temporária |
|---------|----------|------------------|
| `src/UI/Wpf/MainWindow.xaml.cs` | UI + orquestra 20 serviços | **Claude Code** pode editar UI/navegação; **qualquer nova chamada de serviço** precisa contrato aprovado pelo orquestrador. Preferir não expandir wiring até fachada existir. |
| `src/Models/WindowsOptimizationModels.cs` | contrato novo WIP | Orquestrador; Codex solicita campos via handoff |

## Proibições absolutas

Nenhum agente pode:

- editar arquivos do outro sem autorização do orquestrador;
- alterar `src/Models/**` sem PR/diff revisado pelo orquestrador;
- aplicar GPO/Registro/BCD/serviços reais na máquina de desenvolvimento;
- usar `--dangerously-skip-permissions` / bypass de UAC em automação;
- atualizar lock/csproj de pacotes sem justificativa documentada;
- apagar código “parece morto” sem prova (referência + teste).

## Worktrees (modo automatizado)

Quando usar CLI:

| Agente | Worktree sugerido |
|--------|-------------------|
| Codex | `../Apextweaker-codex` (branch `agent/codex-<task>`) |
| Claude Code | `../Apextweaker-claude` (branch `agent/claude-<task>`) |
| Orquestrador | worktree principal `C:\projetos\Apextweaker` |

Nunca dois agentes no mesmo working tree.

## Resolução de conflito de ownership

1. Agente para e registra no handoff.
2. Orquestrador decide e atualiza este arquivo se necessário.
3. Só então a alteração compartilhada é aplicada.
