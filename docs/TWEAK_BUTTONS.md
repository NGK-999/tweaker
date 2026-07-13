# Funcionalidades do ApexTweaker

O objetivo do app e otimizar o Windows para jogos, principalmente VALORANT, mantendo alteracoes rastreaveis e reversiveis.

Versao: **3.3.0** · Shell ativa: **WPF** (`MainWindow`).

## Mapa da interface WPF

### Dashboard

| Acao | Backend |
|------|---------|
| **OTIMIZAR SISTEMA AO MAXIMO** | `TweakService.ApplyAutonomousOptimization()` via `OptimizationEngine` |
| **Criar ponto de restauracao** | `SystemRestoreService.CreatePreOptimizationRestorePoint()` |
| **Resumo** | `SystemDiagnosticsService.GetHardwareInfo()` + perfil de CPU |

Backup automatico e criado antes de qualquer otimizacao iniciada pela UI. Nao ha botao manual de backup.

### Modulos

| Botao | Metodo |
|-------|--------|
| CPU/Scheduler | `ApplyCpuSchedulerTweaks()` |
| GPU/Display | `ApplyGpuDisplayTweaks()` |
| Energia | `ApplyPowerTweaks()` |
| Latencia extrema | `ApplyExtremeLatencyTweaks()` |
| Input/USB | `ApplyInputTweaks()` |
| Rede | `ApplyNetworkTweaks()` |
| Politicas/Servicos | `ApplyPolicyAndServiceTweaks()` |
| GPU Windows | `ApplyGpuWindowsProfile()` |
| GPU regedit | `ApplyGpuDriverRegistryProfile()` |
| Background | `ApplyBackgroundTweaks()` |

### Telemetria

| Acao | Backend |
|------|---------|
| Iniciar/Parar teste A/B | `HardwareTelemetryService` + `EtwFrameTracker` |
| Grafico FPS / 1% low | Amostras via ETW e sensores LHM |
| Metricas DPC, boost, temperatura | `TelemetryMetricsSnapshot` |
| Console | Log da sessao em tempo real |

Sessoes salvas em `%LOCALAPPDATA%\ApexTweaker\Telemetry` (`Sessao_Baseline.json`, `Sessao_Optimized.json`).

### Cobblemon

| Acao | Backend |
|------|---------|
| Auditar pasta | `MinecraftAuditService` + `ModJarScanner` |
| Gerar relatorios | `MinecraftReportService` |
| Previsualizar perfil | `MinecraftProfileService.PlanProfile()` |
| Aplicar perfil | `MinecraftProfileService.ApplyProfile()` |
| Restaurar perfil | `MinecraftProfileService.RollbackLatest()` |
| Mover selecionados | `MinecraftQuarantineService.Apply()` |
| Desfazer quarentena | `MinecraftQuarantineService.RollbackLatest()` |
| Benchmark 60 s | `MinecraftBenchmarkService.CaptureAsync()` |

Essa aba nunca preseleciona nem exclui JARs. Movimentacao exige selecao e
confirmacao explicitas. Consulte `docs/COBBLEMON_LOW_END.md`.

### Utilidades

| Botao | Funcao |
|-------|--------|
| **Reverter** | `MasterRollbackService.ExecuteAsync()` — rollback LIFO dos snapshots pendentes |
| **Desinstalar e Sair** | Restaura ultimo estado, limpa `ProgramData\ApexTweaker`, encerra |
| **Sobre** | Versao, creditos, caminho de backups |
| **Suporte Riot** | Abre URL oficial de suporte do VALORANT |

---

## Diagnostico

Exibido no Dashboard (resumo) e no console ao iniciar:

- permissao de administrador
- versao/build do Windows
- arquitetura 64-bit
- CPU, nucleos fisicos/logicos, RAM
- perfil de arquitetura heterogenea (P/E cores)
- classificacao do PC: Low-End, Mid-Range ou High-End
- preset recomendado pelo `OptimizationEngine`
- deteccao de sistema ja otimizado

Relatorio completo disponivel via `SystemDiagnosticsService.BuildDiagnosticReport()` (Game Mode, Game DVR, VBS, Secure Boot, TPM, HAGS, MPO, etc.).

## OptimizationEngine

Camada leve de regras, sem IA:

- RAM menor que 16 GB **ou** CPU com menos de 6 nucleos fisicos: **Low-End**
- RAM de 16 GB ou mais, CPU com 8+ nucleos fisicos e processador recente: **High-End**
- demais casos: **Mid-Range**

Protecoes:

- **Low-End**: libera apenas preset seguro / Auto-Tuning conservador
- **Low-End**: bloqueia preset competitivo, extremo e latencia extrema
- **High-End**: libera preset competitivo e latencia extrema

Objetivo: evitar tweaks agressivos em maquinas sujeitas a superaquecimento, throttling ou instabilidade.

## Criar restore point

Executa `Checkpoint-Computer` via PowerShell.

Use antes de pacotes agressivos. Pode falhar sem Administrador ou com Restauracao do Sistema desativada.

## Backup e rollback

Diretorio: `C:\ProgramData\ApexTweaker\Backups`

O `BackupService` salva:

- plano de energia ativo
- valores de Registro alterados pelo app
- ajustes de Game Bar/Game DVR
- mouse/teclado
- scheduler/MMCSS
- HAGS
- snapshots de mutacao (`mutation-*.json`) para rollback transacional

**Reverter** (Utilidades) executa `MasterRollbackService` em ordem LIFO sobre snapshots pendentes no ledger.

Backup manual e criado automaticamente antes de cada otimizacao iniciada pela UI.

## Preset seguro

Ajustes conservadores:

- Game Mode
- Game DVR/captura desligados
- plano Alto desempenho (ou Ultimate Performance quando disponivel)
- fullscreen optimizations do VALORANT quando encontrado

## Preset competitivo

Aplica em sequencia:

- Energia
- CPU/Scheduler
- GPU/Display
- Input/USB
- Rede
- Background

Nao desativa idle states nem hibernacao.

## Preset extremo / Auto-Tuning

O **Auto-Tuning** do Dashboard escolhe o perfil ideal via `OptimizationEngine` e aplica o pacote correspondente.

Preset extremo executa em sequencia:

- Energia
- Latencia extrema
- CPU/Scheduler
- GPU/Display
- Input/USB
- Rede
- Background

Nao mexe no Vanguard, nao injeta codigo, nao aplica patch de kernel, nao desativa Defender e nao altera BCDEdit automaticamente no fluxo padrao.

## Perfil GPU

Detecta placas via `Win32_VideoController` e mostra recomendacoes por vendor:

- NVIDIA
- AMD/Radeon
- Intel/Arc

O app aplica automaticamente ajustes expostos pelo Windows (HAGS, Game Mode, Game DVR). Configuracoes do painel NVIDIA/AMD/Intel sao exibidas como checklist quando aplicavel — cada vendor usa APIs ou software proprietario.

## GPU regedit

Detecta adaptadores em:

`HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}`

Antes de alterar, exporta backup `.reg` para:

`C:\ProgramData\ApexTweaker\Backups`

**NVIDIA:**

- `PerfLevelSrc = 0x2222`
- `PowerMizerEnable = 0`
- `PowerMizerLevel = 0`
- `PowerMizerLevelAC = 0`
- `DisableDynamicPstate = 1`

**AMD/Radeon:**

- `EnableUlps = 0`
- `EnableUlps_NA = 0`
- `PP_SclkDeepSleepDisable = 1`

**MSI/Interrupt Policy:**

- localiza adaptadores via `Win32_PnPEntity`
- caminho `HKLM\SYSTEM\CurrentControlSet\Enum\...`
- `MSISupported = 1`
- `Interrupt Management\Affinity Policy\DevicePriority = 3` (High)
- exporta backup `.reg` antes da alteracao

**Intel/Arc:**

- nao aplica Registro automaticamente
- recomenda Intel Graphics Software / Low Latency Mode

Settings 3D do NVIDIA Control Panel ficam em perfis DRS/NVAPI, nao em chaves simples de regedit.

## Monitor VAL

Monitora processos do VALORANT em segundo plano:

- `VALORANT`
- `VALORANT-Win64-Shipping`

Quando detectado:

- handle com `PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_SET_INFORMATION`
- afinidade via `SetProcessAffinityMask` (com apoio de `ApexTweaker.Native.dll` em CPUs hibridas)
- prioridade `High` via `SetPriorityClass`
- nao usa `ntdll.dll!NtSetTimerResolution`

Em Intel hibrida, tenta usar P-cores via topologia nativa e `IntelHybridProbeStrategy`.

Se Vanguard ou o Windows negarem o handle, a tentativa e ignorada silenciosamente.

## Energia

Foco: reduzir economia de energia durante jogo.

Aplica:

- Ultimate Performance (com fallback para Alto desempenho)
- CPU minimo/maximo 100% na tomada
- politica de resfriamento ativa
- suspensao seletiva USB desligada na tomada

## Latencia extrema

Foco: perfil agressivo de energia para jogo.

No plano ativo:

- CPU boost mode `Aggressive`
- EPP em `0`
- CPU homogenea: core parking min/max 100%
- CPU heterogenea (Intel 12a gen+, Core Ultra): preserva Thread Director (`HETEROPOLICY=4`, `HETEROTHREAD=0`, `SCHEDPOLICY=2`)
- processor idle states desativados
- PCIe ASPM desligado
- disco/suspensao/hibernacao desligados na tomada
- `powercfg /hibernate off`

Aumenta consumo e temperatura. Pode melhorar input em alguns PCs e piorar throttle em outros.

Windows nao controla ring ratio, PL1, PL2 ou core current limit — ficam em BIOS/firmware.

Tambem aplica:

- `OverlayTestMode = 5` em DWM (MPO)
- backup automatico DWM/MMCSS
- ajustes MMCSS/DPC para `Tasks\Games`
- `NetworkThrottlingIndex = 0xffffffff`

## MPO/DWM

Via `ApplyMpoStabilityFix` / preset extremo.

Chave: `HKLM\SOFTWARE\Microsoft\Windows\Dwm` → `OverlayTestMode = 5`

Backup `.reg` em `C:\ProgramData\ApexTweaker\Backups`.

## CPU/Scheduler

Perfil MMCSS para jogos.

Em `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile`:

- `SystemResponsiveness = 0`
- `NetworkThrottlingIndex = 0xffffffff`

Em `Tasks\Games`:

- `GPU Priority = 8`
- `Priority = 6`
- `Scheduling Category = High`
- `SFIO Priority = High`

Exige Administrador; reinicio recomendado.

## GPU/Display

- HAGS com `HwSchMode = 2`
- Game Mode ativo
- Game DVR/captura desligados
- fullscreen optimizations do VALORANT quando encontrado

HAGS depende de Windows, GPU e driver.

## Input/USB

- remove aceleracao do mouse
- repeticao rapida do teclado
- suspensao seletiva USB desligada na tomada

## Rede

- `NetworkThrottlingIndex = 0xffffffff`
- `netsh int tcp set global rss=enabled`
- `netsh int tcp set global ecncapability=disabled`
- desativa `Interrupt Moderation`, `Green Ethernet` / `EEE` nos adaptadores encontrados

Backup `.reg` antes de alteracoes de driver de rede.

Implementado tambem via `NetworkInterruptModerationTweakCommand` no pipeline estruturado.

## Background

- desativa Game DVR e App Capture
- reduz paineis/startup do Game Bar

Nao remove Game Bar nem desativa Windows Defender.

## Politicas/Servicos

Modulo que aplica ajustes de servicos e politicas do Windows conforme catalogo do `TweakService`. Alteracoes sao condicionais e reversiveis via ledger.

## Reverter

Via **Utilidades → Reverter** (`MasterRollbackService`):

- restaura snapshots pendentes em ordem reversa (LIFO)
- cobre Registro, energia, BCD, servicos e estados capturados no ledger

Para reversao completa do sistema operacional, use tambem o restore point criado antes do preset.

## Pipeline transacional

Toda mutacao encapsulada em `MutationExecutor` segue:

```
Validate -> Snapshot -> Execute -> Verify/ReadBack -> Log
```

Nenhuma operacao deve reportar sucesso sem read-back do estado real do Windows.

## Telemetria

- **HardwareTelemetryService**: sensores via LibreHardwareMonitor, snapshots periodicos
- **EtwFrameTracker**: frametime via ETW (`Microsoft-Windows-DxgKrnl`)
- Degradacao graciosa: falta de sensor ou privilegio nao deve crashar o app
- ETW filtra PID do jogo e descarta ruido de DWM/overlays

## Comandos estruturados adicionais

| Comando | Funcao |
|---------|--------|
| `ProcessorIdleStatesTweakCommand` | Idle states do processador |
| `EdgeRemovalTweakCommand` | Remocao do Edge (modulo avancado) |
| `NetworkInterruptModerationTweakCommand` | Moderacao de interrupcao de rede |
| `MemoryCompressionTweakCommand` | Compressao de memoria |
| `ExtremeMutationCommands` | Hypervisor, timer resolution, MPO, MSI Mode |

## Divida tecnica conhecida

- Telemetria e console compartilham a mesma view; a aba Telemetria e instanciada sob demanda na primeira escrita de log.
- Alguns servicos ainda usam strings legadas em comentarios internos; paths publicos usam `ApexTweaker`.
