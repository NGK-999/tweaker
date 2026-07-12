# Cobblemon Low-End Lab v2.2.0

## Objetivo

O modulo aplica otimizacoes reais, reversiveis e mensuraveis em uma instancia
Minecraft 1.21.1 + Fabric + Cobblemon. O alvo extremo e um Intel Core i3 de
quarta geracao com 4 GB de RAM e video integrado.

Nesse hardware, o objetivo e abrir o jogo e obter estabilidade minima em
1280x720. Nao existe promessa de FPS. O upgrade para 8 GB, preferencialmente
2x4 GB em dual-channel, e SSD continuam sendo as melhorias de maior impacto.

## Fluxos

1. `Audit`: le hardware, Java, pagefile e metadados dos JARs.
2. `Dry-run`: mostra arquivo, chave, valor atual e valor proposto.
3. `Apply`: cria backup e aplica o plano com escrita atomica.
4. `Rollback`: restaura o ultimo perfil e confere o hash original.
5. `Quarantine`: move somente JARs selecionados depois de backup SHA-256.
6. `Quarantine rollback`: restaura os JARs sem sobrescrever arquivo divergente.
7. `Benchmark`: registra ambiente, processo, mods, configs, logs e crashes.
8. `Manual recommendations`: gera o Plano de Sobrevivencia 4 GB.

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
- simulation distance 4;
- entity distance 0.5;
- graficos rapidos;
- particulas minimas;
- biome blend 0;
- mipmap 0;
- nuvens, sombras de entidades e VSync desligados;
- limite inicial de 45 FPS;
- screen effect 0.25 e FOV effect 0.50.

O perfil considera somente chaves existentes nos JSONs abaixo:

- `config/sodium-options.json`;
- `config/immediatelyfast.json`;
- `config/entityculling.json`.

Se o arquivo ou a chave nao existir, nada e inventado. Opcoes experimentais e
debug do ImmediatelyFast nao sao ativadas.

Configs de Sodium Extra, More Culling, Dynamic FPS, ModernFix e Noisium sao
detectadas, mas permanecem sem escrita automatica nesta versao.

## Memoria Java

O heap e escolhido de acordo com a RAM livre e limitado pelo perfil:

```text
-Xms512M -Xmx2048M
-Xms512M -Xmx2304M
-Xms512M -Xmx2560M
```

O `EXTREME_4GB` escolhe 2048, 2304 ou 2560 MB conforme a memoria livre e nunca
passa de `-Xmx2560M`. Prism/MultiMC recebem `OverrideMemory`, `MinMemAlloc` e `MaxMemAlloc` em `instance.cfg`. Outros
launchers recebem `apextweaker-java-args.txt` com instrucao manual.

Nao use `-Xmx4G` em uma maquina com 4 GB. Mantenha o pagefile ativo e gerenciado
pelo Windows, de preferencia em SSD.

## Backup e rollback do perfil

Backups ficam em:

```text
C:\ProgramData\ApexTweaker\MinecraftBackups
```

O manifesto guarda launcher, instancia, arquivos permitidos e SHA-256 antes e
depois. O rollback rejeita caminhos fora da lista gerenciada e restaura:

- `options.txt`;
- `apextweaker-java-args.txt`;
- os tres JSONs suportados, quando alterados;
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

## Auditoria real do pacote local

Pasta:

```text
C:\Users\igor.silva\Downloads\mods\mods
```

Resultado em 2026-07-12:

- 88 JARs Fabric;
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
confirmacao humana. As cinco visuais nao sao selecionadas automaticamente.

## Benchmark

O benchmark aguarda um processo Java do Minecraft e registra:

- ambiente antes/depois;
- RAM e CPU por segundo;
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

## Interface

1. Abra `Cobblemon`.
2. Selecione a pasta `mods` ou a instancia.
3. Clique `Auditar`.
4. Leia o Plano de Sobrevivencia 4 GB.
5. Em uma instancia completa, clique `Previsualizar (dry-run)`.
6. Revise o relatorio antes/depois.
7. Clique `Aplicar plano com backup` e confirme.
8. Para JARs, selecione candidatos individualmente e confirme a quarentena.
9. Teste a entrada no servidor.
10. Use o rollback correspondente se houver falha.

## CLI

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
  --profile EXTREME_4GB
```

Apply e rollback do perfil:

```powershell
dotnet ApexTweaker.dll --minecraft-apply-profile `
  --instance "C:\caminho\da\instancia" `
  --profile EXTREME_4GB --yes

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

## Leitura complementar

- [Autoauditoria v2.1.0 -> v2.2.0](V2_2_IMPLEMENTATION_AUDIT.md)
- [Matriz Fabric 1.21.1](COBBLEMON_COMPATIBILITY_1.21.1.md)
