# ApexTweaker v3.3.0 - Minecraft geral e Session Hooks

## Mudancas principais

- `Cobblemon Facil` tornou-se `Minecraft Facil`;
- instancias vanilla sem pasta `mods` agora sao validas depois do primeiro
  `options.txt`;
- Cobblemon passou a ser um perfil de conteudo opcional detectado;
- loader e inferido pelos JARs, sem recomendar o catalogo Fabric para
  vanilla, Forge ou NeoForge;
- perfis visiveis `POTATO_4GB` e `POTATO_4GB_480P`;
- aliases antigos de Cobblemon preservados para backups e scripts;
- teste separa mundo local de servidor;
- localizador do Java exige correspondencia com a instancia selecionada;
- hooks de sessao `Off`, `Safe` e `Extreme`;
- diario de recuperacao, rollback idempotente e relatorio JSON;
- relatorio de hooks incluido no ZIP de diagnostico;
- CLI aceita `--hooks off|safe|extreme` e recuperacao pendente.

## Catalogo restrito de hooks

- Safe: AboveNormal, boost do scheduler e HighQoS documentado;
- Extreme: High, P-cores somente em topologia hibrida confirmada e Power Mode
  Best Performance somente com snapshot AC/DC;
- nunca RealTime, kernel hook, injecao, BCD, Registro, servicos, Defender,
  Windows Update ou pagefile.

## Seguranca

- manifesto continua `asInvoker`;
- hooks nao exigem administrador;
- o Java de outra instancia nunca e escolhido como fallback;
- boost, QoS e afinidade so sao aplicados quando o estado anterior foi capturado;
- nenhuma operacao do modo facil move ou exclui JARs;
- backup e rollback de configs permanecem inalterados;
- FPS continua manual quando nao medido.

## Validacao

- build Release: 0 avisos e 0 erros;
- self-test: `SELF_TEST_OK`;
- teste Win32 Safe: prioridade/boost aplicados e restaurados; QoS sem snapshot
  foi ignorado; zero diarios pendentes;
- teste Win32 Extreme: High, afinidade hibrida e Best Performance aplicados e
  restaurados; zero diarios pendentes;
- layout validado em 1180x780 e 980x680;
- manifesto publicado: `asInvoker`;
- NuGet: nenhum pacote vulneravel nas fontes consultadas;
- vanilla sem mods, Fabric/Cobblemon, aliases legados, mundo/servidor, journal,
  rollback idempotente e recuperacao de hooks cobertos;
- teste fisico no i3 de quarta geracao/4 GB continua obrigatorio.

## Assets locais

| Asset | SHA-256 |
|---|---|
| `ApexTweaker.exe` | `0ff9d65c6e67b3a9bb91c6cda586573d0df43c1fbd781635a905fc160ca150fa` |
| `ApexTweaker.Native.dll` | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |
| `ApexTweaker-Portable-v3.3.0.zip` | `72b222e674f3aa830d334773bb5d22be65555e55c0b35659022d2b43707ebfde` |
| `ApexTweaker-Setup.exe` | `4daf50470a0fd4b8f211af8b13d7b6571e81d230b43dedafcddfbc7ad3d81ab6` |

O ZIP portatil e o caminho recomendado sem UAC. O instalador atual solicita
administrador somente para gravar em `Program Files`; o executavel instalado
continua `asInvoker`.
