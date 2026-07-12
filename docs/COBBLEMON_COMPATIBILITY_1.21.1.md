# Matriz Fabric 1.21.1

Consulta realizada em 2026-07-12 pela API oficial do Modrinth, filtrando
`loader=fabric`, `game_version=1.21.1` e releases estaveis. Esta tabela comprova
que existe uma versao compativel; ela nao autoriza upgrade automatico de um
modpack de servidor.

| Projeto | Release Fabric 1.21.1 validada | Papel |
|---|---|---|
| Fabric API | [0.116.13+1.21.1](https://modrinth.com/mod/fabric-api/version/FHknjVVa) | Base |
| Fabric Language Kotlin | [1.13.12+kotlin.2.4.0](https://modrinth.com/mod/fabric-language-kotlin/version/Pd0xrHCw) | Runtime Kotlin |
| Cobblemon | [1.7.3](https://modrinth.com/mod/cobblemon/version/kF7CvxTo) | Mod principal |
| Sodium | [0.8.12](https://modrinth.com/mod/sodium/version/KIRFiWG4) | Renderizacao |
| Lithium | [0.15.4](https://modrinth.com/mod/lithium/version/N08Z8wog) | Ticks/logica |
| FerriteCore | [7.0.3](https://modrinth.com/mod/ferrite-core/version/sOzRw3CG) | Memoria |
| ImmediatelyFast | [1.6.11](https://modrinth.com/mod/immediatelyfast/version/ATB4eNEP) | HUD/texto/render imediato |
| ModernFix | [5.25.1](https://modrinth.com/mod/modernfix/version/NnNX8LBn) | Memoria/carregamento |
| EntityCulling | [1.10.5](https://modrinth.com/mod/entityculling/version/hsWvcyFJ) | Culling de entidades |
| More Culling | [1.0.7](https://modrinth.com/mod/moreculling/version/y4J2jK6V) | Culling adicional |
| Dynamic FPS | [3.11.4](https://modrinth.com/mod/dynamic-fps/version/GBH14HiF) | Economia em segundo plano |
| FastQuit | [3.0.0+1.20.6](https://modrinth.com/mod/fastquit/version/dIGKewCo) | Saida de single-player |
| Noisium | [2.3.0](https://modrinth.com/mod/noisium/version/4sGQgiu2) | Worldgen |
| Sodium Extra | [0.9.3](https://modrinth.com/mod/sodium-extra/version/M9kVcb0e) | Controles visuais |
| Reese's Sodium Options | [2.2.3](https://modrinth.com/mod/reeses-sodium-options/version/jDOK2MQs) | Interface de opcoes |
| Mod Menu | [11.0.4](https://modrinth.com/mod/modmenu/version/v6Xx3fbU) | Interface de mods |
| Cloth Config | [15.0.140](https://modrinth.com/mod/cloth-config/version/HpMb5wGb) | Dependencia de More Culling |

## Pacote local auditado

O pacote local usa Cobblemon 1.7.3, Sodium 0.6.13, Lithium 0.15.4,
FerriteCore 7.0.3, ModernFix 5.25.1 e EntityCulling 1.10.5. ImmediatelyFast
esta ausente.

Nao atualize Sodium 0.6.13 para 0.8.12 isoladamente. Primeiro clone a
instancia, confirme addons, configs, Java e entrada no servidor. Para o pacote
atual, o Indium separado e redundante porque Sodium 0.6.x ja incorpora essa
compatibilidade.

More Culling 1.0.7 declara Cloth Config como dependencia obrigatoria.
EntityCulling 1.10.5 declara Fabric API como dependencia obrigatoria.

## Contratos usados pelo perfil

- [Sodium 0.6.13 - `SodiumGameOptions`](https://github.com/CaffeineMC/sodium/blob/mc1.21.1-0.6.13/common/src/main/java/net/caffeinemc/mods/sodium/client/gui/SodiumGameOptions.java)
- [ImmediatelyFast 1.6.11 - config regular](https://github.com/RaphiMC/ImmediatelyFast/blob/v1.6.11/common/src/main/java/net/raphimc/immediatelyfast/feature/core/ImmediatelyFastConfig.java)
- [EntityCulling 1.10.5 - config](https://github.com/tr7zw/EntityCulling/blob/1.10.5/EntityCulling-Versionless/src/main/java/dev/tr7zw/entityculling/versionless/Config.java)
- [Prism Launcher - Java e memoria](https://prismlauncher.org/wiki/help-pages/java-settings/)
