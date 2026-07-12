# ApexTweaker v2.2.0

Esta release transforma o Cobblemon Low-End Lab de uma auditoria parcial em um
fluxo operacional, reversivel e mensuravel.

## Implementado

- deteccao de instancias oficial, Prism, MultiMC, Modrinth, CurseForge e custom;
- dry-run com valores antes/depois;
- aplicacao real de `options.txt`;
- configuracao conservadora de Sodium, ImmediatelyFast e EntityCulling;
- memoria por instancia em Prism/MultiMC;
- backup e rollback transacionais, inclusive manifestos v2.1.0;
- quarentena de JARs com selecao explicita, SHA-256 e rollback separado;
- benchmark com ambiente antes/depois, processo, mods, configs, logs e crashes;
- estado `NotTested` quando Minecraft nao esta aberto;
- Plano de Sobrevivencia 4 GB;
- CLI completa para audit, dry-run, apply, rollback e benchmark.

## Validacao

- builds Debug e Release sem avisos ou erros;
- 10 verificacoes integradas aprovadas;
- 88 JARs reais auditados;
- 88 hashes preservados no dry-run;
- nenhum JAR real movido durante a validacao.

## Atencao

Confirme o manifesto do servidor antes de colocar Mega Showdown 1.7.3, Indium
ou qualquer mod visual em quarentena. ImmediatelyFast 1.6.11 e a ausencia de
performance prioritaria encontrada, mas deve ser testado em uma copia da
instancia.

Com 4 GB de RAM, a meta e estabilidade minima em 720p. O upgrade recomendado
continua sendo 8 GB, preferencialmente 2x4 GB em dual-channel, e SSD.
