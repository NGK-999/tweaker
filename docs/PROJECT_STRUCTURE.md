# Estrutura do projeto

Este projeto e um aplicativo Windows Forms focado em ajustes seguros para VALORANT.

## Pastas principais

- `src/App`
  - Entrada da aplicacao.
  - Contem `Program.cs`.

- `src/Forms`
  - Telas do aplicativo.
  - Contem `ValorantTweakerForm.cs`, a janela principal.

- `src/Services`
  - Regras e acoes do tweaker.
  - `SystemDiagnosticsService.cs`: coleta informacoes do Windows.
  - `TweakService.cs`: aplica e reverte tweaks seguros.
  - `ValorantLocator.cs`: procura o executavel do VALORANT.
  - `RegistryService.cs`: leitura e escrita no Registro do Windows.

- `src/Infrastructure`
  - Codigo tecnico reutilizavel.
  - `CommandRunner.cs`: executa comandos externos, como PowerShell e powercfg.

- `src/Models`
  - Tipos simples de dados.
  - `CommandResult.cs`: resultado de um comando executado.

- `release`
  - Pasta oficial de distribuicao.
  - Contem o arquivo unico `ValorantTweaker.exe`.
  - Esta e a pasta que deve ser usada para testar ou enviar o app.

## Pastas geradas pelo .NET

- `bin`
  - Saida compilada, incluindo o `.exe` publicado.

- `obj`
  - Arquivos temporarios de build.

Essas duas pastas sao geradas automaticamente e nao devem receber codigo manual.
Elas podem ser apagadas sem perder o codigo do projeto.
