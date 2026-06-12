# Botoes do Valorant Tweaker

O objetivo do app e otimizar o Windows para jogos, principalmente VALORANT, mantendo as alteracoes rastreaveis e reversiveis.

## Diagnosticar

Mostra informacoes do sistema:

- permissao de administrador
- versao/build do Windows
- arquitetura 64-bit
- Game Mode
- Game DVR
- VBS configurado
- Secure Boot
- TPM
- caminho do executavel do VALORANT
- fullscreen optimizations do VALORANT
- classificacao do PC: Low-End, Mid-Range ou High-End
- preset recomendado pelo `OptimizationEngine`

## OptimizationEngine

O app usa uma camada leve de regras, sem IA pesada:

- RAM menor que 16 GB ou CPU com menos de 6 nucleos fisicos: `Low-End`
- RAM de 16 GB ou mais, CPU com 8+ nucleos fisicos e processador recente: `High-End`
- demais casos: `Mid-Range`

Protecoes:

- `Low-End`: libera apenas `Preset seguro`
- `Low-End`: bloqueia `Preset competitivo`, `Preset extremo` e `Latencia extrema`
- `High-End`: libera `Preset competitivo` e `Latencia extrema`

Objetivo: evitar aplicar tweaks agressivos em maquinas que podem sofrer com superaquecimento, throttling ou instabilidade.

## Criar restore point

Executa `Checkpoint-Computer` pelo PowerShell.

Use antes de qualquer preset agressivo. Pode falhar se o app nao estiver como Administrador ou se a Restauracao do Sistema estiver desativada.

## Backup

Cria backup granular em:

`C:\ProgramData\ValorantTweaker\Backups`

Salva:

- plano de energia ativo
- valores de Registro alterados pelo app
- ajustes de Game Bar/Game DVR
- mouse/teclado
- scheduler/MMCSS
- HAGS

O botao Reverter usa o backup mais recente.

## Preset seguro

Aplica ajustes conservadores:

- Game Mode
- Game DVR/captura desligados
- plano Alto desempenho
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

## Preset extremo

Executa em sequencia:

- Energia
- Latencia extrema
- CPU/Scheduler
- GPU/Display
- Input/USB
- Rede
- Background

Nao mexe no Vanguard, nao injeta codigo, nao aplica patch de kernel, nao desativa Defender e nao altera BCDEdit automaticamente.

## Perfil GPU

Detecta placas via `Win32_VideoController` e mostra recomendacoes por vendor:

- NVIDIA
- AMD/Radeon
- Intel/Arc

O app aplica automaticamente apenas ajustes expostos pelo Windows, como HAGS/Game Mode/Game DVR. Configuracoes do painel NVIDIA/AMD/Intel sao exibidas como checklist porque cada vendor usa APIs, banco de dados ou software proprietario.

## GPU regedit

Detecta adaptadores na classe:

`HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}`

Antes de alterar, exporta backup `.reg` para:

`C:\ProgramData\ValorantTweaker\Backups`

NVIDIA:

- `PerfLevelSrc = 0x2222`
- `PowerMizerEnable = 0`
- `PowerMizerLevel = 0`
- `PowerMizerLevelAC = 0`
- `DisableDynamicPstate = 1`

AMD/Radeon:

- `EnableUlps = 0`
- `EnableUlps_NA = 0`
- `PP_SclkDeepSleepDisable = 1`

MSI/Interrupt Policy:

- localiza adaptadores de video via `Win32_PnPEntity`
- usa o caminho `HKLM\SYSTEM\CurrentControlSet\Enum\...`
- aplica `MSISupported = 1`
- aplica `Interrupt Management\Affinity Policy\DevicePriority = 3` (High)
- exporta backup `.reg` do dispositivo antes da alteracao

Intel/Arc:

- nao aplica Registro automaticamente
- recomenda Intel Graphics Software / Low Latency Mode

Observacao: settings 3D do NVIDIA Control Panel ficam em perfis DRS/NVAPI, nao em chaves simples e estaveis de regedit. Por isso o app aplica via Registro apenas ajustes de power-state/driver que sao rastreaveis.

## Monitor VAL

Monitora processos do VALORANT em segundo plano usando enumeração simples por nome e acesso nativo de baixo privilégio.

Processos observados:

- `VALORANT`
- `VALORANT-Win64-Shipping`

Quando detectado:

- solicita handle com `PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_SET_INFORMATION`
- tenta aplicar afinidade via `SetProcessAffinityMask`
- tenta definir prioridade `High` via `SetPriorityClass`
- nao usa `ntdll.dll!NtSetTimerResolution`

Em Intel de nova geracao com arquitetura hibrida, o app tenta usar apenas os primeiros nucleos logicos estimados como P-cores, evitando E-cores. Essa estimativa usa os dados de nucleos fisicos/logicos coletados via WMI.

Se o Windows, Vanguard ou outro anti-cheat negarem o handle, o app ignora silenciosamente aquela tentativa e continua funcionando. O jogo continua responsavel pelo timer de plataforma.

## Energia

Foco: reduzir economia de energia durante jogo.

Aplica:

- tenta liberar e ativar Ultimate Performance
- se falhar, ativa Alto desempenho
- CPU minimo em 100% na tomada
- CPU maximo em 100% na tomada
- politica de resfriamento ativa
- suspensao seletiva USB desligada na tomada

## Latencia extrema

Foco: aproximar pelo Windows o comportamento de uma BIOS agressiva para jogo.

Aplica no plano de energia atual:

- CPU boost mode em `Aggressive`
- EPP em `0`, preferencia total por desempenho
- em CPU homogenea/legacy: core parking minimo em `100%` e core parking maximo em `100%`
- em CPU heterogenea (Intel 12a geracao+, Core Ultra, P-Cores/E-Cores): nao altera Core Parking; aplica `HETEROPOLICY=4`, `HETEROTHREAD=0` e `SCHEDPOLICY=2` para preservar o Thread Director
- processor idle states desativados
- PCIe ASPM desligado
- disco sem desligamento automatico na tomada
- suspensao automatica desligada na tomada
- hibernacao automatica desligada
- `powercfg /hibernate off`

Isso aumenta consumo, temperatura e ruido. Pode melhorar feeling/input em alguns PCs e piorar temperatura/throttle em outros.

O Windows nao controla diretamente ring ratio, PL1, PL2 ou core current limit. Esses valores ficam em BIOS/UEFI, firmware ou ferramentas com driver proprio.

Tambem aplica correcoes avancadas:

- `OverlayTestMode = 5` em `HKLM\SOFTWARE\Microsoft\Windows\Dwm`
- backup automatico das chaves DWM/MMCSS antes de alterar
- ajustes MMCSS/DPC para `Tasks\Games`
- `NetworkThrottlingIndex = 0xffffffff`

## MPO/DWM

Aplicado pelo preset extremo via `ApplyMpoStabilityFix`.

Chave:

`HKLM\SOFTWARE\Microsoft\Windows\Dwm`

Valor:

- `OverlayTestMode = 5`

Antes de alterar, exporta backup `.reg` para:

`C:\ProgramData\ValorantTweaker\Backups`

## CPU/Scheduler

Foco: perfil multimidia/MMCSS para jogos.

Aplica em `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile`:

- `SystemResponsiveness = 0`
- `NetworkThrottlingIndex = 0xffffffff`

Aplica em `Tasks\Games`:

- `GPU Priority = 8`
- `Priority = 6`
- `Scheduling Category = High`
- `SFIO Priority = High`

Exige Administrador e normalmente pede reinicio.

## GPU/Display

Foco: recursos graficos e captura em segundo plano.

Aplica:

- solicita HAGS com `HwSchMode = 2`
- ativa Game Mode
- desativa Game DVR/captura em segundo plano
- desativa fullscreen optimizations no executavel do VALORANT quando encontrado

HAGS depende de Windows, GPU e driver. Pode nao aparecer em todo hardware.

## Input/USB

Foco: resposta de mouse, teclado e USB.

Aplica:

- remove aceleracao do mouse
- configura repeticao rapida do teclado
- desativa suspensao seletiva USB na tomada

## Rede

Foco: baixa latencia sem quebrar o TCP moderno do Windows.

Aplica:

- `NetworkThrottlingIndex = 0xffffffff`
- `netsh int tcp set global rss=enabled`
- `netsh int tcp set global ecncapability=disabled`
- varre adaptadores em `HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}`
- desativa parametros existentes de `Interrupt Moderation`
- desativa parametros existentes de `Green Ethernet` / `EEE`

Antes das alteracoes de driver de rede, exporta backup `.reg` para:

`C:\ProgramData\ValorantTweaker\Backups`

## Background

Foco: reduzir capturas e paineis em segundo plano.

Aplica:

- desativa Game DVR
- desativa App Capture
- reduz paineis/startup do Game Bar

Nao remove Game Bar e nao desativa Windows Defender.

## Reverter

Tenta voltar:

- Game Mode/Game DVR para valores comuns
- fullscreen optimizations do VALORANT
- mouse para padrao comum
- HAGS para decisao do Windows/driver
- plano de energia Equilibrado

Para reverter tudo com garantia, use tambem o restore point criado antes do preset.
