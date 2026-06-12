# ApexTweaker

Windows Forms utility focused on Windows performance, frametime stability, telemetry, and reversible optimization workflows.

## Distribuicao

Instalador para distribuir aos clientes:

`release-v2/ApexTweaker-Setup-2.0.0.exe`

O instalador copia o app para `Program Files\ApexTweaker`, cria atalho no Menu Iniciar e pede Administrador quando necessario.

Executavel portatil mantido para testes locais:

`release-v2/ApexTweaker.exe`

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
