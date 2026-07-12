# Arquitetura ApexTweaker v3

## Analise critica da base anterior

A v2.3 ja tinha scanner de JARs, perfis reais, backup, rollback, quarentena,
benchmark operacional e relatorios. Os principais limites eram arquiteturais:

- benchmark com estado final amplo demais (`Approved`, `Unstable`, `Failed`);
- ausencia de hipotese e variavel independente persistidas;
- baseline e candidato nao formavam uma maquina de estados;
- rollback generico usava o backup mais recente, inadequado para experimento;
- diagnostico nao explicava regra, evidencia e confianca;
- classificacao de mod tinha um unico rotulo principal;
- CPU/RAM eram coletadas, mas disco e comparacao ponderada nao existiam;
- GUI/CLI executavam operacoes, mas nao impediam uma sequencia experimental
  invalida;
- contratos de configs suportadas estavam espalhados no aplicador.

Esses limites nao exigiam trocar de linguagem. Exigiam separar evidencia,
diagnostico, decisao, persistencia e mutacao.

## Decisao de linguagem

Foi mantido **C#/.NET 10 + WPF**, com C++ apenas na DLL nativa existente.

Razoes:

- WPF e a shell ativa e ja possui instalador e publicacao self-contained;
- WMI/Win32/Process/Performance APIs e JSON possuem suporte maduro no .NET;
- o gargalo do app e I/O e telemetria, nao throughput numerico;
- migracao criaria duas runtimes ou reescrita de UI sem ganho operacional;
- o modelo imutavel de records, nullable reference types e escrita atomica atende
  a rastreabilidade exigida;
- C++ permanece disponivel apenas quando acesso nativo realmente e necessario.

## Nova separacao

```text
GUI / CLI
   |
   v
ScientificExperimentService  <->  ScientificExperimentStore
   |          |                         |
   |          +-> ScientificReportService
   |
   +-> ScientificAutoOptimizeService
   |      +-> AuditService
   |      +-> BottleneckDiagnosticService
   |      +-> ProfileService
   |      +-> ModConfigContractCatalog
   |
   +-> BenchmarkService + ScientificMetricsService
   +-> InstanceEvidenceService
   +-> ScientificComparisonService
   +-> ProfileService -> exact backup / rollback
```

O `MinecraftProfileService` continua sendo a unica camada que escreve configs.
O motor cientifico apenas coordena, valida estado e registra evidencia.

## Modulos criados

| Modulo | Responsabilidade |
|---|---|
| `MinecraftScientificModels` | contratos de evidencia, hipotese, medicao, decisao e plano |
| `MinecraftInstanceEvidenceService` | snapshot de hashes, options e resource packs |
| `MinecraftBottleneckDiagnosticService` | regras rastreaveis para gargalos |
| `MinecraftModConfigContractCatalog` | matriz de suporte por mod/versao |
| `MinecraftScientificMetricsService` | consolida benchmark, observacao e logs |
| `MinecraftScientificComparisonService` | compara metricas com pesos e limiares |
| `MinecraftScientificAutoOptimizeService` | escolhe candidato conservador por gargalo |
| `MinecraftScientificExperimentStore` | JSON atomico e IDs seguros |
| `MinecraftScientificExperimentService` | maquina de estados e rollback exato |
| `MinecraftScientificReportService` | JSON/Markdown/TXT cientificos |

## Modulos removidos ou substituidos

Nenhum modulo legado foi removido fisicamente. Isso foi deliberado para manter
compatibilidade com os fluxos v2.3 e evitar regressao nas ferramentas Windows.

O fluxo cientifico substitui conceitualmente o benchmark generico como caminho
recomendado para decisao de performance. O benchmark e a homologacao anteriores
continuam disponiveis como coleta operacional e compatibilidade. O rollback
`latest` tambem continua disponivel para uso manual; experimentos usam o novo
rollback por ID exato.

## Fronteiras de escrita

Escrita permitida pelo experimento:

- `options.txt`;
- `instance.cfg` de Prism/MultiMC;
- arquivo de argumentos manual quando o launcher nao tem contrato;
- JSON/properties de Sodium, ImmediatelyFast, EntityCulling e Iris quando a
  chave suportada ja existe;
- manifestos e relatorios sob raizes gerenciadas.

Escrita proibida pelo experimento:

- JARs;
- registry;
- servicos;
- Defender/Windows Update;
- pagefile;
- arquivos fora da instancia e das raizes de dados;
- configs sem contrato fixado.

## Integridade

- snapshots usam SHA-256;
- apply de perfil cria manifesto antes da escrita;
- escrita de estado do experimento e atomica;
- rollback usa backup ID persistido no experimento;
- mods devem manter o mesmo conjunto de hashes entre baseline e candidato;
- drift de configs entre baseline e apply e bloqueado;
- config alterada fora da hipotese força `RETEST` com baixa confianca;
- path traversal em ID de experimento e rejeitado;
- mudanca externa de perfil/quarentena invalida o experimento ativo na GUI;
- dry-run recalculado no apply evita executar um plano obsoleto;
- ausencia de variavel independente reduz a decisao para inconclusiva/reteste.
- somente `KEEP` permanece aplicado; demais decisoes restauram o backup
  gerenciado quando ele existe.

## Arquivos alterados

Nucleo:

- `src/Minecraft/Models/MinecraftAuditModels.cs`
- `src/Minecraft/MinecraftCommandLine.cs`
- `src/Minecraft/MinecraftSelfTest.cs`
- `src/Minecraft/Services/MinecraftAuditService.cs`
- `src/Minecraft/Services/MinecraftBenchmarkService.cs`
- `src/Minecraft/Services/MinecraftModCatalog.cs`
- `src/Minecraft/Services/MinecraftProfileService.cs`
- `src/Minecraft/Services/MinecraftReportService.cs`

Interface:

- `src/UI/Wpf/MainWindow.xaml`
- `src/UI/Wpf/MainWindow.xaml.cs`
- `src/UI/Wpf/Views/MinecraftView.xaml`
- `src/UI/Wpf/Views/MinecraftView.xaml.cs`

Metadados/documentacao:

- `ApexTweaker.csproj`
- `src/App/AppInfo.cs`
- `installer/ApexTweaker.iss`
- `README.md`
- documentos da v3 em `docs`.

## Testes

`--minecraft-self-test` cobre:

- scanner, duplicidade e dependencia ausente;
- classificacao multitag;
- relatorios JSON/Markdown/TXT;
- quarentena, confirmacao, SHA-256 e rollback;
- heap de 2048/2304/2560 MB;
- dry-run e parsers de options/JSON/properties;
- apply e rollback de configs;
- rollback direcionado por backup ID;
- diagnostico e contratos de config;
- experimento completo com decisao `KEEP`;
- experimento completo com regressao, `REVERT` e conferencia da restauracao;
- armazenamento seguro e path traversal;
- XAML em thread STA.

O teste usa instancia sintetica para ser deterministico. A homologacao em um PC
real continua necessaria e nao e substituida por build ou self-test.
