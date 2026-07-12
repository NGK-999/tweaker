# Contexto completo do ApexTweaker

Atualizado em: 2026-07-12
Objetivo: documento de continuidade para outro agente ou nova conversa.  
Regra: fatos marcados como **confirmados** foram verificados no repositorio local. Itens marcados como **solicitados** vieram do historico, mas nao devem ser tratados como implementados sem auditoria de codigo e teste no Windows.

## 1. Identidade do produto

- Nome: ApexTweaker.
- Autor/empresa: Igor Silva.
- Versao atual declarada: `2.1.0`.
- Plataforma: Windows 10/11, com foco atual em Windows 11.
- Framework atual: `.NET 10`, destino `net10.0-windows`.
- Objetivo tecnico: otimizar estabilidade de frametime e 1% low, reduzir stutters e oferecer telemetria, backup e rollback.
- Prioridade declarada: estabilidade e reversibilidade antes de ganho de FPS maximo.
- O aplicativo exige privilegios administrativos por manifesto para mutacoes de sistema.

## 2. Caminhos e repositorio

### Caminho canonico atual

- Codigo real: `C:\Apextweaker`.
- Workspace antigo/auxiliar: `C:\VSCODE`.
- Este documento foi criado em `C:\VSCODE\CONTEXTO_COMPLETO_APEXTWEAKER.md`.

### Git

- Repositorio: `https://github.com/NGK-999/tweaker.git`.
- Branch: `main`.
- Estado local confirmado em 2026-06-29: limpo e alinhado a `origin/main`.
- Commit local mais recente confirmado: `1a0ca1a`.
- Mensagem: `feat: ApexTweaker v2.0.1 - initial commit with WPF UI, transactional pipeline, native C++ topology, and telemetry system`.
- O repositorio em `C:\Apextweaker` possui ownership diferente do usuario sandbox; comandos Git automatizados precisam usar `-c safe.directory=C:/Apextweaker`.

## 3. Build e distribuicao

### Projeto principal

- Solucao: `C:\Apextweaker\ApexTweaker.sln`.
- Projeto: `C:\Apextweaker\ApexTweaker.csproj`.
- Assembly: `ApexTweaker`.
- UI habilitada no mesmo projeto: WinForms e WPF (`UseWindowsForms=true`, `UseWPF=true`).
- Publicacao: single-file comprimido, ReadyToRun, bibliotecas nativas extraidas.
- Trimming permanece desativado por incompatibilidade anterior com UI/reflection.

### Dependencias confirmadas

- `LibreHardwareMonitorLib 0.9.6`.
- `Microsoft.Diagnostics.Tracing.TraceEvent 3.1.21`.
- `System.Management 10.0.2`.
- `LiveChartsCore.SkiaSharpView.WinForms 2.0.0-rc2`.
- `ReaLTaiizor 3.8.1.8`.
- DLL nativa C++: `ApexTweaker.Native.dll`.

### Artefatos locais confirmados

- Portatil antigo/oficial atual: `C:\Apextweaker\release-v2\ApexTweaker.exe`.
  - Data: 2026-06-24 15:00.
  - Tamanho: 106.571.584 bytes.
- Staging mais novo: `C:\Apextweaker\release-v2-staging\ApexTweaker.exe`.
  - Data: 2026-06-25 06:25.
  - Tamanho: 106.574.616 bytes.
- Instalador local: `C:\Apextweaker\release-installer\ApexTweaker-Setup.exe`.
  - Data: 2026-06-24 10:49.
  - Tamanho: 75.050.916 bytes.
- A publicacao para `release-v2` falhou anteriormente porque uma instancia elevada de `ApexTweaker.exe` manteve o arquivo bloqueado.
- Consequencia: o usuario provavelmente testou um binario antigo mesmo depois de correcoes no fonte.

### Comandos de validacao

```powershell
dotnet build C:\Apextweaker\ApexTweaker.sln -c Release
dotnet publish C:\Apextweaker\ApexTweaker.csproj -c Release -r win-x64 --self-contained true -o C:\Apextweaker\release-v2
```

Antes de substituir `release-v2`, fechar toda instancia do ApexTweaker e comparar hash/data dos executaveis.

## 4. Arquitetura atual confirmada

### Inicializacao

Arquivo: `src/App/Program.cs`.

Fluxo atual:

1. Registra handlers globais de excecao.
2. Cria `System.Windows.Application`.
3. Exibe `StartupDisclaimerWindow` como dialogo.
4. Exibe `LoadingWindow` como dialogo.
5. Cria e executa `MainWindow` WPF.

### UI WPF ativa

Arquivos principais:

- `src/UI/Wpf/MainWindow.xaml`.
- `src/UI/Wpf/MainWindow.xaml.cs`.
- `src/UI/Wpf/Views/DashboardView.xaml`.
- `src/UI/Wpf/Views/ModulesView.xaml`.
- `src/UI/Wpf/Views/TelemetryView.xaml`.
- `src/UI/Wpf/Views/UtilitiesView.xaml`.
- `src/UI/Wpf/Animations/PageTransitionAnimator.cs`.

O `MainWindow` mantem instancias unicas das quatro views em um dicionario e usa um `ContentControl` como host de paginas.

### UI WinForms legada ainda presente

Arquivos como `ValorantTweakerForm.cs`, `ConsoleControl.cs`, `UiAnimator.cs`, `RoundedButton.cs`, `GamerCard.cs` e componentes ReaLTaiizor ainda existem. Eles nao representam a shell ativa iniciada por `Program.cs`, mas continuam compilados no mesmo assembly.

Isso cria divida tecnica:

- duas stacks de UI no mesmo executavel;
- dependencias WinForms que podem nao ser usadas pela shell WPF;
- risco de corrigir a tela errada;
- documentacao antiga ainda afirma que o app principal e WinForms.

### Backend e mutacoes

Componentes confirmados:

- `TweakService`: fachada atual para presets e modulos.
- `OptimizationEngine`: analise de hardware e decisao de perfil.
- `GpuOptimizationService`: gera planos de mutacao de GPU.
- `RegistryService`: operacoes de Registro.
- `CommandRunner`: execucao de processos externos.
- `MutationExecutor`: pipeline central de mutacoes.
- `BackupService`: snapshots e ledger.
- `MasterRollbackService`: rollback transacional.
- `SystemRestoreService`: ponto de restauracao.
- comandos estruturados em `src/Core/Pipeline` e `src/Services`.

Contrato pretendido para toda mutacao:

`Validate -> Snapshot -> Execute -> Verify/ReadBack -> Log`

Regra de seguranca: nenhuma mutacao deve retornar sucesso apenas porque um comando terminou com exit code zero. O estado real deve ser relido e comparado.

### Ledger e rollback

- Diretorio de dados: `C:\ProgramData\ApexTweaker`.
- Backups: `C:\ProgramData\ApexTweaker\Backups`.
- Logs: `C:\ProgramData\ApexTweaker\Logs\latest_runtime.log`.
- O `BackupService` possui captura de Registro, BCD, energia, servico, processo e estado de comando.
- O rollback mestre deve consumir snapshots pendentes em ordem LIFO.
- O fluxo de desinstalacao deve parar telemetria, restaurar mutacoes e somente depois remover dados/binarios.

### Telemetria

- `HardwareTelemetryService`: sensores e snapshots.
- `EtwFrameTracker`: frametime via ETW.
- `TelemetryPipeServer` e `TelemetryPipeClient`: IPC planejado/implementado no mesmo repositorio.
- `IntelHybridProbeStrategy`: filtragem de P-Cores.
- DLL nativa C++: topologia e afinidade.
- Meta: telemetria parcial em caso de falta de sensor ou privilegio, nunca crash.
- ETW deve usar timestamps nativos do evento, filtrar PID do jogo e descartar ruido de DWM/overlays.

## 5. Catalogo funcional existente por API

Os seguintes metodos publicos existem em `TweakService`; isso confirma superficie de codigo, nao eficacia em todas as versoes do Windows:

- Preset maximo, competitivo, seguro e autonomo.
- Energia e Ultimate Performance com fallback.
- CPU/Scheduler e arquitetura de CPU.
- Latencia extrema.
- Fullscreen Exclusive/Game DVR.
- Quantum do scheduler.
- Controles BCD de timer.
- Memoria de kernel.
- MPO.
- DPC/latencia avancada.
- GPU/Display, GPU Windows e GPU Registry.
- Input/USB.
- Rede e moderacao de interrupcao/Green Ethernet.
- Background.
- Politicas e servicos.
- Remocao do Edge.
- Hypervisor off.
- Timer resolution/useplatformclock.
- Compressao de memoria.
- MSI Mode de GPU.
- Afinidade Ryzen/VALORANT.
- Reversao do ultimo estado aplicado.

Comandos estruturados confirmados incluem:

- `ProcessorIdleStatesTweakCommand`.
- `EdgeRemovalTweakCommand`.
- `NetworkInterruptModerationTweakCommand`.
- `MemoryCompressionTweakCommand`.
- comandos extremos em `ExtremeMutationCommands.cs`.

## 6. Catalogo historico solicitado

Estes itens foram solicitados ao longo do projeto. Cada um precisa ser auditado no codigo e testado antes de ser anunciado como funcional:

### CPU, scheduler e memoria

- `Win32PrioritySeparation = 0x26`.
- `DisablePagingExecutive = 1`.
- `LargeSystemCache = 1` condicionado a RAM.
- `IoPageLimit = 1048576`.
- MMCSS Games: categoria High e prioridade 6.
- Game Mode ativo em CPUs hibridas/multi-CCD.
- Politicas heterogeneas para Intel P/E cores sem desativar Core Parking.
- Core Parking agressivo apenas em CPU homogenea/legacy.
- Boost termico condicionado a temperatura historica.
- desativacao de compressao de memoria, marcada como critica.
- monitor/limpeza de standby memory solicitado, mas de alto risco e sujeito a revisao.

### GPU e display

- MPO/`OverlayTestMode = 5`.
- HAGS/Game Mode/Game DVR conforme compatibilidade.
- MSI Mode e prioridade de interrupcao.
- NVIDIA PowerMizer/`DisableDynamicPstate`.
- AMD `EnableUlps = 0`.
- prioridade DWM.
- FSE/GameConfigStore.
- deteccao de refresh rate via `EnumDisplaySettings`.

### Energia e BCD

- Ultimate Performance com verificacao de GUID e fallback.
- EPP/boost no plano ativo para hardware moderno.
- `disabledynamictick` e `useplatformclock` foram solicitados, mas exigem cautela e rollback exato.
- `hypervisorlaunchtype off` foi solicitado como comando critico.
- USB selective suspend por GUID absoluto.

### Rede, input e servicos

- desativacao de aceleracao do mouse.
- moderacao de interrupcao, EEE e Green Ethernet.
- Delivery Optimization.
- servicos Xbox, localizacao, Bluetooth, biometria, smart card, SysMain, sensores, WER, Gaming Services, WSearch e UvfsService foram solicitados.
- Prefetch/Superfetch e `SvcHostSplitThresholdInKB` foram solicitados.

Observacao tecnica: desativar servicos em massa nao e uma otimizacao universal. Bluetooth, biometria, busca, Xbox/Gaming Services e Smart Card devem ser condicionais e reversiveis; desativacao cega quebra recursos reais.

### Debloat, UX e hardcore

- Cortana e sugestoes do Explorer.
- apps UWP em background.
- telemetria/WER.
- menu de contexto classico.
- Explorer em Meu Computador, extensoes e arquivos ocultos.
- botao Finalizar Tarefa.
- remocao de Appx/Edge.
- CompactOS.
- UAC e BitLocker foram solicitados como tweaks criticos.

Observacao tecnica: desativar UAC, BitLocker, VBS/Hyper-V ou protecoes do Defender reduz seguranca e nao deve integrar o Auto-Tuning padrao. Exige consentimento explicito, snapshot verificavel e aviso de reinicializacao/risco.

## 7. Requisitos de produto consolidados

- UI passiva: nenhuma mutacao de Registro/BCD dentro da view.
- Nao bloquear a thread da UI.
- `BeginInvoke`/Dispatcher apenas para atualizacao visual.
- `Task.Run` somente para trabalho sincrono pesado; I/O deve ser realmente assincrono quando possivel.
- Nenhum `Thread.Sleep`, `.Wait()` ou `.Result` na UI.
- Controle de corrida com `isTweaking` e desativacao de comandos concorrentes.
- Backup automatico antes de qualquer otimizacao; botao manual de backup removido.
- Manter apenas o botao de restauracao na UI.
- Toda mutacao precisa ser idempotente ou verificar estado antes de repetir.
- Nao manipular memoria do jogo, nao injetar DLL e nao usar hooks agressivos contra anti-cheat.
- Handles de processo devem ser de privilegio minimo e falhas por Vanguard/EAC devem ser tratadas sem crash.
- Persistir log da sessao em ProgramData.
- Fechamento deve dispor ETW, sensores, timers, CTS, processos e conexoes.

## 8. Design e UX desejados

- Tema escuro, solido e coerente.
- Sidebar minimalista com estado ativo claro.
- Cards com cantos arredondados e espacamento consistente.
- Evitar transparencia falsa/ARGB complexa em WinForms.
- WPF foi introduzido para resolver limitacoes recorrentes de GDI+/RichTextBox e animacoes WinForms.
- Animacoes devem ser sutis, cancelaveis e nunca reparentear controles vivos de forma insegura.
- Console deve ser legivel, com buffer limitado, sem duplicacao e com fundo opaco.
- Strings em portugues devem ser UTF-8 e nao podem aparecer como mojibake.

## 9. Bugs bloqueadores confirmados no fonte atual

### 9.1 Crash de navegacao WPF

Erro observado:

`O elemento especificado ja e o filho logico de outro elemento. Desconecte-o primeiro.`

Stack principal:

- `FrameworkElement.ChangeLogicalParent`.
- `UIElementCollection.AddInternal`.
- `PageTransitionAnimator.ShowAsync`.
- `MainWindow.ShowPageAsync`.

Causa confirmada no desenho atual:

- as views sao instancias cacheadas;
- o animador remove o conteudo do host;
- cria um `Grid` temporario;
- tenta adicionar `outgoing` e `incoming` ao mesmo stage;
- WPF ainda pode considerar uma view ligada a outro pai logico, mesmo quando o pai visual parece removido.

Correcao arquitetural recomendada:

- nao colocar duas views vivas cacheadas em um `Grid` temporario;
- animar a pagina atual para fora ainda dentro do `ContentControl`;
- definir `host.Content = null`;
- colocar apenas a nova view;
- animar a nova view para dentro;
- ou usar snapshot visual (`VisualBrush`/bitmap) para animar a pagina antiga.

### 9.2 Crash ao fechar a janela

Erro observado:

`Nao sera possivel definir Visibility como Visible nem chamar Show, ShowDialog, Close ou EnsureHandle enquanto uma Janela estiver sendo fechada.`

Causa no fonte atual:

- handler `Closing` e `async void`;
- cancela o fechamento;
- aguarda teardown;
- agenda `Close()` enquanto o ciclo de fechamento/reentrada ainda pode estar ativo;
- o handler permanece inscrito e pode ser reentrante.

Correcao recomendada:

- o handler `Closing` apenas cancela uma vez e agenda `ShutdownAndCloseAsync` depois de retornar;
- ao terminar, remover a inscricao `Closing -= MainWindow_OnClosing`;
- marcar estado final;
- chamar `Close()` uma unica vez no Dispatcher.

### 9.3 Mojibake/encoding ainda presente

O fonte atual contem strings como:

- `jï¿½ estï¿½`.
- `Latï¿½ncia extrema`.
- `Polï¿½ticas/Serviï¿½os`.
- `Sessï¿½o`.

As capturas mostram `MÃ³dulos`, `SeguranÃ§a` e simbolos quebrados.

Correcao recomendada:

- substituir strings corrompidas por portugues valido;
- salvar `.cs` e `.xaml` como UTF-8;
- usar escapes `\uXXXX` em strings C# sensiveis e entidades XML/glifos validos em XAML;
- fazer busca por `Ã`, `Â`, `ï¿½`, `â€` e pelo caractere U+FFFD antes do build.

### 9.4 Binario testado possivelmente obsoleto

- `release-v2` e o instalador sao anteriores ao staging mais novo.
- A aplicacao elevada ficou bloqueando substituicao do executavel.
- Nenhuma correcao pode ser considerada testada ate confirmar o caminho do processo em execucao e o hash do binario.

### 9.5 Documentacao obsoleta

`docs/PROJECT_STRUCTURE.md` ainda descreve:

- app principal WinForms;
- `TweakManager.cs`, que nao aparece na arvore atual;
- modelos que nao constam na arvore atual.

README tambem chama o produto de Windows Forms, embora o entry point atual abra WPF.

## 10. Estado de verificacao conhecido

- Um build Release foi concluido com sucesso apos patches WPF anteriores.
- Permaneceram warnings `NU1701` relacionados a pacotes OpenTK restaurados para .NET Framework.
- O publish para `release-v2-staging` foi concluido.
- Nao ha confirmacao de que o binario de staging foi executado e validado visualmente.
- Nao ha confirmacao de que o instalador atual contem o build WPF corrigido.
- O Git local esta limpo, portanto correcoes posteriores ao commit podem nao estar presentes ou foram perdidas; comparar fonte e artefatos antes de publicar.

## 11. Ordem de trabalho recomendada

1. Confirmar o caminho do `ApexTweaker.exe` em execucao.
2. Reescrever `PageTransitionAnimator` sem stage com duas views vivas.
3. Reescrever o fechamento para eliminar reentrada de `Close()`.
4. Corrigir todo mojibake em `.cs` e `.xaml`.
5. Executar build Release.
6. Publicar em uma pasta nova e executar diretamente essa copia.
7. Testar navegacao rapida entre as quatro abas por pelo menos 100 trocas.
8. Testar fechar durante animacao, telemetria e mutacao em andamento.
9. Testar disclaimer, loading, maximize/minimize e DPI 100/125/150%.
10. Somente depois substituir `release-v2`, reconstruir instalador e criar release GitHub.
11. Atualizar README e `PROJECT_STRUCTURE.md` para refletir WPF e o pipeline real.

## 12. Matriz minima de testes antes da proxima release

- Windows 11 23H2/24H2/25H2 quando disponivel.
- CPU Intel hibrida, Intel classica e AMD Ryzen.
- GPU NVIDIA, AMD e iGPU Intel.
- Execucao com e sem sensores LHM disponiveis.
- ETW com administrador e modo degradado sem acesso.
- Jogo inexistente e jogo em execucao.
- clique duplo/rapido em navegacao e otimizacoes.
- fechamento durante telemetria.
- rollback sem snapshots e com varios snapshots.
- instalacao limpa, opcao abrir ao concluir e desinstalacao com rollback.
- comparacao A/B preservando sessoes ativas.
- log em disco e console apos muitas linhas.
- DPI, resolucao pequena e maximizada.

## 13. Criterios para declarar uma otimizacao valida

Uma otimizacao so pode ser anunciada como aplicada quando:

1. O hardware/Windows suporta a alteracao.
2. O estado anterior foi capturado.
3. A mutacao foi executada sem erro.
4. O estado foi relido do Windows.
5. O valor relido corresponde ao esperado.
6. O ledger foi persistido.
7. O rollback foi testado.
8. O efeito foi medido em benchmark A/B, nao apenas inferido.

## 14. Regras de colaboracao para proximos agentes

- Ser direto e tecnicamente imparcial.
- Nao vender tweaks placebo.
- Nao entregar blocos genericos quando o pedido for analise.
- Ler o repositorio antes de editar.
- Preservar mudancas do usuario.
- Usar `apply_patch` para edicoes manuais.
- Fazer build e verificacao antes de afirmar que resolveu.
- Atualizar o artefato de distribuicao somente depois de fechar o processo que o bloqueia.
- Nao fazer push sem o pedido explicito do usuario.
- Quando houver push, incluir apenas arquivos relacionados e informar commit/hash.

## 15. Prompt curto para retomar o trabalho

```text
Continue o ApexTweaker em C:\Apextweaker usando CONTEXTO_COMPLETO_APEXTWEAKER.md como handoff. Nao trate pedidos historicos como implementados sem verificar o codigo. Primeiro corrija os tres bloqueadores WPF confirmados: (1) PageTransitionAnimator nao pode adicionar views cacheadas vivas a um Grid temporario; use transicao sequencial ou snapshot, (2) MainWindow_OnClosing nao pode chamar Close de forma reentrante; desinscreva Closing antes do fechamento final, (3) remova todo mojibake dos arquivos .cs/.xaml e salve UTF-8. Depois execute build Release, publique em uma pasta nova, confirme o hash e teste exatamente o novo executavel. Nao substitua release-v2 enquanto ApexTweaker.exe estiver em execucao.
```

## 16. Minecraft/Cobblemon 2.1.0

- A shell WPF possui a pagina `MinecraftView`, exibida como **Cobblemon**.
- O modulo em `src/Minecraft` audita JARs, inclusive dependencias aninhadas, sem escrever na pasta de mods.
- Relatorios sao gerados em JSON, Markdown e TXT, com plano separado de sugestoes de quarentena.
- Os perfis alteram apenas `options.txt` e um TXT de argumentos JVM depois de backup proprio.
- Backups Minecraft ficam em `C:\ProgramData\ApexTweaker\MinecraftBackups`.
- O rollback rejeita caminhos fora da instancia e nunca manipula JARs.
- A pasta auditada em julho de 2026 tinha 88 mods, duplicidade de `mega_showdown` e colisao entre Sodium 0.6.13 e Indium separado.
- `--minecraft-self-test` valida scanner, relatorios, perfil, rollback e carregamento XAML.
- Detalhes operacionais: `docs/COBBLEMON_LOW_END.md`.
