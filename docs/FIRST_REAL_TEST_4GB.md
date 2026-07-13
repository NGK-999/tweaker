# Primeiro teste seguro em 4 GB

Versao: **ApexTweaker 3.2.0**

Este roteiro prepara o primeiro teste real no Intel Core i3 de quarta geracao,
4 GB de RAM e Intel HD. O objetivo e abrir o jogo, entrar no servidor e obter
uma medicao honesta. Nao existe promessa de 30 FPS.

## Privilegio

- Execute `ApexTweaker.exe` normalmente. O fluxo Minecraft nao exige UAC.
- Nao execute Prism, Java ou Minecraft como administrador.
- UAC aparece somente ao escolher Auto-Tuning, modulos, restore point ou
  rollback de sistema do Windows.
- Instalacao em Program Files pode exigir administrador; executar o app nao.

Dados Minecraft ficam em `%LOCALAPPDATA%\ApexTweaker`. Backups de mutacoes do
Windows continuam em `C:\ProgramData\ApexTweaker\Backups`.

## Preparacao

1. Baixe `ApexTweaker-Portable-v3.2.0.zip` da release autenticada.
2. Confira o SHA-256 publicado e extraia em `C:\ApexTweaker\v3.2.0-portable`.
3. Confirme que `ApexTweaker.exe` e `ApexTweaker.Native.dll` estao juntos.
4. No Prism Launcher, crie `Cobblemon-1.21.1-EXTREME-4GB`.
5. Selecione Minecraft `1.21.1`, Fabric e Java `21` x64.
6. Configure inicialmente `512 MB` minimo e `2048 MB` maximo.
7. Abra a instancia sem mods uma vez, chegue ao menu e feche.
8. Copie os JARs para `.minecraft\mods`; nunca mova a pasta original.
9. Abra uma vez com os mods para gerar configs e feche o jogo.
10. Compare IDs/versoes com o manifesto exato do servidor.

## Mods do primeiro baseline

Manter:

- Cobblemon e dependencias exigidas pelo servidor;
- Fabric API;
- Fabric Language Kotlin quando exigido;
- Sodium 0.6.13;
- Lithium;
- FerriteCore;
- EntityCulling;
- ModernFix ate benchmark isolado.

Nao retirar no primeiro baseline. Depois, testar sem apenas em uma copia:

- Indium separado;
- Iris;
- Distant Horizons;
- Continuity;
- Entity Model Features (EMF);
- Entity Texture Features (ETF);
- Mega Showdown duplicado ou versao nao exigida pelo servidor.

ImmediatelyFast `1.6.11+1.21.1 Fabric` entra em um experimento separado, nunca
misturado com a primeira aplicacao do perfil.

## Preset inicial obrigatorio

Use `EXTREME_4GB` com:

- resolucao 1280x720 em janela;
- render distance 4;
- simulation distance 5;
- entity distance 0.50;
- FPS 30;
- VSync desligado;
- shaders desligados;
- resource packs pesados desligados;
- mipmap 0;
- biome blend 0;
- particulas minimas;
- `-Xms512M -Xmx2048M`.

Nao comece com 2304 ou 2560 MB. Esses valores sao hipoteses posteriores caso os
logs indiquem heap insuficiente sem pressao excessiva de pagefile.

Se o jogo ainda nao abrir, crie uma nova rodada com `POTATO_COBBLEMON_4GB`.
Nao misture Potato, heap 1792 MB e remocao de mod na mesma hipotese.

## Experimento real

1. Abra o ApexTweaker em modo normal e selecione a pasta `.minecraft` do Prism.
2. Clique `Auditar` e leia duplicidades, dependencias e conflitos.
3. Nao aplique quarentena sem selecao e confirmacao explicitas.
4. Clique `Diagnosticar` e confirme `EXTREME_4GB`, 30 FPS e heap 2048 MB.
5. Clique `Novo experimento`.
6. Abra o jogo ainda sem o candidato, execute a mesma rota e entre no servidor.
7. Execute `Benchmark 60 s` e registre menu, entrada, FPS observado e quedas.
8. Avance para congelar o baseline.
9. Avance novamente e confirme o apply do candidato com backup.
10. Repita a mesma rota, servidor, resolucao e duracao.
11. Execute outro `Benchmark 60 s` e registre o candidato.
12. Compare e finalize.

Decisoes:

- `KEEP`: mantem somente o candidato que apresentou melhora suficiente;
- `REVERT`: restaura o backup exato;
- `RETEST` ou `INSUFFICIENT_DATA`: restaura o candidato gerenciado e exige nova rodada.

## Evidencias e logs

Automatico:

- CPU e RAM do processo Java;
- RAM livre, pagefile e I/O;
- hashes de configs e mods;
- `latest.log` e crash report;
- sinais de crash, OOM, config e mismatch conhecidos.

Informado pelo usuario:

- FPS medio/minimo;
- tempo ate menu e servidor;
- stutter e quedas severas;
- confirmacao de entrada no servidor.

Nao disponivel automaticamente:

- FPS e 1% low;
- GPU percentual confiavel por processo.

Arquivos a preservar depois da rodada:

```text
.minecraft\logs\latest.log
.minecraft\crash-reports\*
%LOCALAPPDATA%\ApexTweaker\MinecraftReports\*
%LOCALAPPDATA%\ApexTweaker\MinecraftExperiments\*
```

## Criterio minimo

- jogo abriu e chegou ao menu;
- mundo/servidor carregou;
- nao houve fechamento por falta de memoria;
- 720p permaneceu minimamente jogavel;
- nenhum mod obrigatorio foi perdido;
- logs, hashes e decisao foram preservados.

Se falhar com OOM, nao aumente o heap imediatamente. Primeiro confira RAM livre,
pagefile, resource packs, mods visuais e `latest.log`. O upgrade para 8 GB e SSD
continua sendo a recomendacao de maior impacto.
