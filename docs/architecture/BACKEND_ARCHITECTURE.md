# Arquitetura do backend

## Estado atual

O backend esta sendo separado progressivamente em assemblies com dependencia
unidirecional. A primeira fatia isolada e o fluxo de analise de otimizacoes do
Windows.

```text
ApexTweaker.sln
|
+-- ApexTweaker.Contracts
|   +-- Inventory
|   |   +-- GpuInfo
|   |   `-- IWindowsOptimizationInventory
|   `-- Optimizations
|       `-- WindowsOptimizationModels
|
+-- ApexTweaker.Application
|   `-- Optimizations
|       +-- WindowsOptimizationCatalog
|       +-- WindowsOptimizationRecommendationService
|       +-- WindowsOptimizationApplicationFacade
|       `-- WindowsOptimizationSelfTest
|
+-- ApexTweaker.Windows
|   +-- Inventory
|   |   `-- WindowsOptimizationInventoryService
|   +-- Hardware
|   |   +-- HardwareEnvironmentDetector
|   |   `-- IntelHybridProbeStrategy
|   `-- Native
|       `-- NativeMethods
|
+-- ApexTweaker
|   +-- App
|   |   +-- Program
|   |   `-- WindowsOptimizationService
|   +-- Minecraft
|   +-- Services              legado em migracao
|   +-- Infrastructure        legado em migracao
|   `-- UI/Wpf                host desktop atual
|
`-- native/ApexTweaker.Native
    `-- C++ de topologia e afinidade
```

## Responsabilidades

### ApexTweaker.Contracts

Contem somente dados e interfaces compartilhados. Nao coleta informacoes, nao
recomenda ajustes e nao executa comandos.

### ApexTweaker.Application

Compara inventario com o catalogo e produz decisoes explicadas. Nao referencia
`ApexTweaker.Windows`, WPF, Registro, WMI, PowerShell ou processos.

### ApexTweaker.Windows

Implementa leitura de inventario e topologia no Windows. Esta camada pode usar
WMI, Registro somente leitura e P/Invoke. Ela nao escolhe presets e nao decide
quais otimizacoes aplicar.

### ApexTweaker

E o composition root e o host desktop atual. A fachada
`ApexTweaker.Services.WindowsOptimizationService` mantem a assinatura existente
e conecta a implementacao Windows ao caso de uso da Application.

### ApexTweaker.Native

Permanece isolado em C++. O acesso gerenciado esta em
`ApexTweaker.Windows/Native`.

## Fluxo de analise

```text
Desktop/CLI
    |
    v
WindowsOptimizationService
    |
    v
WindowsOptimizationApplicationFacade
    |                         |
    v                         v
IWindowsOptimizationInventory  WindowsOptimizationRecommendationService
    |                         |
    v                         v
Windows inventory             typed catalog
```

O inventario coleta fatos. A recomendacao toma decisoes. Nenhuma dessas etapas
aplica mutacoes.

## Fluxo de mutacao legado

Enquanto a segunda etapa nao for concluida, o pipeline existente continua
obrigatorio:

```text
Validate -> Snapshot -> Execute -> Verify/ReadBack -> Log
```

`MutationExecutor`, `BackupService` e os comandos concretos nao foram movidos
nesta primeira etapa porque ainda possuem dependencias cruzadas. O modo demo e
o bloqueio fail-safe de comandos desconhecidos devem permanecer ativos durante
toda a migracao.

## Regras de dependencia

- `Contracts` nao referencia nenhum outro projeto ApexTweaker.
- `Application` referencia somente `Contracts`.
- `Windows` referencia somente `Contracts`.
- o executavel referencia `Application`, `Contracts` e `Windows`.
- `Application` nao instancia implementacoes Windows.
- UI nao deve receber executavel ou script arbitrario.
- novos comandos mutaveis devem entrar pelo pipeline central.

## Compatibilidade

Os contratos movidos preservam namespace, nomes, membros e acessibilidade
`internal`. `InternalsVisibleTo` esta limitado aos assemblies da solucao para
permitir uma migracao sem ampliar a API publica.

Nao houve alteracao de payload, rota, serializacao ou formato persistido.

## Proximas etapas

1. Criar contratos pequenos para snapshot, execucao e read-back.
2. Separar captura, armazenamento e rollback hoje concentrados em
   `BackupService`.
3. Mover Registro, energia, BCD, servicos e processos para
   `ApexTweaker.Windows`.
4. Transformar `TweakService` em casos de uso menores que dependem de
   interfaces, nao de comandos concretos.
5. Separar contratos e persistencia de telemetria/benchmark.
6. Extrair o WPF para `ApexTweaker.Desktop` somente quando a migracao do host
   entrar no escopo.
