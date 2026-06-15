# Distribuicao

O arquivo para enviar ao cliente fica em:

`release-v2/ApexTweaker.exe`

Esse executavel e self-contained para Windows 10/11 64-bit. O cliente nao precisa instalar .NET nem rodar instalador separado.

## Como gerar

No terminal, a partir da raiz do projeto:

```powershell
dotnet publish VSCODE.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o release-v2
```

Ou rode a task:

`Publicar EXE unico`

## Como o cliente deve executar

O app ja pede Administrador pelo manifesto.

O cliente so precisa executar:

`ApexTweaker.exe`

## Observacoes

- Windows SmartScreen pode alertar enquanto o arquivo nao estiver assinado digitalmente.
- Para distribuicao comercial, assine o executavel com certificado de code signing.
- A versao atual e `2.0.0`.
- O app cria backups em `C:\ProgramData\ApexTweaker\Backups`.
- O app nao depende de instalador `.exe`; a entrega oficial e o binario unico em `release-v2`.
