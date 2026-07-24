# Backend Task — FPS P0/P1 (modo A)

**Task ID:** `FPS-P0-P1-BE`  
**Executor:** Codex (`gpt-5.4`, effort `high`) — CLI 0.139 não aceita `gpt-5.6-terra`  
**Worktree:** `C:\projetos\Apextweaker-codex` branch `agent/codex-fps-p0`  
**Proibido:** mutar Windows na máquina de dev nos testes; unsigned kernel drivers; Defender/Update off no Auto

## Contexto

Prioridade FPS realista (não Winaero/Cyberpunk/ComputerCraft):
1. Probe VBS + HVCI + HAGS + Game Mode/DVR + ReBAR best-effort
2. `ApplyVbsMemoryIntegrityDisable(confirmed)` + restart note; MayApplyAutomatically=false
3. `ApplyGameFullscreenOptimizationsOff(exePath?)` (Valorant locator se null)
4. `ApplyCompetitiveCaptureQuiet()`
5. Catálogo: `fps.vbs-hvci`, `fps.hags-status`, `fps.rebar-checklist`, `fps.fso-per-game`, `fps.competitive-overlays`
6. `--gaming-fps-probe-self-test` (só leitura)

## Idempotência

Preferir log `[SKIP] já aplicado` quando read-back == alvo. Reaplicar registry/serviço/power é seguro.

## Estado parcial (continuar, não recomeçar)

Já no worktree: `GamingPerformanceProbe` no inventory/facade/host; wiring `Apply*` no host **sem** implementação em `TweakService` ainda.  
**Nota ownership:** `GamingPerformanceProbe` em Contracts deve ser **public** (não `internal`) — orquestrador aprova.

## Verificação

```
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -c Release -- --gaming-fps-probe-self-test
```

## Handoff

`docs/coordination/backend-handoff.md` — arquivos, testes, riscos.
