# Arquitetura-alvo — ApexTweaker

Data: 2026-07-24  
Status: proposta do orquestrador (ainda não implementada)

## Princípio

Preservar o monólito .NET + WPF funcional, **sem** inventar um servidor HTTP nesta fase.
A separação FE/BE deve ser **por pastas + contratos C# + ownership**, com migração
progressiva para reduzir o god-object `MainWindow`.

## Visão alvo (progressiva)

```text
src/App                 → bootstrap (orquestrador)
src/Models              → contratos compartilhados (orquestrador)
src/Application         → (futuro) casos de uso / fachadas estáveis
src/Services            → backend Windows (Codex)
src/Core                → backend mutações (Codex)
src/Infrastructure      → CommandRunner / RuntimeMode (Codex, com gates demo)
src/Minecraft           → backend Minecraft (Codex)
src/UI/Wpf              → frontend (Claude Code)
native/                 → nativo (orquestrador / Codex com autorização)
docs/contracts          → contratos documentados (orquestrador)
docs/coordination       → planos e handoffs (orquestrador)
```

## Fronteira FE ↔ BE (alvo imediato)

Em vez de HTTP, o contrato é uma **fachada de aplicação** estável:

```text
UI (Views/ViewModels)
   │  chama apenas
   ▼
IWindowsOptimizationFacade / ITweakFacade / IMinecraftFacade
   │  implementado por
   ▼
Services (inventário, recomendação, apply, backup, rollback, telemetria)
```

Regras:

1. UI **não** monta strings de `powercfg`/`reg`/`bcdedit`.
2. UI **não** escolhe chaves de Registro.
3. UI envia **intenções tipadas** (preset, confirmações de uso, IDs de regra).
4. Backend devolve **planos tipados** + logs + necessidade de reboot + risco.
5. Mutações reais só via `MutationExecutor` + backup; em `--demo`, nunca escrevem.

## Presets gamer (alvo de produto)

Unificar conceitualmente (sem fundir código legado de uma vez):

| Camada | Conteúdo |
|--------|----------|
| Performance hardware | power, CPU híbrida, DPC, GPU, rede, input (`TweakService` legado) |
| Ruído Windows / GPO | catálogo `WindowsOptimization*` + LGPO (novo) |
| Uso detectado | Game Pass, OBS, OneDrive, remoto, notebook, etc. |

Preset recomendado final = interseção filtrada (**15–35** regras GPO + tweaks de performance compatíveis).

Expectativa de UX: **estabilidade / frametime**, não “+FPS mágico”.

## Fases de migração (sem big-bang)

### M0 — Congelar contratos (esta entrega)

Documentar APIs atuais; ownership; não reorganizar pastas.

### M1 — Completar backend Windows Optimization (análise)

Codex: inventário de uso mais rico, catálogo Gamer Seguro, self-tests.
Sem LGPO apply ainda. Sem desligar Xbox/Search automaticamente.

### M2 — Fachada + UI demo

- Codex: `WindowsOptimizationService` expõe plano tipado estável.
- Claude: tela/seção de presets gamer em `--demo`, consumindo só a fachada.
- Orquestrador: aprova qualquer mudança em `Models`.

### M3 — Apply via LGPO + rollback

Codex: export/backup LGPO, apply seletivo, `gpupdate`, restore.
Proibido executar mutações na máquina de desenvolvimento sem `--yes` em ambiente de teste dedicado.

### M4 — Desacoplar legado perigoso

`ApplyPolicyAndServiceTweaks` deixa de ser caminho padrão da UI;
marcado legado ou reescrito para respeitar o catálogo novo.

### M5 — (Opcional, futuro) extrair assembly

Só depois das fachadas estáveis: `ApexTweaker.Core` + `ApexTweaker.UI`.
Não fazer agora.

## O que explicitamente NÃO é alvo agora

- Reescrever em Electron/React
- Introduzir API REST só para “separar FE/BE”
- Fundir `PresetKind` e `WindowsOptimizationPreset` numa tacada
- Apagar Minecraft / telemetria / pipeline transacional
- Desabilitar Defender, Windows Update, VBS em presets automáticos

## Critérios de sucesso da arquitetura-alvo

- Codex e Claude não editam os mesmos arquivos
- UI de otimização Windows funciona em `--demo` sem mutar a máquina
- Plano GPO mostra risco, propósito e evidência (`None`/`Plausible`/`Measured`)
- Backup/rollback continuam obrigatórios antes de apply real
- Self-tests passam: `--demo-self-test`, `--minecraft-self-test`, build Release
