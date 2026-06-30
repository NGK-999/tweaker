# ApexTweaker

Utilitario Windows (.NET 10 + WPF) focado em performance, estabilidade de frametime, telemetria e otimizacoes reversiveis — com foco em jogos competitivos, especialmente VALORANT.

Versao: **2.0.1** · Autor: **Igor Silva**

## Distribuicao

Executavel oficial para clientes:

`release-v2/ApexTweaker.exe`

Baixar da release:

- [ApexTweaker.exe (portatil)](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.exe)
- [ApexTweaker-Setup.exe (instalador)](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker-Setup.exe)

O `.exe` e self-contained, pede Administrador pelo manifesto e nao exige .NET instalado.

## Fluxo recomendado

1. Abrir o app e revisar o resumo de hardware no Dashboard
2. Criar ponto de restauracao (opcional, recomendado)
3. Executar **Auto-Tuning** ou modulos especificos
4. Reiniciar o PC
5. Usar **Telemetria** para comparar estabilidade antes/depois

## Interface (WPF)

| Aba | Funcao |
|-----|--------|
| **Dashboard** | Auto-Tuning inteligente, restore point, resumo de hardware |
| **Modulos** | Tweaks individuais (energia, CPU, GPU, rede, etc.) |
| **Telemetria** | Teste A/B, grafico de FPS, metricas de kernel, console |
| **Utilidades** | Reverter, desinstalar, sobre, suporte Riot |

Documentacao detalhada dos modulos: [`docs/TWEAK_BUTTONS.md`](docs/TWEAK_BUTTONS.md).

## Build local

```powershell
dotnet build ApexTweaker.sln -c Release
dotnet publish ApexTweaker.csproj -c Release -r win-x64 --self-contained true -o release-v2
```

Requisito de build: **Visual Studio Build Tools com C++** (compila `ApexTweaker.Native.dll`).

Mais detalhes: [`docs/DISTRIBUTION.md`](docs/DISTRIBUTION.md) e [`docs/PROJECT_STRUCTURE.md`](docs/PROJECT_STRUCTURE.md).

## Observacoes

- O app **nao modifica arquivos de jogos**.
- O app **evita hooks agressivos** em processos protegidos por anti-cheat.
- Backups: `C:\ProgramData\ApexTweaker\Backups`
- Logs: `C:\ProgramData\ApexTweaker\Logs\latest_runtime.log`
- Toda mutacao passa por pipeline transacional com snapshot e rollback (`MutationExecutor` + `BackupService`).
