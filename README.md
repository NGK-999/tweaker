# ApexTweaker

Utilitario Windows em .NET 10 + WPF para diagnostico de hardware, telemetria,
otimizacoes reversiveis e preparacao segura de Minecraft/Cobblemon em hardware
limitado.

Versao: **2.1.0** | Autor: **Igor Silva**

## Cobblemon Low-End Lab

A aba **Cobblemon** adiciona um fluxo separado das mutacoes de Windows:

- le `fabric.mod.json`, metadados Forge/NeoForge e JARs aninhados;
- identifica loader, versao, ambiente, dependencias, `provides` e `breaks`;
- calcula SHA-256 e encontra duplicidades sem modificar os arquivos;
- classifica mods com postura conservadora para requisitos de servidor;
- gera relatorios JSON, Markdown e TXT;
- cria uma pasta de sugestoes de quarentena com hashes, sem mover JARs;
- oferece os perfis `SAFE`, `LOW_END`, `EXTREME_4GB`,
  `COBBLEMON_SERVER_CLIENT` e `BENCHMARK`;
- cria backup antes de alterar `options.txt`;
- permite rollback do ultimo perfil;
- mede RAM, CPU e pressao de memoria do processo Java por ate 10 minutos;
- nunca exclui ou move mods automaticamente.

Documentacao completa: [docs/COBBLEMON_LOW_END.md](docs/COBBLEMON_LOW_END.md).

## Interface

| Aba | Funcao |
|-----|--------|
| **Dashboard** | Auto-Tuning, restore point e resumo de hardware |
| **Modulos** | Tweaks individuais de energia, CPU, GPU e rede |
| **Telemetria** | Teste A/B, frametime, metricas e console |
| **Cobblemon** | Auditoria de mods, perfis, benchmark e rollback Minecraft |
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

## Distribuicao

Artefatos oficiais:

- [ApexTweaker.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.exe)
- [ApexTweaker-Setup.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker-Setup.exe)

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
- Backups Minecraft: `C:\ProgramData\ApexTweaker\MinecraftBackups`
- Relatorios Minecraft: `C:\ProgramData\ApexTweaker\MinecraftReports`
- Logs: `C:\ProgramData\ApexTweaker\Logs\latest_runtime.log`
- O perfil Minecraft altera somente `options.txt` e cria
  `apextweaker-java-args.txt` depois de um backup verificavel.
- Argumentos Java sao recomendados, nao injetados automaticamente no launcher.
- Defender, Windows Update, pagefile e mods de servidor nao sao desativados ou
  removidos por esse fluxo.

Mais detalhes de build e distribuicao:
[docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) e
[docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md).
