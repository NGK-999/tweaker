# Cobblemon Low-End Lab

## Objetivo

O modulo prepara uma analise defensavel para Minecraft 1.21.1 + Fabric +
Cobblemon em PCs com pouca RAM. Ele nao promete FPS e nao remove mods com base
apenas no nome do arquivo.

Para 4 GB fisicos, o objetivo e experimental: abrir o jogo, entrar no servidor
e manter estabilidade minima em 1280x720. O upgrade para 8 GB, idealmente 2x4
GB, continua sendo a mudanca de maior impacto, principalmente com iGPU Intel.

## Arquitetura

O codigo fica isolado em `src/Minecraft`:

| Componente | Responsabilidade |
|---|---|
| `ModJarScanner` | Le metadados, hashes e JARs aninhados sem escrever nos mods |
| `MinecraftAuditService` | Dependencias, duplicidades, conflitos e classificacao |
| `MinecraftEnvironmentService` | CPU, GPU, RAM, pagefile, Java, discos e launchers |
| `MinecraftProfileService` | Perfis de `options.txt`, backup e rollback |
| `MinecraftBenchmarkService` | Amostras de CPU/RAM do processo Java |
| `MinecraftReportService` | Saida JSON, Markdown e TXT |
| `MinecraftCommandLine` | Automacao auditavel sem abrir a GUI |

O fluxo Minecraft nao usa o pipeline de mutacoes do Windows porque os alvos e o
modelo de rollback sao diferentes. Backups ficam em
`C:\ProgramData\ApexTweaker\MinecraftBackups`.

## Regras de seguranca

- JARs sao abertos com acesso de leitura.
- Nenhum mod e excluido, sobrescrito ou movido automaticamente.
- `SERVER_REQUIRED_POSSIVEL` nunca deve ser retirado sem comparar o manifesto do
  servidor.
- Perfis exigem uma raiz com `options.txt` e subpasta `mods`.
- Antes da escrita, `options.txt` e o arquivo de argumentos sao versionados.
- Escritas usam arquivo temporario e substituicao no mesmo diretorio.
- Rollback aceita somente os dois arquivos gerenciados; caminhos fora da
  instancia sao rejeitados.
- Argumentos JVM sao gravados como recomendacao em
  `apextweaker-java-args.txt`; o usuario decide se os aplica no launcher.
- Cada auditoria cria `quarantine-suggestions-*` com um plano JSON; a pasta nao
  contem JARs e nao executa movimentacao automatica.

## Classificacoes

| Classe | Significado |
|---|---|
| `ESSENCIAL_PROVAVEL` | Mod principal ou requisito muito provavel |
| `PERFORMANCE` | Mod conhecido de desempenho |
| `DEPENDENCIA` | Biblioteca/API consumida por outros mods |
| `CLIENT_ONLY` | O proprio metadata declara ambiente client |
| `SERVER_REQUIRED_POSSIVEL` | Conteudo comum aos dois lados; preservar por padrao |
| `REMOVIVEL_PROVAVEL` | Candidato de teste no perfil extremo, nunca automatico |
| `INCOMPATIVEL_POSSIVEL` | Duplicidade, dependencia ausente ou conflito declarado |
| `DESCONHECIDO` | Evidencia insuficiente |

## Perfis

| Perfil | Render / simulacao | FPS | Uso |
|---|---:|---:|---|
| `SAFE` | 8 / 5 | 60 | Reducao moderada |
| `LOW_END` | 6 / 4 | 60 | Hardware antigo com mais memoria |
| `EXTREME_4GB` | 4 / 4 | 45 | 720p, RAM e estabilidade |
| `COBBLEMON_SERVER_CLIENT` | 5 / 4 | 60 | Cliente leve sem tocar no conjunto de mods |
| `BENCHMARK` | 6 / 4 | 120 | Comparacao A/B sem VSync |

Todos desligam nuvens, sombras de entidades e VSync. O perfil extremo usa
particulas minimas, mipmap 0, biome blend 0, graphics fast e entity distance
0.5.

Para uma maquina com aproximadamente 4 GB, a recomendacao JVM fica entre:

```text
-Xms512M -Xmx2048M
-Xms512M -Xmx2304M
-Xms512M -Xmx2560M
```

O valor e escolhido pela RAM fisica e livre. Nao use `-Xmx4G` em uma maquina
com 4 GB. Mantenha o pagefile ativo e gerenciado pelo Windows, de preferencia
em SSD.

## Auditoria local de julho de 2026

A auditoria somente leitura da pasta fornecida encontrou:

- 88 JARs Fabric;
- 7 mods classificados como performance;
- nenhuma dependencia obrigatoria ausente depois de considerar JARs aninhados;
- duas versoes do ID `mega_showdown`;
- `Sodium 0.6.13` fornecendo `indium` enquanto um JAR de Indium separado tambem
  esta instalado;
- metadados JSON malformados em tres JARs, normalizados apenas em memoria para
  analise;
- Distant Horizons, Iris, Continuity, Entity Model Features e Entity Texture
  Features como candidatos de teste fora do perfil extremo.

Acao manual mais urgente:

1. Confirmar com o servidor qual Mega Showdown e exigido.
2. Manter `mega_showdown 1.8.4` e colocar a copia `1.7.3` em quarentena apenas
   depois dessa confirmacao.
3. Testar sem o JAR separado do Indium, pois Sodium 0.6.13 ja declara que
   fornece esse ID.
4. Testar sem Distant Horizons, Iris e recursos visuais pesados em uma copia da
   instancia.

## Recomendacoes de mods

Camada 1, essencial e segura:

- Fabric API;
- Fabric Language Kotlin, ja aninhado no Cobblemon auditado;
- Cobblemon;
- Sodium;
- Lithium;
- FerriteCore;
- ImmediatelyFast para Fabric 1.21-1.21.1.

Camada 2, testar no pacote completo:

- ModernFix;
- Entity Culling;
- More Culling 1.0.1 com Cloth Config;
- Dynamic FPS 3.7.7.

Camada 3, experimental ou situacional:

- FastQuit, util principalmente em single-player;
- Noisium 2.3.0, util no servidor integrado ou quando instalado no servidor;
- Sodium Extra para controles visuais;
- Reese's Sodium Options e Mod Menu para interface, nao para FPS direto.

Camada 4, evitar no `EXTREME_4GB`:

- shaders e Iris;
- Distant Horizons;
- Indium separado com Sodium 0.6.x;
- resource packs pesados;
- mods cosmeticos de modelos e texturas de entidades.

Fontes primarias consultadas:

- [Cobblemon Installation](https://wiki.cobblemon.com/index.php/Guides/Installation)
- [ImmediatelyFast](https://modrinth.com/mod/immediatelyfast)
- [ModernFix](https://modrinth.com/mod/modernfix)
- [Entity Culling](https://modrinth.com/mod/entityculling)
- [More Culling](https://modrinth.com/mod/moreculling)
- [Dynamic FPS](https://modrinth.com/mod/dynamic-fps)
- [FastQuit](https://modrinth.com/mod/fastquit)
- [Noisium source](https://github.com/Steveplays28/noisium)

## Uso

Auditar qualquer pasta de mods:

```powershell
dotnet run --project ApexTweaker.csproj -- `
  --minecraft-audit `
  --mods "$env:USERPROFILE\Downloads\mods\mods" `
  --output ".\artifacts\cobblemon-audit"
```

Aplicar o perfil somente em uma instancia completa:

```powershell
dotnet run --project ApexTweaker.csproj -- `
  --minecraft-apply-profile `
  --instance "$env:APPDATA\.minecraft" `
  --profile EXTREME_4GB `
  --yes
```

Rollback:

```powershell
dotnet run --project ApexTweaker.csproj -- `
  --minecraft-rollback `
  --instance "$env:APPDATA\.minecraft" `
  --yes
```

Benchmark de 60 segundos com o jogo aberto:

```powershell
dotnet run --project ApexTweaker.csproj -- `
  --minecraft-benchmark `
  --seconds 60
```

O benchmark nao injeta codigo e nao mede FPS. Para FPS medio e 1% low, use uma
ferramenta externa e mantenha mundo, rota, resolucao e configuracao identicos.
