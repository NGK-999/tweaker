# ApexTweaker

Utilitario Windows em .NET 10 + WPF para diagnostico de hardware, telemetria,
otimizacoes reversiveis e preparacao segura de Minecraft/Cobblemon em hardware
limitado.

Versao: **3.0.0** | Autor: **Igor Silva**

## Minecraft Scientific Optimization Engine

A aba **Cobblemon** adiciona um motor experimental separado das mutacoes de Windows:

- le `fabric.mod.json`, metadados Forge/NeoForge e JARs aninhados;
- identifica loader, versao, ambiente, dependencias, `provides` e `breaks`;
- calcula SHA-256 e encontra duplicidades sem modificar os arquivos;
- classifica mods com postura conservadora para requisitos de servidor;
- gera relatorios JSON, Markdown e TXT;
- gera dry-run de quarentena e move somente JARs explicitamente selecionados;
- oferece os perfis `SAFE`, `LOW_END`, `EXTREME_4GB`,
  `COBBLEMON_SERVER_CLIENT` e `BENCHMARK`;
- cria backup antes de alterar `options.txt`, configs suportadas e memoria Prism/MultiMC;
- permite rollback separado do perfil e da quarentena;
- mede RAM, CPU, configs, logs e crashes do processo Minecraft por ate 10 minutos;
- permite escolher 30, 45 ou 60 FPS e explica o patamar de heap de 4 GB;
- gera checklist e resultado de homologacao sem inventar FPS ou entrada no servidor;
- exige confirmacao adicional do manifesto para quarentenar mod possivelmente server-side;
- nunca exclui mods e nunca preseleciona candidatos de quarentena.
- diagnostica gargalos de RAM, CPU, GPU, disco, heap, pagefile, configs e mods;
- cria experimentos persistentes com hipotese, baseline, candidato e hashes;
- compara metricas com limiares declarados e decide `KEEP`, `REVERT` ou `RETEST`;
- restaura pelo backup exato do experimento quando existe regressao;
- distingue fato medido, inferencia e recomendacao manual;
- oferece perfis `GPU_LIMITED`, `RAM_LIMITED`, `CPU_LIMITED` e
  `SERVER_ENTRY_COMPATIBLE` alem dos perfis anteriores.

Documentacao completa: [docs/COBBLEMON_LOW_END.md](docs/COBBLEMON_LOW_END.md).
Fluxo para o PC real: [docs/HOMOLOGACAO_OPERACIONAL_COBBLEMON.md](docs/HOMOLOGACAO_OPERACIONAL_COBBLEMON.md).
Motor cientifico e CLI: [docs/SCIENTIFIC_ENGINE.md](docs/SCIENTIFIC_ENGINE.md).
Arquitetura da v3: [docs/ARCHITECTURE_V3.md](docs/ARCHITECTURE_V3.md).

## Interface

| Aba | Funcao |
|-----|--------|
| **Dashboard** | Auto-Tuning, restore point e resumo de hardware |
| **Modulos** | Tweaks individuais de energia, CPU, GPU e rede |
| **Telemetria** | Teste A/B, frametime, metricas e console |
| **Cobblemon** | Diagnostico, experimento A/B, perfis, quarentena, benchmark e rollback Minecraft |
| **Utilidades** | Rollback mestre, desinstalacao e suporte |

## Linha de comando

Auditoria somente leitura:

```powershell
dotnet run --project ApexTweaker.csproj -- `
  --minecraft-audit `
  --mods "$env:USERPROFILE\Downloads\mods\mods" `
  --output ".\artifacts\cobblemon-audit"
```

Autoteste do scanner, relatorios, perfil e rollback:

```powershell
dotnet run --project ApexTweaker.csproj -- --minecraft-self-test
```

Use `--minecraft-help` para listar os demais comandos. Operacoes de escrita por
CLI exigem `--yes` e uma instancia valida com `options.txt` e subpasta `mods`.

Inicio de um experimento cientifico:

```powershell
dotnet ApexTweaker.dll --minecraft-scientific-start `
  --instance "C:\PrismLauncher\instances\Cobblemon Low-End\.minecraft" `
  --fps 30
```

O baseline precisa ser registrado antes de qualquer escrita. O fluxo completo,
incluindo os comandos de medicao e finalizacao, esta em
[docs/SCIENTIFIC_ENGINE.md](docs/SCIENTIFIC_ENGINE.md).

## Distribuicao

Artefatos oficiais:

- [ApexTweaker.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.exe)
- [ApexTweaker.Native.dll](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.Native.dll)
- [ApexTweaker-Setup.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker-Setup.exe)
- [ApexTweaker-Portable-v3.0.0.zip](https://github.com/NGK-999/tweaker/releases/download/v3.0.0/ApexTweaker-Portable-v3.0.0.zip)

O executavel publicado e self-contained, pede Administrador pelo manifesto e
nao exige .NET instalado.

## Build local

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet publish ApexTweaker.csproj -c Release -r win-x64 --self-contained true -o release-v2
```

Requisito: Visual Studio Build Tools com suporte a C++ para compilar
`ApexTweaker.Native.dll`.

## Dados e seguranca

- Backups de Windows: `C:\ProgramData\ApexTweaker\Backups`
- Backups de perfis Minecraft: `C:\ProgramData\ApexTweaker\MinecraftBackups`
- Backups de quarentena: `C:\ProgramData\ApexTweaker\MinecraftQuarantineBackups`
- Relatorios Minecraft: `C:\ProgramData\ApexTweaker\MinecraftReports`
- Experimentos cientificos: `C:\ProgramData\ApexTweaker\MinecraftExperiments`
- Logs: `C:\ProgramData\ApexTweaker\Logs\latest_runtime.log`
- O perfil altera `options.txt` e apenas chaves existentes validadas de Sodium,
  ImmediatelyFast e EntityCulling, alem de desativar Iris por chave reconhecida.
- Prism/MultiMC recebem memoria por instancia; outros launchers recebem uma
  instrucao JVM manual.
- Defender, Windows Update, pagefile e mods de servidor nao sao desativados ou
  removidos por esse fluxo.

O repositorio GitHub esta privado; links de release retornam `404` sem uma conta
autorizada. Mais detalhes de build e distribuicao:
[docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) e
[docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md).
