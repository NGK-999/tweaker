# ApexTweaker v3.2.0 - Cobblemon One-Click Mode

## Objetivo

A v3.2.0 adiciona uma camada operacional para quem nao conhece Java, heap,
Sodium, hashes ou logs. O laboratorio cientifico da v3.1.0 continua disponivel
em **Modo Avancado**, sem perda de funcionalidade.

O fluxo visivel por padrao e:

`Detectar -> Analisar -> Otimizar -> Testar -> Corrigir ou Restaurar`.

## Os seis botoes

### 1. Detectar Instancia

- valida a pasta atual;
- procura instancias oficiais, Prism, MultiMC, Modrinth App e CurseForge;
- valida Java x64, `options.txt`, `mods`, `config` e `logs`;
- quando existe mais de uma candidata, exige selecao manual;
- nao escolhe silenciosamente uma instancia ambigua.

### 2. Analisar Mods

- executa o scanner somente leitura;
- resume mods essenciais, performance, peso visual, duplicatas e riscos;
- mantem a lista tecnica completa escondida no Modo Avancado;
- calcula SHA-256 sem mover ou modificar JARs.

### 3. Otimizar para PC Fraco

Depois da confirmacao do usuario:

- recalcula o plano no momento da aplicacao;
- cria backup transacional;
- aplica `POTATO_COBBLEMON_4GB`;
- gera relatorio antes/depois;
- registra hashes;
- nao toca nos JARs.

Preset padrao:

- janela 960x540;
- render distance 2;
- simulation distance 5;
- entity distance 0.30;
- 24 FPS, com opcao de 30 FPS;
- graficos rapidos, AO, nuvens, sombras e VSync desligados;
- particulas minimas, mipmap 0 e biome blend 0;
- bob view e efeitos desligados;
- resource packs locais desmarcados sem apagar arquivos;
- `-Xms512M -Xmx2048M`.

Prism e MultiMC recebem a memoria diretamente no `instance.cfg`. Launchers sem
contrato de escrita recebem a instrucao exata para configuracao manual; a tela
nao declara que o heap foi aplicado nesses casos.

A opcao extrema usa 854x480 e preserva os demais limites. Somente chaves que
ja existem em `options.txt` sao alteradas. Sodium e Iris usam apenas contratos
ja validados; configs ausentes nao sao inventadas.

### 4. Preparar para Servidor

- pergunta se o servidor exige Mega Showdown;
- verifica Cobblemon, Fabric API, dependencias e duplicatas;
- marca Indium apenas como candidato a teste sem;
- sinaliza Iris, Distant Horizons, Continuity, EMF e ETF como peso visual;
- nunca remove, desativa ou move um mod.

O resultado e traduzido para uma destas mensagens:

- Provavelmente pronto para servidor;
- Pode faltar mod obrigatorio;
- Ha duplicatas;
- Ha mods visuais pesados;
- Requer confirmacao manual.

### 5. Testar Jogo

O ApexTweaker coleta automaticamente, por 60 segundos:

- RAM do processo Java;
- RAM livre;
- CPU;
- I/O;
- commit/pagefile;
- `latest.log`;
- crash report;
- evidencia de OOM ou crash.

O usuario informa apenas:

- se o jogo abriu;
- se chegou ao menu;
- se entrou no servidor;
- se travou muito;
- se fechou sozinho;
- se apareceu erro de mod;
- FPS aproximado, somente quando observado.

FPS vazio permanece `NAO DISPONIVEL`.

### 6. Corrigir Problemas

O botao nao aplica uma sequencia opaca de tweaks. Ele traduz a evidencia em
uma lista curta de proximos testes:

- OOM/pagefile: testar heap 1792 MB, 854x480, 24 FPS e render 2;
- servidor recusou: comparar IDs e versoes com o manifesto oficial;
- stutter: manter pagefile, testar 1792 contra 2048 MB e remover peso visual
  apenas em copia;
- crash: destacar mods citados no log e recomendar teste isolado.

Uma sugestao nao e aplicada sem nova confirmacao. Isso evita misturar varias
hipoteses e perder a causa real da melhora ou regressao.

## Restaurar Tudo

O botao permanece visivel no rodape. Ele restaura o ultimo backup gerenciado:

- `options.txt`;
- configs suportadas alteradas;
- resource packs ativos registrados em `options.txt`;
- memoria da instancia Prism/MultiMC;
- arquivo JVM criado pelo ApexTweaker.

O rollback confere SHA-256. A quarentena avancada possui rollback separado,
mas o Modo Facil nunca executa quarentena.

## Exportar Diagnostico

O ZIP e criado em:

`%LOCALAPPDATA%\ApexTweaker\MinecraftDiagnosticPackages`.

Quando disponiveis, inclui:

- `diagnostic.json` e `diagnostic.md`;
- lista de mods e hashes SHA-256;
- `latest.log` e ate cinco crash reports recentes;
- configuracoes antes/depois do ultimo perfil;
- manifesto do backup;
- benchmark automatico;
- respostas do usuario;
- hardware, servidor e recomendacoes.

Logs maiores que 8 MB sao limitados aos ultimos 8 MB. O ZIP e somente leitura
sobre a instancia e recebe seu proprio SHA-256. Como caminhos e logs podem
conter dados do computador, revise o pacote antes de compartilhar.

## Modo Avancado

O segundo separador preserva:

- wizard cientifico de dez etapas;
- dry-run e diffs;
- hashes completos;
- quarentena confirmada;
- experimentos de uma variavel;
- baseline/candidato;
- KEEP, REVERT, RETEST e INSUFFICIENT_DATA;
- grafico e relatorios completos.

## Passo a passo no i3 com 4 GB

1. Extraia o ZIP portatil e execute `ApexTweaker.exe` sem administrador.
2. Abra **Cobblemon Facil**.
3. Clique **Detectar Instancia** e selecione a instancia correta se houver mais de uma.
4. Clique **Analisar Mods**.
5. Compare os alertas com a lista oficial do servidor.
6. Clique **Otimizar para PC Fraco** e confirme o backup.
7. Comece em 960x540, 24 FPS e Xmx2048M.
8. Use **Preparar para Servidor** e informe a exigencia do Mega Showdown.
9. Clique **Testar Jogo**, abra o Minecraft e tente entrar no servidor.
10. Responda as perguntas sem estimar o que nao foi observado.
11. Use **Corrigir Problemas** ou **Restaurar Tudo**.
12. Gere **Exportar Diagnostico** se precisar de analise tecnica.

## Riscos restantes

- 4 GB continuam abaixo do ideal para Cobblemon e iGPU compartilhada;
- entrada no servidor depende do manifesto real, indisponivel ao ApexTweaker;
- FPS continua manual;
- mods visuais podem ser obrigatorios por um resource pack especifico;
- o comportamento final somente pode ser homologado no i3/4 GB real;
- 8 GB em dual-channel e SSD continuam sendo as recomendacoes de maior impacto.
