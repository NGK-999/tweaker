# ApexTweaker

Utilitario Windows em .NET 10 + WPF para diagnostico de hardware, telemetria,
otimizacoes reversiveis e preparacao segura de Minecraft/Cobblemon em hardware
limitado.

Versao: **2.2.0** | Autor: **Igor Silva**

## Cobblemon Low-End Lab

A aba **Cobblemon** adiciona um fluxo separado das mutacoes de Windows:

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
- nunca exclui mods e nunca preseleciona candidatos de quarentena.

Documentacao completa: [docs/COBBLEMON_LOW_END.md](docs/COBBLEMON_LOW_END.md).

## Interface

| Aba | Funcao |
|-----|--------|
| **Dashboard** | Auto-Tuning, restore point e resumo de hardware |
| **Modulos** | Tweaks individuais de energia, CPU, GPU e rede |
| **Telemetria** | Teste A/B, frametime, metricas e console |
| **Cobblemon** | Dry-run, perfil real, quarentena, benchmark e rollback Minecraft |
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
- Backups de perfis Minecraft: `C:\ProgramData\ApexTweaker\MinecraftBackups`
- Backups de quarentena: `C:\ProgramData\ApexTweaker\MinecraftQuarantineBackups`
- Relatorios Minecraft: `C:\ProgramData\ApexTweaker\MinecraftReports`
- Logs: `C:\ProgramData\ApexTweaker\Logs\latest_runtime.log`
- O perfil altera `options.txt` e apenas chaves existentes validadas de Sodium,
  ImmediatelyFast e EntityCulling.
- Prism/MultiMC recebem memoria por instancia; outros launchers recebem uma
  instrucao JVM manual.
- Defender, Windows Update, pagefile e mods de servidor nao sao desativados ou
  removidos por esse fluxo.

Mais detalhes de build e distribuicao:
[docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) e
[docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md).
