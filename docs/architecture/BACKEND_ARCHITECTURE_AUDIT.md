# Auditoria estrutural do backend

Data da auditoria: 2026-07-24

## Escopo e restricoes

Esta auditoria cobre somente o backend C#/.NET e a integracao nativa existente.
WPF, estilos, paginas e uma futura interface React/WebView2 nao fazem parte
desta reorganizacao.

Nenhum comando de mutacao do Windows deve ser executado durante a refatoracao.
As validacoes ficam limitadas a compilacao e self-tests sinteticos.

## Estrutura observada

```text
ApexTweaker.csproj                 host WPF e todo o backend C#
native/ApexTweaker.Native          DLL C++ de topologia e afinidade
src/App                            entrada e caminhos da aplicacao
src/Core/Pipeline                  comandos mutaveis do Windows
src/Infrastructure                execucao de processos e modo demo
src/Minecraft                     contratos e casos de uso Minecraft
src/Models                        contratos compartilhados
src/NativeInterop                 P/Invoke da DLL nativa e da UI
src/Services                      aplicacao e infraestrutura misturadas
src/UI                            host e telas WPF
```

O ponto de entrada e `src/App/Program.cs`. Nao existem servidor HTTP,
controllers, rotas ou banco de dados. O host WPF instancia servicos diretamente.
A telemetria entre processos usa named pipe.

Os testes existentes sao self-tests executados por
`--minecraft-self-test`. Tambem existe o teste de seguranca do modo demo,
acionado por `--demo-self-test`.

## Problemas encontrados

### Responsabilidades misturadas

- `TweakService` combina selecao de preset, orquestracao, comandos, verificacao,
  energia, Registro e servicos.
- `BackupService` combina captura, persistencia, geracao de arquivos de restore
  e restauracao de Registro, BCD, servicos e energia.
- `HardwareTelemetryService` combina coleta, persistencia, benchmark, ETW,
  historico e varios contratos de telemetria.
- `OptimizationEngine` combina classificacao de hardware, leitura do Registro e
  consulta ao historico de telemetria.

### Infraestrutura Windows espalhada

Uso de Registro, PowerShell, `powercfg`, `bcdedit` e `sc.exe` existe em
`Services`, `Core/Pipeline` e `Infrastructure`. `CommandRunner` centraliza a
criacao de processos, mas recebe executavel e argumentos como texto. O modo
demo usa classificacao fail-safe e bloqueia comandos desconhecidos.

### Modelos sobrepostos

O fluxo legado usa `HardwareInfo`, `PresetKind`, `PresetRecommendation` e
`OptimizationEngine`. O fluxo novo usa `WindowsOptimizationContext`,
`WindowsOptimizationPreset`, `WindowsOptimizationPlan` e um catalogo tipado.
Os conceitos sao relacionados, mas ainda nao sao equivalentes; fundi-los agora
mudaria comportamento e regras existentes.

### Acoplamento

O projeto C# unico nao impede referencias da UI para infraestrutura. Uma divisao
imediata de todo o legado em assemblies tambem produziria ciclos: recomendacao
consulta telemetria e Registro, enquanto a orquestracao de tweaks depende de
backup, execucao e comandos concretos.

### Codigo grande e duplicacao

Os maiores pontos de risco observados sao:

- `HardwareTelemetryService`: mais de 1.800 linhas;
- `BackupService`: mais de 1.200 linhas;
- `TweakService`: mais de 1.100 linhas;
- `MinecraftProfileService`: mais de 1.200 linhas, embora ja esteja isolado no
  modulo Minecraft.

Nao foi encontrado modulo React dentro do backend. O WPF esta em `src/UI`, mas
compartilha o mesmo assembly do backend.

## Estrutura proposta

Primeira etapa:

```text
src/
  ApexTweaker.Contracts/
    Inventory/
    Optimizations/
  ApexTweaker.Application/
    Optimizations/
  ApexTweaker.Windows/
    Inventory/
  App/
    WindowsOptimizationService.cs
```

O fluxo de dependencia permitido nesta etapa e:

```text
ApexTweaker.Contracts
        ^
        |
ApexTweaker.Application
        ^
        |
ApexTweaker (composition root) -> ApexTweaker.Windows
                                      |
                                      v
                             ApexTweaker.Contracts
```

`Application` nao referencia `Windows`. A fachada no executavel compoe a
implementacao concreta de inventario com o motor de recomendacao.

Etapas posteriores:

1. Extrair contratos do pipeline de mutacao sem mover implementacoes.
2. Dividir `BackupService` em captura, armazenamento e rollback.
3. Mover Registro, energia, BCD, servicos e processos para
   `ApexTweaker.Windows`.
4. Quebrar `TweakService` em casos de uso por preset/operacao.
5. Extrair telemetria e benchmark sem alterar seus formatos persistidos.
6. Criar o host `ApexTweaker.Desktop` quando a migracao WPF/WebView2 entrar no
   escopo.

## Arquivos da primeira etapa

Movidos:

- `src/Models/GpuInfo.cs` para
  `src/ApexTweaker.Contracts/Inventory/GpuInfo.cs`;
- `src/Models/WindowsOptimizationModels.cs` para
  `src/ApexTweaker.Contracts/Optimizations/WindowsOptimizationModels.cs`;
- `src/Services/WindowsOptimizationCatalog.cs` para
  `src/ApexTweaker.Application/Optimizations/WindowsOptimizationCatalog.cs`;
- `src/Services/WindowsOptimizationRecommendationService.cs` para
  `src/ApexTweaker.Application/Optimizations/WindowsOptimizationRecommendationService.cs`;
- `src/Services/WindowsOptimizationSelfTest.cs` para
  `src/ApexTweaker.Application/Optimizations/WindowsOptimizationSelfTest.cs`;
- `src/Services/WindowsOptimizationInventoryService.cs` para
  `src/ApexTweaker.Windows/Inventory/WindowsOptimizationInventoryService.cs`;
- `src/Services/HardwareEnvironmentDetector.cs` para
  `src/ApexTweaker.Windows/Hardware/HardwareEnvironmentDetector.cs`;
- `src/Services/IntelHybridProbeStrategy.cs` para
  `src/ApexTweaker.Windows/Hardware/IntelHybridProbeStrategy.cs`;
- `src/NativeInterop/NativeMethods.cs` para
  `src/ApexTweaker.Windows/Native/NativeMethods.cs`;
- `src/Services/WindowsOptimizationService.cs` para
  `src/App/WindowsOptimizationService.cs`, como fachada de composicao
  compativel.

Criados:

- projetos `ApexTweaker.Contracts`, `ApexTweaker.Application` e
  `ApexTweaker.Windows`;
- contrato `IWindowsOptimizationInventory`;
- metadados `InternalsVisibleTo` estritamente entre os assemblies da solucao.

## Contratos

Nao ha mudanca de formato ou acessibilidade publica nesta etapa. Os tipos
continuam `internal`; `InternalsVisibleTo` permite somente que os assemblies da
solucao compartilhem os contratos durante a migracao. A fachada
`ApexTweaker.Services.WindowsOptimizationService` preserva nome, construtor e
assinatura.

## Riscos e mitigacoes

- Referencias de projeto incorretas: verificadas por build Release.
- Tipos internos inacessiveis: limitados por `InternalsVisibleTo`, sem ampliar
  a API publica.
- Alteracao acidental de comportamento: os corpos de catalogo, inventario e
  recomendacao sao mantidos; o self-test existente continua sendo executado.
- Mutacao do Windows: nenhuma rotina de aplicacao e chamada pelas validacoes.
- Legado ainda acoplado: documentado como pendencia; nao sera movido em massa
  antes de possuir interfaces e testes de caracterizacao.
