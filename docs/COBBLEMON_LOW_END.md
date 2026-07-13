# Cobblemon Low-End Lab v3.2.1

## Objetivo

O modulo aplica otimizacoes reais, reversiveis e mensuraveis em uma instancia
Minecraft 1.21.1 + Fabric + Cobblemon. O alvo extremo e um Intel Core i3 de
quarta geracao com 4 GB de RAM e video integrado.

Nesse hardware, o objetivo e abrir o jogo e obter estabilidade minima em
1280x720. Nao existe promessa de FPS. O upgrade para 8 GB, preferencialmente
2x4 GB em dual-channel, e SSD continuam sendo as melhorias de maior impacto.

## Fluxos

Para uso comum, abra **Cobblemon Facil** e siga:

`Detectar -> Analisar -> Otimizar -> Testar -> Corrigir ou Restaurar`.

Os fluxos tecnicos abaixo permanecem em **Modo Avancado**.

1. `Audit`: le hardware, Java, pagefile e metadados dos JARs.
2. `Dry-run`: mostra arquivo, chave, valor atual e valor proposto.
3. `Apply`: cria backup e aplica o plano com escrita atomica.
4. `Rollback`: restaura o ultimo perfil e confere o hash original.
5. `Quarantine`: move somente JARs selecionados depois de backup SHA-256.
6. `Quarantine rollback`: restaura os JARs sem sobrescrever arquivo divergente.
7. `Benchmark`: registra ambiente, processo, mods, configs, logs e crashes.
8. `Manual recommendations`: gera o Plano de Sobrevivencia 4 GB.
9. `Operational checklist`: prepara a rodada real sem declarar sucesso antecipado.
10. `Operational result`: combina observacao manual com processo, logs e crashes.
11. `Scientific plan`: diagnostica o gargalo e gera um candidato em dry-run.
12. `Scientific experiment`: exige baseline, aplica uma unica hipotese,
    registra candidato, compara e mantem ou reverte.

## Instancias reconhecidas

- `.minecraft` oficial;
- Prism Launcher;
- MultiMC;
- Modrinth App;
- CurseForge;
- raiz customizada;
- selecao direta da subpasta `mods`.

Para aplicar perfil, a instancia precisa conter `options.txt` e `mods`. Prism e
MultiMC podem ser selecionados pela pasta da instancia ou por `.minecraft`.

## EXTREME_4GB

O perfil altera `options.txt` para:

- 1280x720 em janela;
- render distance 4;
- simulation distance 5, minimo validado para o controle vanilla 1.21.1;
- entity distance 0.5;
- graficos rapidos;
- particulas minimas;
- biome blend 0;
- mipmap 0;
- nuvens, sombras de entidades e VSync desligados;
- limite selecionavel de 20, 24, 30, 45 ou 60 FPS;
- screen effect 0.25 e FOV effect 0.50.

O perfil considera somente chaves existentes do Sodium e Iris:

- `config/sodium-options.json`;
- `config/iris.properties`, somente para `enableShaders=false`.

Se o arquivo ou a chave nao existir, nada e inventado. Opcoes experimentais e
debug nao sao ativadas. ImmediatelyFast e EntityCulling permanecem nos defaults
e devem ser comparados isoladamente por causa de bugs visuais possiveis.

## POTATO_COBBLEMON_4GB

Use somente quando o primeiro teste seguro ainda nao abre ou pagina demais:

- 960x540 em janela;
- render distance 2;
- simulation distance 5;
- entity distance 0.30;
- 24 FPS;
- graficos rapidos, AO, nuvens, sombras, bob view e efeitos desligados;
- particulas minimas, mipmap 0 e biome blend 0;
- resource packs locais desmarcados sem excluir arquivos;
- `-Xms512M -Xmx2048M`.

O modo facil tambem oferece a variante confirmada `854x480`, mantendo render 2,
simulation 5, entity distance 0.30, 24/30 FPS e heap de 2048 MB.

O Potato altera somente opcoes que ja existem no `options.txt`. Abra o jogo uma
vez antes. Resource packs exigidos pelo servidor devem ser confirmados antes.

Configs de Sodium Extra, More Culling, Dynamic FPS, ModernFix e Noisium sao
detectadas, mas permanecem sem escrita automatica nesta versao.

Lithium, FerriteCore, Noisium e Reese's Sodium Options nao recebem chaves
inventadas: quando o mod usa defaults seguros ou nao exige config para seu ganho
principal, o motor apenas registra o contrato. Consulte
[SCIENTIFIC_ENGINE.md](SCIENTIFIC_ENGINE.md) para a matriz completa.

## Memoria Java

O primeiro teste do `EXTREME_4GB` usa obrigatoriamente:

```text
-Xms512M -Xmx2048M
```

Somente rodadas posteriores, justificadas por logs e pagefile, podem comparar:

```text
-Xms512M -Xmx1792M
-Xms512M -Xmx2304M
-Xms512M -Xmx2560M
```

O `EXTREME_4GB` fica limitado a `-Xmx2048M`. Prism/MultiMC recebem
`OverrideMemory`, `MinMemAlloc` e `MaxMemAlloc` em `instance.cfg`. Outros
launchers recebem `apextweaker-java-args.txt` com instrucao manual.

Nao use `-Xmx4G` em uma maquina com 4 GB. Mantenha o pagefile ativo e gerenciado
pelo Windows, de preferencia em SSD.

## Backup e rollback do perfil

Backups ficam em:

```text
%LOCALAPPDATA%\ApexTweaker\MinecraftBackups
```

O manifesto guarda launcher, instancia, arquivos permitidos e SHA-256 antes e
depois. O rollback rejeita caminhos fora da lista gerenciada e restaura:

- `options.txt`;
- `apextweaker-java-args.txt`;
- JSON do Sodium, quando alterado;
- `iris.properties`, quando alterado;
- `instance.cfg`, quando alterado.

O rollback de perfil nao mexe na quarentena de mods.

## Quarentena

Construir o plano e um dry-run. Nenhum item e preselecionado na interface.

Ao aplicar:

1. o JAR precisa constar no plano atual;
2. o SHA-256 precisa ser igual ao hash da auditoria;
3. uma copia e criada em `MinecraftQuarantineBackups`;
4. a copia e conferida;
5. o original e movido para uma pasta irma `mods_quarantine_EXTREME_4GB_*`;
6. origem, destino, backup, hash e motivo entram no manifesto.

Nenhum JAR e excluido. Em falha parcial, os arquivos ja movidos sao restaurados.
Apply exige confirmacao explicita. Candidatos comuns aos dois lados tambem
exigem confirmacao separada de que o manifesto do servidor foi comparado.

## Auditoria real do pacote local

Pasta:

```text
C:\Users\igor.silva\Downloads\mods\mods
```

Resultado em 2026-07-12:

- 88 JARs Fabric;
- agregado SHA-256 do conjunto: `debf68ca0762e31af47c834648c361adbfc2f307763014749865484da895f935`;
- zero dependencias obrigatorias ausentes;
- um ID duplicado (`mega_showdown`);
- Sodium 0.6.13 fornece `indium`, mas existe Indium separado;
- ImmediatelyFast ausente;
- 88 hashes iguais antes/depois do dry-run;
- zero JARs movidos.

Candidatos encontrados:

| Arquivo | Risco | Acao |
|---|---|---|
| `mega_showdown-fabric-1.7.3+1.7.3+1.21.1.jar` | Alto | Confirmar versao do servidor; depois quarentenar a copia antiga |
| `indium-1.0.35+mc1.21.jar` | Medio | Testar sem ele porque Sodium 0.6.13 fornece o ID |
| `DistantHorizons-3.1.2-b-1.21.1-fabric-neoforge.jar` | Medio | Testar fora do perfil extremo |
| `iris-fabric-1.8.8+mc1.21.1.jar` | Medio | Testar sem shaders/Iris |
| `continuity-3.0.0+1.21.jar` | Medio | Preservar se resource pack depender |
| `entity_model_features-3.2.4-1.21-fabric.jar` | Medio | Testar sem modelos extras |
| `entity_texture_features_1.21-fabric-7.1.jar` | Medio | Testar sem texturas extras |

As duas primeiras aparecem como recomendadas pelo plano, mas continuam exigindo
confirmacao humana. Mega Showdown exige confirmacao de manifesto; Indium e
client-only e deve ser testado fora apenas em uma copia. As cinco visuais nao
sao selecionadas automaticamente.

## Benchmark

O benchmark aguarda um processo Java do Minecraft e registra:

- ambiente antes/depois;
- RAM e CPU por segundo;
- commit usado por segundo, quando a API do Windows o fornece;
- pico de working set e private memory;
- menor RAM fisica livre;
- JARs ativos;
- SHA-256 de `options.txt` e configs;
- cauda de `logs/latest.log`;
- crash report criado durante a janela;
- evidencias de `OutOfMemoryError` e crash.

Estados:

- `Approved`: processo permaneceu ativo sem evidencia de crash/OOM e sem RAM
  criticamente baixa;
- `Unstable`: RAM livre caiu abaixo de 0,40 GB;
- `Failed`: processo encerrou, houve crash/OOM ou amostras insuficientes;
- `NotTested`: nenhum processo Minecraft foi detectado.

FPS nunca e inventado. Use F3, Spark, PresentMon ou ferramenta equivalente.

## Interface guiada

O wizard possui dez etapas: objetivo, instancia, diagnostico, mods, modo,
baseline, candidato, pos-teste, resultado e finalizacao. O modo simples mostra
o caminho essencial. O modo avancado revela diffs, quarentena, hashes,
observacoes manuais e argumentos JVM.

Para o fluxo cientifico, use o cartao verde no topo da aba:

1. `Diagnosticar`: gera somente o plano e nao escreve arquivos.
2. `Novo experimento`: congela hipotese, hashes e perfil candidato.
3. Com o jogo na cena de teste, execute `Benchmark 60 s`.
4. Preencha a observacao guiada e avance para registrar o baseline.
5. Avance novamente, revise o dialogo e confirme a aplicacao com backup.
6. Repita exatamente a mesma cena e o benchmark.
7. Registre o candidato, compare e finalize.
8. Somente `KEEP` permanece aplicado. `REVERT`, `RETEST` e
   `INSUFFICIENT_DATA` restauram o backup gerenciado depois de nova confirmacao.

## CLI

Plano cientifico somente leitura:

```powershell
dotnet ApexTweaker.dll --minecraft-scientific-plan `
  --instance "C:\caminho\da\instancia" --fps 30
```

Experimentos completos estao documentados em
[SCIENTIFIC_ENGINE.md](SCIENTIFIC_ENGINE.md).

Auditoria:

```powershell
dotnet ApexTweaker.dll --minecraft-audit `
  --mods "$env:USERPROFILE\Downloads\mods\mods" `
  --output ".\artifacts\cobblemon-audit"
```

Dry-run do perfil:

```powershell
dotnet ApexTweaker.dll --minecraft-profile-dry-run `
  --instance "C:\caminho\da\instancia" `
  --profile EXTREME_4GB --fps 30
```

Apply e rollback do perfil:

```powershell
dotnet ApexTweaker.dll --minecraft-apply-profile `
  --instance "C:\caminho\da\instancia" `
  --profile EXTREME_4GB --fps 30 --yes

dotnet ApexTweaker.dll --minecraft-rollback `
  --instance "C:\caminho\da\instancia" --yes
```

Dry-run e apply da quarentena:

```powershell
dotnet ApexTweaker.dll --minecraft-quarantine-dry-run `
  --mods "$env:USERPROFILE\Downloads\mods\mods"

dotnet ApexTweaker.dll --minecraft-quarantine-apply `
  --mods "$env:USERPROFILE\Downloads\mods\mods" `
  --files "arquivo-a.jar;arquivo-b.jar" --yes
```

Rollback da quarentena:

```powershell
dotnet ApexTweaker.dll --minecraft-quarantine-rollback `
  --mods "$env:USERPROFILE\Downloads\mods\mods" --yes
```

Benchmark:

```powershell
dotnet ApexTweaker.dll --minecraft-benchmark `
  --instance "C:\caminho\da\instancia" `
  --seconds 60 --wait-seconds 30
```

Checklist e homologacao:

```powershell
dotnet ApexTweaker.dll --minecraft-operational-checklist `
  --mods "$env:USERPROFILE\Downloads\mods\mods" `
  --instance "C:\caminho\da\instancia" --fps 30

dotnet ApexTweaker.dll --minecraft-homologation-report `
  --instance "C:\caminho\da\instancia" `
  --game-opened --menu-reached --server-entered --playable-720p `
  --average-fps 30 --minimum-fps 15
```

## Leitura complementar

- [Autoauditoria v2.1.0 -> v2.2.0](V2_2_IMPLEMENTATION_AUDIT.md)
- [Matriz Fabric 1.21.1](COBBLEMON_COMPATIBILITY_1.21.1.md)
- [Homologacao operacional no PC real](HOMOLOGACAO_OPERACIONAL_COBBLEMON.md)
