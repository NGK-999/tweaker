# Frontend handoff — L2 (Otimização Windows / Presets Gamer)

Status: **L2 concluída (placeholder mock)** — aguardando integração pelo orquestrador

Agente: Claude Code (frontend)
Branch: `agent/claude-l2-winopt`
Worktree: `C:\projetos\Apextweaker-claude` (criado a partir do HEAD `c3d4733`)

## Resumo

Criada a shell visual placeholder da futura página "Otimização Windows / Presets Gamer",
**100% mock**, sem ligar backend. Layout gamer-friendly consistente com o MacTheme:
cabeçalho + descrição (foco em estabilidade/frametime, sem "FPS mágico"), lista de 5
perfis com badges de estado (Recomendado / Requer confirmação / Bloqueado), perguntas de
uso (checkboxes com estado local) e botão "Analisar" **desabilitado** até o backend existir.

Trabalho feito em **worktree separado** (regra de ownership: "nunca dois agentes no mesmo
working tree"). O tree principal segue com o WIP do Codex intacto.

## Arquivos criados

- `src/UI/Wpf/Views/WindowsOptimizationView.xaml`
- `src/UI/Wpf/Views/WindowsOptimizationView.xaml.cs`
- `src/UI/Wpf/ViewModels/WindowsOptimizationViewModel.cs`

## Arquivos modificados

- Nenhum. (MainWindow **não** foi tocado — ver "Itens não concluídos".)

## Arquivos removidos

- Nenhum.

## Decisões de UX

- **Presets como `ListBox`** (não botões soltos): navegação por teclado + foco visível de
  graça (acessibilidade). Seleção via `SelectedItem` (estado local do VM).
- **Badge de estado = glifo + texto + cor** (não depende só de cor). Cores vêm de tokens do
  tema (`SuccessSurfaceBrush`/`AccentBrush`/`ErrorSurfaceBrush`), não hardcoded.
- **5 perfis mock**: Gamer Seguro, Competitivo, Streamer, Notebook, Experimental. Os textos
  falam de estabilidade/latência/térmica — evitam prometer FPS.
- **Botão "Analisar" desabilitado** com tooltip explicando que liga no backend (L4/L5), para
  deixar explícito que nada é aplicado.
- Banner de aviso reforçando "prévia visual, dados simulados, nada é alterado".

## Contratos afetados

Nenhum contrato alterado. **Pendência de alinhamento** (não é mudança de contrato): o VM usa
um enum de apresentação `PresetStatus { Recommended, RequiresConfirmation, Blocked }` como
mock. Quando o binding real for ligado (L4/L5), **substituir** por
`ApexTweaker.Contracts.Optimizations.WindowsOptimizationModels` (o status real). Não referenciei
o assembly de Contracts para não mexer em `.csproj` (proibido para o frontend) e porque a
tarefa pede mock-only.

## Testes executados

| Comando | Exit code | Notas |
|---------|-----------|-------|
| `dotnet build ApexTweaker.sln -c Release` (no worktree) | 0 | 0 avisos / 0 erros; WPF globbing incluiu os arquivos novos |
| `dotnet run --project ApexTweaker.csproj -- --minecraft-self-test` | 0 | `SELF_TEST_OK` — sem regressão |

## Erros restantes

- **No tree principal**, `dotnet build ApexTweaker.sln` está **quebrado pelo WIP do Codex**:
  `ApexTweaker.Windows/Inventory/WindowsOptimizationInventoryService.cs` referencia
  `HardwareEnvironmentDetector`, que foi **deletado** no mesmo WIP (`src/Services/HardwareEnvironmentDetector.cs`).
  **Não é causado pelo frontend.** Por isso a validação da L2 foi feita no worktree limpo (HEAD).

## Riscos

- Dois agentes no mesmo working tree principal (Codex + docs). Mitigado: meu código foi para
  worktree/branch próprios.
- Enum de apresentação pode divergir do contrato se a substituição em L4/L5 for esquecida
  (ver "Contratos afetados").

## Itens não concluídos

- **Página NÃO registrada no host de navegação.** Deixei desconectada de propósito: registrar
  exigiria tocar `MainWindow` (pageFactory + botão de nav), e a tarefa manda parar se o wiring
  passar de ~30 linhas / para evitar colisão com o Codex no God object.
  - **Ponto de integração** (para o orquestrador): em `MainWindow.xaml.cs`, adicionar
    `pageFactories["WindowsOptimization"] = () => new WindowsOptimizationView();` + um botão de
    nav e um case em `ShowPageAsync`/título. Mudança pequena e reversível.

## Nota de coordenação

- Trabalho anterior fora do meu lane (modo `--demo` / Unit 1, que tocou Infrastructure/Services/App)
  foi **revertido** a pedido — o tree principal não tem mais nenhuma alteração minha além deste handoff.

## Commit

Não commitado. Branch `agent/claude-l2-winopt` no worktree `C:\projetos\Apextweaker-claude`.
(AGENTS.md proíbe commit/merge sem pedido explícito.)
