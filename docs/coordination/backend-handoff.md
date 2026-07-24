# Backend handoff

Status: **concluido em 2026-07-24**

## Resumo

Implementado o escopo `FPS-P0-P1-BE` com probe tipado de gaming performance, novas APIs explicitas para VBS/HVCI, fullscreen optimizations por jogo e quiet mode de overlays/captura, alem de CLI self-test read-only e atualizacao do catalogo.

## Arquivos criados

- `src/App/GamingFpsProbeSelfTest.cs`

## Arquivos modificados

- `src/ApexTweaker.Contracts/Optimizations/WindowsOptimizationModels.cs`
- `src/ApexTweaker.Contracts/Inventory/IWindowsOptimizationInventory.cs`
- `src/ApexTweaker.Application/Optimizations/WindowsOptimizationApplicationFacade.cs`
- `src/ApexTweaker.Application/Optimizations/WindowsOptimizationCatalog.cs`
- `src/ApexTweaker.Application/Optimizations/WindowsOptimizationSelfTest.cs`
- `src/ApexTweaker.Windows/Inventory/WindowsOptimizationInventoryService.cs`
- `src/App/Program.cs`
- `src/App/WindowsOptimizationService.cs`
- `src/Services/SystemDiagnosticsService.cs`
- `src/Services/TweakService.cs`
- `graphify-out/*` via `graphify update .`

## Arquivos removidos

- nenhum

## Decisoes tomadas

- `GamingPerformanceProbe` ficou no contrato compartilhado e eh capturado por `WindowsOptimizationInventoryService`, para manter leitura separada das mutacoes.
- ReBAR ficou como `best effort`: o probe retorna `Unknown` com ponteiro para `bios.resizable-bar` quando nao ha sinal confiavel no inventario Windows.
- `ApplyVbsMemoryIntegrityDisable(bool confirmed)` so muta quando `confirmed == true`; sem confirmacao retorna `[SKIP]`.
- As novas mutacoes usam read-back previo para registrar `[SKIP]` quando o valor alvo ja esta aplicado.
- O self-test `--gaming-fps-probe-self-test` nao chama nenhuma API de mutacao; ele valida probe, diagnostico e catalogo apenas por leitura.

## Contratos afetados

- `IWindowsOptimizationInventory` agora expõe `CaptureGamingPerformanceProbe()`.
- `WindowsOptimizationModels` ganhou `FeatureState`, `RequestedFeatureState`, `ResizableBarStatus`, `ResizableBarProbe` e `GamingPerformanceProbe`.
- `WindowsOptimizationService` agora expõe:
  - `CaptureGamingPerformanceProbe()`
  - `ApplyVbsMemoryIntegrityDisable(bool confirmed)`
  - `ApplyGameFullscreenOptimizationsOff(string? exePath)`
  - `ApplyCompetitiveCaptureQuiet()`

## Testes executados

| Comando | Exit code | Notas |
|---------|-----------|-------|
| `dotnet build ApexTweaker.sln -c Release` | `0` | build Release completo ok |
| `dotnet run --project ApexTweaker.csproj -c Release -- --gaming-fps-probe-self-test` | `1` | falhou por `NU1301` ao consultar `api.nuget.org` para repository signatures, apesar do build ja estar pronto |
| `dotnet run --project ApexTweaker.csproj -c Release --no-build --no-restore -- --gaming-fps-probe-self-test` | `0` | self-test novo passou sem rebuild |
| `dotnet .\bin\Release\net10.0-windows\ApexTweaker.dll --gaming-fps-probe-self-test` | `0` | self-test novo passou no artefato Release |
| `dotnet .\bin\Release\net10.0-windows\ApexTweaker.dll --market-coverage-self-test` | `0` | regressao basica do catalogo tambem passou |
| `graphify update .` | `0` | grafo AST atualizado; `graph.json`, `graph.html` e `GRAPH_REPORT.md` regenerados |

## Erros restantes

- `dotnet run` sem `--no-build --no-restore` continua vulneravel ao erro local de rede/assinatura `NU1301` com NuGet.

## Riscos

- O probe de ReBAR nao afirma `Enabled/Disabled`; ele assume `Unknown` quando o Windows nao entrega um sinal confiavel e delega a confirmacao final ao checklist BIOS/driver.
- As novas APIs de mutacao foram adicionadas no backend, mas este handoff nao inclui ligacao de UI/WPF para botoes dedicados.
- O worktree ja tinha mudancas nao relacionadas em `docs/coordination/backend-task.md` e arquivos fora do escopo; elas nao foram alteradas por esta entrega.

## Itens nao concluidos

- Nenhum dentro do escopo pedido em `backend-task.md`.

## Commit

nao commitado / hash: `git rev-parse HEAD` nao executado
