# ApexTweaker v3.3.1 - Minecraft Rapido

## Objetivo

Remover a aparencia de ferramenta de auditoria do modo facil sem alterar o
motor seguro de Minecraft, backup, rollback, benchmark ou hooks de sessao.

## Novo fluxo

1. `Encontrar Minecraft` localiza Prism, MultiMC ou Modrinth e permite escolher
   a pasta manualmente.
2. `Preparar para Jogar` verifica os mods em leitura, cria backup e aplica o
   perfil leve depois da confirmacao do usuario.
3. `Testar o Jogo` observa memoria, processo Java e logs por 60 segundos.
4. `Resolver Problemas` transforma o resultado em uma proxima acao segura.

`Restaurar` e `Exportar diagnostico` permanecem como acoes secundarias.

## Simplificacao visual

- os botoes separados `Analisar Mods` e `Validar Multiplayer` sairam do caminho
  principal;
- configuracao de resolucao, FPS, hook e destino ficou em `Ajustes opcionais`;
- contadores e detalhes de auditoria ficaram recolhidos em
  `Ver detalhes da verificacao`;
- a acao principal passou a se chamar `Preparar para Jogar`;
- o modo facil nao exibe hashes, JSON, logs longos ou decisoes internas.

## Seguranca preservada

- a verificacao automatica nao escreve nem move JARs;
- aplicar configuracoes continua exigindo confirmacao;
- backup e relatorio antes/depois continuam obrigatorios;
- rollback continua restaurando o backup gerenciado;
- quarentena continua disponivel somente no modo avancado e requer confirmacao
  explicita;
- o manifesto continua `asInvoker`;
- FPS nao e inventado quando indisponivel.

## Validacao

- build Release sem avisos nem erros;
- self-test completo com `SELF_TEST_OK`;
- XAML carregado em thread STA;
- fluxo direto Encontrar -> Preparar coberto pelo self-test;
- teste fisico no PC i3 de quarta geracao com 4 GB continua obrigatorio.

## Assets locais

| Asset | SHA-256 |
|---|---|
| `ApexTweaker.exe` | `eb6cf864477cafa5589914cabc5dd44314c713df742f2a6d7d4cbfa7f250e366` |
| `ApexTweaker.Native.dll` | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |
| `ApexTweaker-Portable-v3.3.1.zip` | `bb5e44ff8993c943f3e6438553ebe787b028d5130bfcb691ceec15e9e1460109` |
| `ApexTweaker-Setup.exe` | `a1eae529cd0bb6cdac0b0e7c6bd78e33c33cd6abcef0a1ea5662b40d43e5a93e` |
