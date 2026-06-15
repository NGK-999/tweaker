# ApexTweaker

Windows Forms utility focused on Windows performance, frametime stability, telemetry, and reversible optimization workflows.

## Distribuicao

Executavel oficial para distribuir aos clientes:

`release-v2/ApexTweaker.exe`

Esse e o artefato correto de distribuicao. O app e executado diretamente como `.exe` e ja solicita privilegios administrativos pelo manifesto quando necessario.

## Fluxo recomendado

1. Diagnosticar
2. Otimizar sistema ao maximo
3. Reiniciar o PC
4. Usar a telemetria para comparar estabilidade antes/depois

## Observacoes

- O app nao modifica arquivos de jogos.
- O app evita hooks agressivos em jogos protegidos por anti-cheat.
- Backups ficam em `C:\ProgramData\ApexTweaker\Backups`.
- Logs da sessao ficam em `C:\ProgramData\ApexTweaker\Logs\latest_runtime.log`.
- Feito por Igor Silva.
