# Minecraft Scientific Optimization Engine

Versao: **ApexTweaker 3.3.0**

## Objetivo e limite

O motor cientifico transforma uma alteracao de configuracao em um experimento
auditavel:

1. captura o estado da instancia;
2. diagnostica o gargalo provavel;
3. cria uma hipotese e um candidato em dry-run;
4. exige uma medicao baseline;
5. cria backup e aplica somente configs suportadas;
6. exige outra medicao na mesma cena;
7. compara antes/depois com limiares declarados;
8. decide `KEEP`, `REVERT`, `RETEST` ou `INSUFFICIENT_DATA`;
9. mantem aplicado somente `KEEP`; os demais restauram o backup exato;
10. persiste JSON, Markdown, TXT, hashes e trilha de auditoria.

O motor nao injeta codigo no Minecraft, nao mede FPS internamente e nao move
mods durante o ciclo automatico. FPS e informado pelo usuario a partir de F3,
Spark, PresentMon ou ferramenta equivalente. Sem processo Java ou observacao
real, o resultado permanece `NOT_TESTED`.

## Decisao de linguagem

C#/.NET 10 foi mantido. O projeto ja usa WPF, WMI, Win32, ETW, manifestos do
Windows, publicacao self-contained e uma DLL C++ pequena para topologia/afinidade.
Migrar o core para Rust, Go ou Python aumentaria o custo de empacotamento e
interoperabilidade sem resolver um gargalo de CPU do ApexTweaker. O trabalho
critico e I/O, parsing, telemetria e integridade transacional, areas atendidas
diretamente pelo .NET.

C++ continua restrito a `ApexTweaker.Native.dll`. O motor Minecraft permanece em
C# com tipos imutaveis, escrita atomica, SHA-256 e servicos isolados.

## Fato, inferencia e recomendacao

Cada evidencia tem uma origem explicita:

| Tipo | Exemplo |
|---|---|
| `FATO_AUTOMATICO` | amostra do processo Java, hash ou padrao explicito de log |
| `INFORMADO_PELO_USUARIO` | FPS via F3, tempo percebido e entrada no servidor |
| `INFERENCIA` | GPU limitada inferida de FPS baixo, CPU/RAM sem saturacao e iGPU Intel |
| `RECOMENDACAO_MANUAL` | medir FPS externamente ou testar um mod fora |
| `NAO_DISPONIVEL` | FPS/GPU nao capturados ou benchmark ausente |

GPU percentual nao e preenchida sem um contador confiavel por processo. A
identidade da GPU pode apoiar uma inferencia, mas nunca aparece como telemetria
medida.

## Maquina de estados

```text
BASELINE_PENDING
  -> BASELINE_RECORDED
  -> CANDIDATE_APPLIED
  -> CANDIDATE_RECORDED
  -> COMPARED
  -> KEPT | REVERTED | NEEDS_RETEST | FAILED
```

Regras:

- nenhum candidato pode ser aplicado antes do baseline;
- apply exige confirmacao explicita;
- conflitos estruturais de mods bloqueiam apply;
- o conjunto de hashes dos mods deve permanecer identico;
- configs nao podem mudar entre a captura baseline e o apply;
- o plano e recalculado e congelado no momento do baseline;
- apply rejeita qualquer diferenca entre o plano congelado e o atual;
- baseline e candidato precisam apontar para a mesma instancia;
- mudanca fora do plano rebaixa a rodada para `RETEST` e confianca baixa;
- qualquer decisao diferente de `KEEP` exige confirmacao e usa o backup exato;
- hashes das configs gerenciadas sao conferidos depois do rollback;
- identificadores de experimento sao validados contra path traversal;
- o manifesto JSON e gravado de forma atomica.

O estado padrao fica em:

```text
%LOCALAPPDATA%\ApexTweaker\MinecraftExperiments\exp-...
```

Cada pasta contem `experiment.json` e os relatorios `.json`, `.md` e `.txt`.
A CLI aceita `--experiment-root <path>` para testes ou um armazenamento gravavel
sem elevacao. Nesse caso, backups e relatorios auxiliares ficam sob a mesma raiz.

## Privilegio minimo

Auditoria, perfis, quarentena confirmada, rollback Minecraft, benchmark e
relatorios funcionam em modo normal. O manifesto usa `asInvoker`. Somente
mutacoes protegidas e rollback do Windows solicitam UAC sob demanda. Prism,
Java e Minecraft nunca devem ser executados como administrador.

## Resultados da medicao

O resultado detalhado e um destes:

- `NOT_TESTED`;
- `PASSED`;
- `PASSED_WITH_WARNINGS`;
- `UNSTABLE`;
- `FAILED_CRASH`;
- `FAILED_MEMORY`;
- `FAILED_SERVER_MOD_MISMATCH`;
- `FAILED_CONFIG`;
- `FAILED_UNKNOWN`.

`PASSED` exige jogo, menu e mundo/servidor registrados, FPS medio e minimo, e
amostras automaticas. Ausencia de FPS automatico pode produzir
`PASSED_WITH_WARNINGS`, mas nunca um FPS inventado.

## Metricas e comparacao

O benchmark automatico coleta por segundo:

- working set e private memory do processo Java;
- RAM fisica livre;
- CPU media e pico normalizadas pelos processadores logicos;
- bytes lidos e escritos pelo processo Java;
- commit por segundo e pagefile antes/depois;
- `latest.log`, crash report e sinais de `OutOfMemoryError`;
- hashes das configs e nomes dos JARs ativos.

A observacao guiada registra:

- jogo e menu abertos;
- mundo ou servidor acessado;
- tempo ate menu e ate o alvo;
- jogabilidade em 720p;
- FPS medio e minimo;
- quedas severas, crash e falta de memoria.

Limiar de mudanca por metrica:

| Metrica | Limiar | Peso |
|---|---:|---:|
| FPS medio | 5% | 2 |
| FPS minimo | 8% | 3 |
| tempo ate menu | 5% | 1 |
| tempo ate mundo/servidor | 5% | 2 |
| pico RAM Java | 5% | 2 |
| menor RAM livre | 8% | 2 |
| CPU media/pico | 7% | 1 cada |
| pagefile | 128 MB absolutos | 2 |
| leitura/escrita Java | 10% | 1 cada |
| entrada no servidor | booleano | 5 |
| crash | booleano | 6 |
| out of memory | booleano | 7 |

Uma melhoria soma o peso; regressao subtrai. `KEEP` exige score >= 3,
`REVERT` score <= -3 e a faixa intermediaria vira `RETEST`. Novo crash, OOM,
perda de entrada no servidor ou nova falha e regressao critica e força
`REVERT`. Menos de tres metricas comparaveis resulta em
`INSUFFICIENT_DATA`.

Confianca alta exige pelo menos nove metricas comparaveis, 40 amostras somadas
e FPS completo nas duas rodadas. Uma rodada curta nao recebe confianca alta.

## Diagnostico de gargalo

O motor classifica:

- `RAM_LIMITED`;
- `CPU_LIMITED`;
- `GPU_LIMITED`;
- `DISK_LIMITED`;
- `JAVA_HEAP_TOO_LOW`;
- `JAVA_HEAP_TOO_HIGH`;
- `PAGEFILE_PRESSURE`;
- `MOD_CONFLICT`;
- `SERVER_MOD_MISMATCH`;
- `CONFIG_TOO_HEAVY`;
- `UNKNOWN`.

Conflito estrutural e mismatch do servidor tem prioridade. RAM, heap e pagefile
usam RAM fisica/livre, OOM, pico Java e variacao do pagefile. CPU usa amostras
do processo. GPU e disco sao inferidos apenas quando existe evidencia auxiliar
suficiente, e o relatorio identifica a inferencia.

## Perfis

| Perfil | Uso |
|---|---|
| `SAFE` | mudancas pequenas e conservadoras |
| `LOW_END` | hardware limitado acima do caso extremo |
| `EXTREME_4GB` | configuracao minima geral para 4 GB |
| `POTATO_COBBLEMON_4GB` | 960x540, render 2, simulation 5, entidades 30% e 24 FPS |
| `GPU_LIMITED` | 720p, efeitos minimos, entidades baixas e 30 FPS |
| `RAM_LIMITED` | render 4/simulation 5, heap conservador e 30 FPS |
| `CPU_LIMITED` | simulation 5, entidades reduzidas e 30 FPS |
| `SERVER_ENTRY_COMPATIBLE` | preserva mods e prioriza compatibilidade, 45 FPS |
| `COBBLEMON_SERVER_CLIENT` | perfil operacional anterior para cliente do servidor |
| `BENCHMARK` | ambiente controlado para rodada comparativa |

O primeiro teste em maquina com aproximadamente 4 GB usa:

```text
-Xms512M -Xmx2048M
```

Heaps maiores ficam reservados para hipoteses posteriores:

```text
-Xms512M -Xmx1792M
-Xms512M -Xmx2304M
-Xms512M -Xmx2560M
```

A escolha inicial nao depende de um pico momentaneo de RAM livre. `-Xmx4G` nunca e emitido.
Prism/MultiMC recebem memoria em `instance.cfg`; launchers sem contrato gravavel
recebem um arquivo de instrucao manual.

## Contratos de configuracao de mods

| Mod | Automacao | Comportamento |
|---|---|---|
| Sodium 0.6.13 | suportada | somente chaves existentes de culling/fog/texturas/memory tracing |
| ImmediatelyFast 1.6.11 | defaults recomendados | teste isolado; reverta crash ou bug visual |
| EntityCulling 1.10.5 | defaults recomendados | validar Pokemon; whitelist permanece manual |
| Iris | suportada parcialmente | apenas `enableShaders=false` |
| Lithium | defaults recomendados | nenhum override inventado |
| FerriteCore | sem config necessaria | ganho vem da instalacao |
| ModernFix | manual | mixins variam por versao/modpack |
| More Culling | manual | exige teste com resource packs |
| Dynamic FPS | manual | reduz consumo em segundo plano, nao FPS em foco |
| Noisium | sem config necessaria | nenhum contrato client-side necessario |
| Sodium Extra | manual | chaves variam por versao |
| Reese's Sodium Options | sem config necessaria | reorganiza UI, nao e otimizador proprio |

O catalogo registra URL da fonte oficial em cada avaliacao. Arquivo ausente ou
chave desconhecida nao e criado.

## Scientific Auto Optimize

O modo gera um plano a partir da auditoria e escolhe um perfil pelo gargalo. Ele
e intencionalmente guiado:

1. `Diagnosticar` produz dry-run;
2. `Novo experimento` fixa hipotese e instancia;
3. o usuario executa a cena baseline e `Benchmark 60 s`;
4. o motor registra baseline;
5. o usuario confirma apply; backup precede qualquer escrita;
6. o usuario repete a mesma cena e benchmark;
7. o motor compara;
8. o usuario confirma a decisao final;
9. somente `KEEP` permanece; `REVERT` e resultados inconclusivos restauram o
   backup gerenciado exato.

O modo nao abre o launcher sozinho, nao escolhe uma cena, nao altera mods e nao
simula FPS. Quarentena continua em um fluxo separado, com selecao humana e
confirmacao adicional quando pode afetar o servidor.

## Fluxo GUI para uma instancia Prism real

1. No Prism, crie uma instancia Minecraft 1.21.1 com Fabric.
2. Selecione Java 21 x64 na configuracao da instancia.
3. Instale Cobblemon e o conjunto exigido pelo servidor.
4. Inicie uma vez para gerar `options.txt` e `config`.
5. Feche o jogo e abra o ApexTweaker.
6. Na aba `Cobblemon`, selecione a pasta `.minecraft` da instancia. Exemplo:

```text
C:\Users\SEU_USUARIO\AppData\Roaming\PrismLauncher\instances\Cobblemon Low-End\.minecraft
```

7. Clique `Auditar` e resolva dependencias/conflitos marcados como erro.
8. Clique `Diagnosticar` e revise o relatorio em dry-run.
9. Clique `Novo experimento`.
10. Abra o jogo, entre na mesma rota/mundo/servidor e execute `Benchmark 60 s`.
11. Registre tempos, FPS e resultado nos campos da homologacao.
12. Clique `Avancar` para congelar o baseline.
13. Clique `Avancar` novamente e confirme a aplicacao do candidato.
14. Repita a mesma rota e duracao; execute outro benchmark.
15. Registre o candidato, compare e finalize.

Use sempre o mesmo launcher, mundo/servidor, rota, resolucao, clima, tempo de
captura e conjunto de mods. Caso contrario, o resultado e apenas indicativo.

## CLI completa

Crie o experimento:

```powershell
$dll = ".\ApexTweaker.dll"
$instance = "C:\PrismLauncher\instances\Cobblemon Low-End\.minecraft"
$root = "$env:LOCALAPPDATA\ApexTweaker\ScientificLab"

dotnet $dll --minecraft-scientific-start `
  --instance $instance --fps 30 --experiment-root $root
```

Copie o ID `exp-...` exibido. Com o jogo baseline aberto na cena de teste:

```powershell
dotnet $dll --minecraft-scientific-record `
  --experiment $id --phase baseline --benchmark-seconds 60 `
  --game-opened --menu-reached --server-entered --playable-720p `
  --menu-seconds 45 --join-seconds 80 --average-fps 28 --minimum-fps 14 `
  --experiment-root $root
```

Revise o relatorio e aplique:

```powershell
dotnet $dll --minecraft-scientific-apply `
  --experiment $id --experiment-root $root --yes
```

Repita exatamente a mesma cena e registre o candidato:

```powershell
dotnet $dll --minecraft-scientific-record `
  --experiment $id --phase candidate --benchmark-seconds 60 `
  --game-opened --menu-reached --server-entered --playable-720p `
  --menu-seconds 39 --join-seconds 68 --average-fps 34 --minimum-fps 18 `
  --experiment-root $root

dotnet $dll --minecraft-scientific-compare `
  --experiment $id --experiment-root $root
```

Finalize somente depois de ler a decisao:

```powershell
dotnet $dll --minecraft-scientific-finalize `
  --experiment $id --experiment-root $root --yes
```

`--yes` confirma a finalizacao e, para qualquer decisao diferente de `KEEP`, o
rollback gerenciado. Para listar ou inspecionar:

```powershell
dotnet $dll --minecraft-scientific-list --experiment-root $root
dotnet $dll --minecraft-scientific-show --experiment $id --experiment-root $root
```

## Pasta de mods real

Auditoria somente leitura:

```powershell
dotnet ApexTweaker.dll --minecraft-audit `
  --mods "C:\Users\igor.silva\Downloads\mods\mods" `
  --output ".\artifacts\cobblemon-audit-v3"
```

Essa pasta agregada nao e uma instancia cientifica: sem `options.txt`, `config`
e contexto do launcher, ela serve para auditoria e plano de quarentena. Copie
somente os mods validados para uma instancia Prism de teste. Nunca aplique
quarentena antes de comparar IDs e versoes com o manifesto exato do servidor.

## O que e automatico e o que e manual

Automatico e reversivel:

- leitura de hardware, Java, pagefile, processo, logs e hashes;
- auditoria/classificacao multitag de JARs;
- dry-run de configs e heap;
- backup atomico e apply das chaves suportadas;
- persistencia do experimento;
- comparacao ponderada;
- rollback direcionado e verificacao de hash.

Manual por seguranca ou falta de telemetria confiavel:

- instalar/remover qualquer mod;
- confirmar manifesto do servidor;
- criar e iniciar a instancia/rota;
- FPS medio, minimo e stutter percebido;
- fechar processos pesados;
- alterar resource packs e opcoes de acessibilidade;
- configs sem contrato fixado;
- confirmar apply, quarentena e rollback.

## Seguranca

- nenhum JAR e deletado;
- mods nao sao movimentados pelo experimento cientifico;
- toda escrita gerenciada tem backup e SHA-256;
- dry-run e baseline precedem apply;
- rollback nao usa simplesmente o backup mais recente: usa o ID do experimento;
- configs devem continuar iguais ao baseline ate o apply;
- arquivos alterados fora do plano impedem `KEEP`;
- caminhos e IDs sao validados;
- Defender e Windows Update nao sao desativados;
- pagefile nao e desativado;
- registry e servicos do Windows nao fazem parte deste fluxo;
- resultado sem evidencia continua `NOT_TESTED` ou inconclusivo.

## Fontes tecnicas fixadas

- [Sodium 0.6.13 options source](https://github.com/CaffeineMC/sodium/blob/mc1.21.1-0.6.13/common/src/main/java/net/caffeinemc/mods/sodium/client/gui/SodiumGameOptions.java)
- [ImmediatelyFast 1.6.11 config source](https://github.com/RaphiMC/ImmediatelyFast/blob/v1.6.11/common/src/main/java/net/raphimc/immediatelyfast/feature/core/ImmediatelyFastConfig.java)
- [EntityCulling 1.10.5 config source](https://github.com/tr7zw/EntityCulling/blob/1.10.5/EntityCulling-Versionless/src/main/java/dev/tr7zw/entityculling/versionless/Config.java)
- [Lithium configuration](https://github.com/CaffeineMC/lithium#configuration)
- [Prism Launcher instance guide](https://prismlauncher.org/wiki/getting-started/create-instance/)
- [Prism Launcher Java settings](https://prismlauncher.org/wiki/help-pages/java-settings/)

## Limitacoes restantes

- nao existe captura interna de FPS/1% low;
- GPU por processo permanece nao medida;
- comparacao de uma unica rota sofre variacao natural do jogo/servidor;
- primeiro carregamento de chunks nao deve ser comparado com chunks em cache;
- o motor nao conhece o manifesto privado do servidor;
- nenhuma ferramenta torna Cobblemon garantidamente jogavel em 4 GB;
- 8 GB em 2x4 GB e SSD continuam sendo recomendacoes de maior impacto.
