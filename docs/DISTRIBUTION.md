# Distribuicao

## Artefato portatil (oficial)

Pasta: `release-v2/`

| Arquivo | Descricao |
|---------|-----------|
| `ApexTweaker.exe` | Executavel principal, self-contained, single-file |
| `ApexTweaker.Native.dll` | DLL nativa C++ (topologia/afinidade de CPU) |
| `ApexTweaker-Portable-v3.1.0.zip` | Pacote portatil com os dois arquivos acima |

O cliente nao precisa instalar .NET. Target: **Windows 10/11 64-bit**.

Versao atual: **3.1.0**.

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

O app usa `asInvoker` no manifesto e inicia sem UAC. Auditoria, configs,
backup/rollback Minecraft, relatorios e benchmark funcionam como usuario normal.
Mutacoes de Registro, BCD, energia, rollback de sistema e ETW de kernel pedem
elevacao sob demanda e somente depois de confirmacao.

Execucao minima:

```text
release-v2\ApexTweaker.exe
```

Somente para testar uma mutacao Windows com elevacao explicita:

```powershell
Start-Process "release-v2\ApexTweaker.exe" -Verb RunAs
```

## Fluxo de release recomendado

1. Fechar todas as instancias do ApexTweaker.
2. `scripts\Build-Release.ps1` (ou `dotnet publish ... -o release-v2`)
3. Confirmar data/tamanho de `ApexTweaker.exe`.
4. Executar `--minecraft-self-test` no EXE publicado.
5. Executar `scripts\Test-Release.ps1` para validar `asInvoker` sem bypass.
6. Conferir versoes, SHA-256 e conteudo do ZIP.
7. Testar navegacao, Auto-Tuning, telemetria e fechamento.
8. Rebuild do instalador.
9. Criar release no GitHub com os quatro artefatos.

## Observacoes

- **SmartScreen** pode alertar enquanto o arquivo nao estiver assinado digitalmente. Para distribuicao comercial, use certificado de code signing.
- Backups do app: `C:\ProgramData\ApexTweaker\Backups`
- Dados Minecraft/telemetria: `%LOCALAPPDATA%\ApexTweaker`
- O modulo Cobblemon modifica somente arquivos de uma instancia explicitamente
  selecionada, sempre com dry-run, confirmacao e backup. Ele nao injeta hooks no jogo.
- O repositorio esta privado em 2026-07-12. Os links abaixo retornam `404` sem
  login em uma conta autorizada:
  - Instalador: [ApexTweaker-Setup.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker-Setup.exe)
  - Portatil: [ApexTweaker.exe](https://github.com/NGK-999/tweaker/releases/latest/download/ApexTweaker.exe)
