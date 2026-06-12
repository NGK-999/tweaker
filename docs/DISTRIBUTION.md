# Distribuicao

O arquivo para enviar ao cliente fica em:

`release/ValorantTweaker.exe`

Esse executavel e self-contained para Windows 10/11 64-bit. O cliente nao precisa instalar .NET.

## Como gerar

No terminal, a partir da raiz do projeto:

```powershell
dotnet publish VSCODE.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o release
```

Ou rode a task:

`Publicar EXE unico`

## Como o cliente deve executar

O app ja pede Administrador pelo manifesto.

O cliente so precisa executar:

`ValorantTweaker.exe`

## Observacoes

- Windows SmartScreen pode alertar enquanto o arquivo nao estiver assinado digitalmente.
- Para distribuicao comercial, assine o executavel com certificado de code signing.
- A versao atual e `1.0.0`.
- O app cria backups em `C:\ProgramData\ValorantTweaker\Backups`.
- O app nao modifica arquivos do VALORANT e nao mexe no Vanguard.
