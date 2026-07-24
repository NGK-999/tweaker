# Frontend Handoff — L2-SHELL (Claude Code)

**Branch:** `agent/claude-l2-shell` (worktree `C:\projetos\Apextweaker-claude`)
**Escopo:** `src/UI/**` apenas — nenhum arquivo fora de `src/UI` foi tocado.

## Arquivos tocados

| Arquivo | Mudança |
|---|---|
| `src/UI/Wpf/MainWindow.xaml` | Removido `MinecraftButton` da sidebar. |
| `src/UI/Wpf/MainWindow.xaml.cs` | Removido `MinecraftButton` do loop de `SetActiveNav`; removido `MinecraftButton_OnClick`; `HandleModuleRequestedAsync` ganhou `case MinecraftPageKey` que chama `ShowPageAsync(MinecraftPageKey, ModulesButton)` — reaproveita o dispatch por `Tag` já usado pelos outros botões de Módulos, sem novo `EventHandler`. |
| `src/UI/Wpf/Views/ModulesView.xaml` | Reescrito: cada grupo (CPU e energia / Rede e periféricos / GPU e background / Jogos e Ferramentas) agora tem 1 linha de descrição por botão; **Políticas/Serviços** ganhou badge "AVANÇADO" (borda/texto `WarningBrush`); novo botão **Minecraft / Cobblemon** (`Tag="Minecraft"`, estilo `MacPrimaryButton` — preenchido, visualmente distinto dos tweaks secundários) na seção "Jogos / Ferramentas". |
| `src/UI/Wpf/Views/ModulesView.xaml.cs` | `EnumerateModuleButtons` trocou de leitura direta de `Children` (quebrava porque os botões agora estão dentro de `Grid`s por linha) para uma busca recursiva na árvore visual (`FindButtons`), e passou a incluir o novo `GamesButtonsPanel`. |
| `src/UI/Wpf/Animations/PageTransitionAnimator.cs` | Corrigido race de navegação rápida (ver abaixo). |
| `src/UI/Wpf/Animations/UiMotion.cs` | Corrigido race no fade do título de página (ver abaixo). |

`docs/coordination/frontend-handoff.md` — este arquivo.

Nenhum arquivo de `src/Minecraft/**`, `src/Services/**`, `src/Core/**`, `src/Infrastructure/**`, `*.csproj`/`*.sln`, `docs/architecture/**` ou `docs/contracts/**` foi alterado. `WindowsOptimizationViewModel.cs` / `WindowsOptimizationView.xaml(.cs)` (herdados de sessão anterior no worktree) permanecem intocados e desconectados da navegação.

## Como testar a opção A (Minecraft)

1. `dotnet build ApexTweaker.sln -c Release` (0 erros confirmado nesta sessão).
2. `dotnet run --project ApexTweaker.csproj -- --demo` e navegar: Sidebar não tem mais "Minecraft Rápido" (só Dashboard, Módulos, Telemetria, Utilidades).
3. Em **Módulos**, seção "Jogos / Ferramentas" → botão **Minecraft / Cobblemon** abre a mesma página Minecraft de sempre (mesmo `MinecraftView`, mesmos handlers/serviços — nada foi movido no backend).
4. `dotnet run --project ApexTweaker.csproj -- --minecraft-self-test` — **PASS em todos os 21 casos**, incluindo "XAML da pagina Minecraft carrega em thread STA", confirmando que a página e os serviços Minecraft continuam intactos.

## Bugs de animação encontrados e corrigidos

Causa raiz (comum aos dois arquivos): em `ShowPageAsync` (MainWindow), uma navegação rápida chama `transitionCancellation.Cancel()` e imediatamente inicia uma nova transição. A resposta ao cancelamento das transições antigas, porém, é retomada de forma assíncrona pelo dispatcher do WPF (continuação de `await` com `SynchronizationContext` capturado) — ou seja, o cleanup da transição **cancelada** podia rodar *depois* que a transição **nova** já tinha assumido o estado compartilhado, sobrescrevendo-o. Isso batia exatamente com os sintomas relatados: flicker, conteúdo sumindo e animação cortada em navegação rápida.

- `PageTransitionAnimator.ShowAsync`: o bloco `catch (OperationCanceledException)` fazia `host.Content = outgoing` incondicionalmente. Se uma transição mais nova já tivesse tomado `host.Content`, essa linha o revertia para a página antiga (fonte do "conteúdo sumindo"). Corrigido com um token de posse (`activeTransition`) por host: o cleanup só toca `host.Content` se ainda for a transição mais recente.
- `UiMotion.AnimateHeaderAsync`: o cleanup do cancelamento rodava dentro de `storyboard.Dispatcher.BeginInvoke(...)`, adiando desnecessariamente a limpeza (`target.Text = newText` antigo) para o próximo tick do dispatcher — podendo sobrescrever o texto/opacidade/transform que uma animação de header mais nova já tinha começado a aplicar (fonte da "animação cortada"). Corrigido removendo o `BeginInvoke`: o callback de `CancellationToken.Register` já roda de forma síncrona na thread que chama `Cancel()`, então a limpeza agora acontece antes da próxima transição começar, sem sobreposição.

Verificação: build limpo + self-test Minecraft (que inclui carregamento de XAML em thread STA) passam. Teste manual de navegação rápida (clicar várias vezes entre abas) fica como verificação visual recomendada com `--demo` local, já que este ambiente não tem sessão interativa de desktop para gravar vídeo/screenshot.

## Módulos — reorganização

- Grupos mantidos (Core / Rede e periféricos / GPU e background), cada botão agora com 1 linha de descrição ao lado.
- **Políticas/Serviços** marcado com badge "AVANÇADO" (cor de aviso), sem ser CTA principal — nenhuma funcionalidade removida, só destaque visual de risco.
- Nova seção "Jogos / Ferramentas" com o Minecraft/Cobblemon, usando o botão preenchido (`MacPrimaryButton`) em vez do secundário usado pelos tweaks — reforça que é uma ferramenta separada, não um tweak de Windows.

## O que ficou de propósito fora

- **WindowsOptimizationView/ViewModel** não foram ligados à navegação (conforme instrução — L1 fica em standby).
- Redesign visual mais amplo (retema de cores/tokens): o `MacTheme.xaml` existente já segue a linha Apple/restrained (paleta escura neutra, sem glow roxo, sem dashboard de métricas no hero — confirmado por inspeção de `DashboardView.xaml`/`UtilitiesView.xaml`, sem nenhum `DropShadow`/`Glow`/gradiente na base de código). Não refiz tokens que já atendiam ao alvo, para não arriscar quebrar `DynamicResource` em massa sem necessidade.
- Nenhuma verificação visual interativa (screenshot) foi feita — ambiente desta sessão é um job em background sem sessão de desktop anexada; `--demo-self-test`/`--demo` abrem janela real e ficam bloqueados aguardando fechamento manual, então a validação ficou em build + self-test CLI (Minecraft) + leitura de código.

## Riscos

- `ModulesView.xaml.cs` agora depende de `VisualTreeHelper` para achar os botões dentro dos `Grid`s de cada linha — funciona porque `EnumerateModuleButtons` só é chamado depois do template estar aplicado (via `SetBusy`, chamado em runtime, não no construtor). Se `SetBusy` passar a ser chamado antes do primeiro layout pass, a árvore visual pode ainda não existir; não é o caso hoje.
- `HandleModuleRequestedAsync` agora mistura navegação (`case MinecraftPageKey`) com aplicação de tweaks (demais cases) no mesmo switch — é o padrão que o prompt pediu para reaproveitar, mas vale registrar para quem for expandir esse método depois.

## Comandos usados

```
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -- --minecraft-self-test
```
