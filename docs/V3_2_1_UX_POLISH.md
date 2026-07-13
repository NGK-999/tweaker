# ApexTweaker v3.2.1 - Cobblemon Facil UX Polish

## Escopo

A v3.2.1 e um patch de apresentacao sobre a v3.2.0. Scanner, perfis,
backup, rollback, benchmark, diagnostico e motor cientifico nao foram
refatorados.

## Fluxo visivel

O modo facil apresenta a sequencia:

`Detectar -> Analisar -> Otimizar -> Testar -> Corrigir ou Restaurar`

Cada card possui um dos estados:

- Nao iniciado;
- Pronto para executar;
- Executando;
- Concluido;
- Atencao;
- Falhou;
- Bloqueado.

A etapa atual recebe fundo e borda destacados. Etapas indisponiveis continuam
visiveis para explicar o fluxo, mas permanecem bloqueadas.

## Status por etapa

- Inicial: `Aguardando deteccao da instancia Minecraft.`
- Deteccao: `Instancia detectada. Proximo passo: Analisar Mods.`
- Auditoria: `Mods analisados. Proximo passo: Otimizar para PC Fraco.`
- Aplicacao: `Otimizacao aplicada com backup. Proximo passo: Testar Jogo.`
- Teste: `Teste concluido. Escolha Corrigir Problemas, Restaurar Tudo ou Exportar Diagnostico.`

Na interface os textos acima usam acentuacao normal. Este documento usa ASCII
para manter consistencia com os demais arquivos historicos do repositorio.

## Ajustes visuais

- CTA inicial `Detectar Instancia Agora` destacado no cabecalho;
- somente um acesso ao `Modo Avancado`, no topo;
- rodape reservado para restauracao e exportacao;
- margem inferior adicional para o ultimo card subir acima do rodape;
- scrollbar escura local ao modo facil;
- cards bloqueados com opacidade legivel;
- tooltips habilitados mesmo em botoes desativados.

## Regras dos botoes

- `Restaurar Tudo`: liberado depois que o modo facil cria um backup;
- `Exportar Diagnostico`: liberado depois da analise ou do teste;
- `Corrigir Problemas`: liberado depois do teste;
- `Testar Jogo`: liberado depois da otimizacao no modo facil.

O modo avancado continua disponivel para benchmark de baseline, dry-run,
quarentena confirmada e experimentos cientificos.

## Linguagem simples

O modo facil traduz marcadores internos quando eles aparecem em evidencias:

- `INSUFFICIENT_DATA`: nao foi possivel provar melhora;
- `SERVER_MOD_MISMATCH`: servidor recusou por mod ausente ou versao diferente;
- `PAGEFILE_PRESSURE`: Windows usando muita memoria virtual.

Hashes, caminhos internos do ZIP, JSON, diffs, logs longos e decisoes internas
do motor cientifico permanecem fora da tela facil.

## Seguranca preservada

- nenhum JAR e movido ou excluido pelo modo facil;
- aplicacao de perfil continua exigindo confirmacao e backup;
- rollback continua validando o backup gerenciado;
- FPS continua manual quando nao existe medicao automatica;
- executavel continua usando `asInvoker`.
