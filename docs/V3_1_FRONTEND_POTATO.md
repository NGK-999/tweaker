# ApexTweaker v3.1 - Frontend e Potato Mode

## Decisao de tecnologia

Foi mantido **C#/.NET 10 + WPF**.

Razoes:

- a shell, o instalador, Win32/WMI e o motor cientifico ja usam .NET;
- WPF nao carrega Chromium e funciona sem uma segunda runtime;
- migrar para Electron seria incoerente com o alvo de 4 GB;
- a renderizacao do grafico usa `DrawingContext` e `StreamGeometry`, sem
  SkiaSharp, navegador ou processo auxiliar;
- o custo e risco de migrar a shell seria maior que o beneficio operacional.

Dependencia adicionada:

| Pacote | Versao | Motivo |
|---|---:|---|
| `CommunityToolkit.Mvvm` | `8.4.2` | `ObservableObject`, comandos, notificacao e estado testavel do wizard |

Nao foram adicionados WPF UI, LiveCharts2, ScottPlot, Serilog ou Ookii. O tema,
seletor de pasta, console e logging existentes atendem o fluxo. Bibliotecas de
grafico baseadas em Skia aumentariam tamanho e memoria sem necessidade para
tres series simples de 60 pontos.

Fontes:

- [CommunityToolkit.Mvvm no NuGet](https://www.nuget.org/packages/CommunityToolkit.Mvvm/8.4.2)
- [RelayCommand oficial](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/relaycommand)
- [Geradores MVVM oficiais](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/overview)

## Telas e fluxo

A pagina Cobblemon funciona como um wizard de dez etapas:

1. objetivo e limite fisico;
2. instancia e launcher;
3. diagnostico automatico;
4. Modpack Survival;
5. perfil completo ou hipotese isolada;
6. baseline;
7. candidato e diff;
8. pos-teste equivalente;
9. comparacao e confianca;
10. finalizacao, relatorios e rollback.

Elementos visuais:

- timeline de duas linhas para caber em resolucao baixa;
- progresso global e progresso real do benchmark;
- cards de mods e alertas;
- grafico de CPU, RAM Java e RAM livre;
- estado da etapa com cor e texto, nunca somente cor;
- rollback persistente no rodape;
- cancelamento explicito da amostragem;
- modo avancado recolhido por padrao;
- legenda para Medido, Manual, Inferido e Nao testado.

O modo simples guia o usuario. O modo avancado revela aplicacao direta,
argumentos JVM, quarentena, observacoes manuais, diffs e hashes.

## Memoria da interface

Smoke A/B local em 2026-07-12, depois de abrir a pagina Cobblemon nos
executaveis self-contained:

- v3.0.1: `221,7 MB` de working set / `148,6 MB` privados;
- v3.1.0: `220,3 MB` de working set / `147,9 MB` privados;
- processo auxiliar de navegador: nenhum;
- pontos do grafico por rodada: no maximo 60 no fluxo padrao.

A diferenca e pequena e deve ser tratada como ruido: nao houve aumento
mensuravel de memoria pela nova interface. O PC i3/4 GB ainda precisa validar
tempo de abertura e working set no hardware alvo.

## POTATO_COBBLEMON_4GB

Preset padrao:

```text
Janela: 960x540
Render distance: 2
Simulation distance: 5
Entity distance: 0.30
FPS cap: 24
VSync: off
Graphics: fast
Ambient occlusion: off
Clouds: off
Particles: minimal
Entity shadows: off
Mipmap: 0
Biome blend: 0
View bobbing: off
Screen/FOV effects: 0
Heap: -Xms512M -Xmx2048M
```

O perfil modifica somente chaves existentes em `options.txt`. Resource packs
locais sao desmarcados, nunca excluidos. Confirme packs exigidos pelo servidor.
O minimo de simulation distance foi fixado em 5; valores 3 e 4 sao rejeitados
pelo catalogo da versao 1.21.1.

A validacao dos nomes das opcoes usa os mappings oficiais do cliente 1.21.1,
SHA-1 `2244b6f072256667bcd9a73df124d6c58de77992`.

## Experimentos isolados

Catalogo fechado:

- resolucao: 1280x720, 960x540 e 854x480;
- FPS: 20, 24, 30, 45 e 60;
- render distance: 2, 3, 4 e 5;
- simulation distance: 5;
- entity distance: 0.30, 0.40, 0.50 e 0.75;
- qualidade visual minima;
- resource packs locais desligados;
- janela 854x480, 960x540 e 1280x720;
- heap 1792, 2048, 2304 e 2560 MB.

Cada preset persiste uma `MinecraftExperimentDefinition`. No baseline o plano e
recalculado e congelado. O apply aceita somente o mesmo ID e o mesmo conjunto
de mudancas. Drift bloqueia ou rebaixa para RETEST.

## Mods

Mantidos no baseline:

- Cobblemon e dependencias do servidor;
- Fabric API e Fabric Language Kotlin quando exigido;
- Sodium 0.6.13, Lithium, FerriteCore e EntityCulling;
- ModernFix ate existir benchmark isolado;
- exatamente a versao de Mega Showdown do servidor.

Experimento manual isolado:

- ImmediatelyFast 1.6.11+1.21.1 Fabric, inicialmente nos defaults.

Testar sem somente em copia:

- Indium, Iris, Distant Horizons, Continuity, EMF e ETF;
- Mega Showdown duplicado ou nao exigido.

EntityCulling e ImmediatelyFast nao recebem ajustes automaticos na v3.1. Bugs
visuais com Pokemon, HUD ou mapas exigem REVERT e analise manual.

## Configuracoes manuais

- instalar/remover JAR de ImmediatelyFast;
- whitelist do EntityCulling;
- mixins de Lithium ou ModernFix;
- configs de Sodium Extra, More Culling e Dynamic FPS;
- confirmar resource pack e manifesto do servidor;
- medir FPS/1% low externamente;
- decidir se fullscreen melhora na Intel HD.

## Configuracoes evitadas

- flags de GC copiadas da internet;
- mixins avancados sem diagnostico;
- chaves ausentes de `options.txt` ou JSON;
- desativacao de pagefile, Defender ou Windows Update;
- BCD, registro ou servicos no fluxo Minecraft;
- remocao automatica de mods;
- `-Xmx4G`.

## CLI

Potato em dry-run:

```powershell
ApexTweaker.exe --minecraft-profile-dry-run `
  --instance "C:\Prism\instances\Cobblemon\.minecraft" `
  --profile POTATO_COBBLEMON_4GB --fps 24
```

Experimento isolado:

```powershell
ApexTweaker.exe --minecraft-experiment-dry-run `
  --instance "C:\Prism\instances\Cobblemon\.minecraft" `
  --preset heap-1792
```

Para iniciar cientificamente, use `--minecraft-scientific-start` com o mesmo
`--preset`. Escritas continuam exigindo `--yes` nos comandos correspondentes.

## Riscos restantes

- a rodada real no i3/4 GB nao foi executada;
- FPS e 1% low continuam manuais;
- GPU por processo continua indisponivel sem contador confiavel;
- resource packs e mods visuais podem ser exigidos pelo servidor;
- 4 GB podem permanecer inviaveis mesmo em 854x480;
- o ganho de ImmediatelyFast, ModernFix ou heap diferente depende do modpack.
