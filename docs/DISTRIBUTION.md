# Distribuicao

## Artefato portatil (oficial)

Pasta: `release-v2/`

| Arquivo | Descricao |
|---------|-----------|
| `ApexTweaker.exe` | Executavel principal, self-contained, single-file |
| `ApexTweaker.Native.dll` | DLL nativa C++ (topologia/afinidade de CPU) |

O cliente nao precisa instalar .NET. Target: **Windows 10/11 64-bit**.

Versao atual: **2.2.0**.

## Como gerar o portatil

Use sempre **`release-v2/`** como unica pasta de release local. Nao use `release-v2-staging`.

Script recomendado (publica em `release-v2` e remove `release-v2-staging` se existir):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\Build-Release.ps1
```

Equivalente manual:

```powershell
dotnet publish ApexTweaker.csproj -c Release -r win-x64 --self-contained true -o release-v2
```

Parametros ja definidos no `.csproj`: single-file comprimido, ReadyToRun, bibliotecas nativas extraidas.

**Importante:** feche qualquer instancia de `ApexTweaker.exe` antes de publicar. Processo elevado bloqueia substituicao do executavel.

## Instalador (opcional)

Script Inno Setup: `installer/ApexTweaker.iss`

Saida: `release-installer/ApexTweaker-Setup.exe`

Reconstrua o instalador apos atualizar `release-v2`, para que o setup embuta o build mais recente.

## Como o cliente deve executar

O app solicita **Administrador** pelo manifesto (`app.manifest`). Mutacoes de Registro, BCD, energia e ETW de kernel exigem privilegio elevado.

Execucao minima:

```text
release-v2\ApexTweaker.exe
```

Ou, com elevacao explicita:

```powershell
Start-Process "release-v2\ApexTweaker.exe" -Verb RunAs
```

## Fluxo de release recomendado

1. Fechar todas as instancias do ApexTweaker.
2. `scripts\Build-Release.ps1` (ou `dotnet publish ... -o release-v2`)
3. Confirmar data/tamanho de `ApexTweaker.exe`.
4. Testar navegacao, Auto-Tuning, telemetria e fechamento.
5. Rebuild do instalador, se necessario.
6. Criar release no GitHub com os artefatos.

## Observacoes

- **SmartScreen** pode alertar enquanto o arquivo nao estiver assinado digitalmente. Para distribuicao comercial, use certificado de code signing.
- Backups do app: `C:\ProgramData\ApexTweaker\Backups`
- Logs: `C:\ProgramData\ApexTweaker\Logs\latest_runtime.log`
- O modulo Cobblemon modifica somente arquivos de uma instancia explicitamente
  selecionada, sempre com dry-run, confirmacao e backup. Ele nao injeta hooks no jogo.
- Downloads publicos (quando disponiveis):
  - Instalador: [ApexTweaker-Setup.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker-Setup.exe)
  - Portatil: [ApexTweaker.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.exe)
