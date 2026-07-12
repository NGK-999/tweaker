# Homologacao operacional Cobblemon em 4 GB

Versao do fluxo: **ApexTweaker 3.0.0**
Alvo: **Minecraft 1.21.1 + Fabric + Cobblemon em Windows, i3 de quarta geracao e 4 GB de RAM**

## Estado comprovado em 2026-07-12

- O repositorio `NGK-999/tweaker` existe, mas esta **privado**.
- Sem login, repositorio, commit, release e assets retornam `404`.
- Com a conta autorizada, o commit `008587c6bd7c75d6687e9368ee82fab880d1ca85`
  e a release `v2.2.0` existem.
- A release autenticada contem `ApexTweaker-Setup.exe`,
  `ApexTweaker-Portable-v2.2.0.zip`, `ApexTweaker.exe` e
  `ApexTweaker.Native.dll`.
- O pacote real em `C:\Users\igor.silva\Downloads\mods\mods` contem 88 JARs.
- O dry-run operacional preservou os 88 arquivos e todos os SHA-256.
- Nenhuma pasta `mods_quarantine_EXTREME_4GB_*` foi criada no dry-run.
- Ainda nao existe evidencia de jogo aberto, FPS ou entrada no servidor no PC
  alvo. O estado operacional continua **NAO_TESTADO** ate essa rodada real.

Hashes publicados da base v2.2.0:

| Asset | SHA-256 |
|---|---|
| `ApexTweaker-Portable-v2.2.0.zip` | `ac225485d9d228423a41e90b89bbe9b7315eb5daef783aab2b97a9b17d0d7656` |
| `ApexTweaker-Setup.exe` | `999bf9f7b0b08528cd5e288d1c16dda90ed91ce86ab522fb67224d27844319ce` |
| `ApexTweaker.exe` | `5faf7bd3e43b9e43c0ecafee0f90b0dc4e01ed7c90c3e648aad8dceb4a5c097e` |
| `ApexTweaker.Native.dll` | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |

Hashes historicos dos artefatos v2.3.0 validados localmente antes do upload:

| Asset | SHA-256 |
|---|---|
| `ApexTweaker-Portable-v2.3.0.zip` | `04129ecbd1ac8cb5a6865c811d07bd706d99f79c06b6b17e89c3e67dc78b50e1` |
| `ApexTweaker-Setup.exe` | `e87f88a89f6cb11773e872116218597ddcb7364b5bd3fdedcfda765a702a909d` |
| `ApexTweaker.exe` | `1789606c537c43e31086886c03a429963fd130311b73ce54f5de344fd288e363` |
| `ApexTweaker.Native.dll` | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |

Nao torne o repositorio publico apenas para evitar o `404`. Visibilidade e uma
decisao separada e deve ser confirmada pelo proprietario.

## Checklist do ZIP portatil

1. Entre no GitHub com a conta que possui acesso ao repositorio privado.
2. Abra a release desejada e baixe o ZIP portatil, nao um arquivo de terceiro.
3. Confira o SHA-256 antes de extrair:

```powershell
Get-FileHash "$env:USERPROFILE\Downloads\ApexTweaker-Portable-v2.3.0.zip" -Algorithm SHA256
```

4. Compare o hash inteiro com o digest exibido na release autenticada.
5. Crie `C:\ApexTweaker\v2.3.0-portable`.
6. Extraia todo o ZIP nessa pasta. Nao execute diretamente de dentro do ZIP.
7. Confirme que `ApexTweaker.exe` e `ApexTweaker.Native.dll` estao juntos.
8. Feche Minecraft e o launcher antes de auditar ou aplicar configuracoes.
9. Execute `ApexTweaker.exe`. O UAC e esperado porque o aplicativo completo
   tambem possui modulos de Windows e usa um manifesto global de administrador.
10. Nunca execute Prism Launcher, Modrinth App, Java ou Minecraft como
    administrador. O modulo Cobblemon nao precisa elevar o jogo.
11. Abra a aba **Cobblemon**.
12. Selecione `C:\Users\igor.silva\Downloads\mods\mods`.
13. Clique **Auditar**. Esta etapa e somente leitura.
14. Confirme no resumo: 88 mods, um ID duplicado, zero dependencias ausentes e
    dois conflitos possiveis.
15. Abra `C:\ProgramData\ApexTweaker\MinecraftReports` pelo botao de relatorios.
16. Leia primeiro `cobblemon-audit-*.md` e depois
    `minecraft-quarantine-dry-run-*.md`.
17. No JSON, use `sha256`, `environment`, `dependencies`, `sideAssessment`,
    `serverEntryImpact` e `operationalRecommendation` como evidencia.
18. Nao clique **Mover selecionados** enquanto o manifesto exato do servidor
    nao estiver disponivel.

## Criar a instancia real no Prism Launcher

O Prism e preferivel porque mantem cada instancia isolada e permite configurar
Java e memoria por instancia.

1. Instale o Prism Launcher de uma fonte oficial.
2. Abra **Add Instance**.
3. Crie uma instancia chamada `Cobblemon-1.21.1-EXTREME-4GB`.
4. Selecione Minecraft `1.21.1`.
5. Selecione Fabric para `1.21.1`. Nao misture Forge ou NeoForge.
6. Abra **Settings > Java** da instancia.
7. Selecione Java `21` de 64 bits e use o teste de Java do Prism.
8. Comece com memoria minima `512 MB` e maxima `2048 MB`.
9. Inicie a instancia uma vez sem mods, espere o menu e feche o jogo. Isso cria
   `options.txt` e a estrutura `.minecraft`.
10. Copie, nao mova, os mods confirmados para a subpasta `.minecraft\mods`.
11. Comece com o mesmo conjunto exigido pelo servidor. Nao use a quarentena no
    primeiro baseline.
12. Inicie uma vez com o pacote e feche. Isso permite que os mods criem configs.
13. No ApexTweaker, selecione qualquer um destes caminhos:

```text
%APPDATA%\PrismLauncher\instances\Cobblemon-1.21.1-EXTREME-4GB
%APPDATA%\PrismLauncher\instances\Cobblemon-1.21.1-EXTREME-4GB\.minecraft
%APPDATA%\PrismLauncher\instances\Cobblemon-1.21.1-EXTREME-4GB\.minecraft\mods
```

14. A tela deve informar **Instancia valida detectada**.
15. Se continuar bloqueada, confirme que existem simultaneamente
    `.minecraft\options.txt` e `.minecraft\mods`.

## Alternativa Modrinth App

Crie um perfil Minecraft 1.21.1 + Fabric, selecione Java 21 x64, abra uma vez e
feche. O ApexTweaker procura perfis em:

```text
%APPDATA%\com.modrinth.theseus\profiles
%LOCALAPPDATA%\ModrinthApp\profiles
```

O perfil e detectado quando a raiz real contem `options.txt` e `mods`. A memoria
do Modrinth App deve ser ajustada manualmente no proprio app; o ApexTweaker gera
`apextweaker-java-args.txt` quando nao existe um contrato de escrita seguro.

## Aplicar EXTREME_4GB

1. Selecione a instancia real e clique **Auditar**.
2. Selecione `30 FPS` para o primeiro baseline.
3. Clique **Previsualizar (dry-run)**.
4. Abra o relatorio e confirme os caminhos, os valores antes/depois e o heap.
5. Confirme que nenhum caminho esta fora da instancia escolhida.
6. Clique **Aplicar plano com backup**.
7. Leia a confirmacao e aceite somente depois de conferir os arquivos.
8. Localize o backup em
   `C:\ProgramData\ApexTweaker\MinecraftBackups`.
9. Abra o jogo e confira as opcoes visuais antes do benchmark.

Valores aplicados em `options.txt`:

| Opcao | EXTREME_4GB |
|---|---:|
| Resolucao em janela | 1280x720 |
| Render distance | 4 |
| Simulation distance | 4 |
| Entity distance scaling | 0.50 |
| Particulas | Minimal |
| Clouds | Off |
| Graphics | Fast |
| Biome blend | 0 |
| Mipmap | 0 |
| Entity shadows | Off |
| VSync | Off |
| FPS | 30, 45 ou 60 conforme a rodada |

Se `config\iris.properties` existir, a chave reconhecida
`enableShaders=false` e aplicada com backup. Resource packs nao sao removidos
automaticamente porque podem ser exigidos pelo servidor; desative packs pesados
manualmente para o teste.

Para desfazer o perfil, feche o jogo e o launcher, selecione a mesma instancia
e clique **Restaurar ultimo perfil**. O rollback valida o manifesto e o SHA-256
antes de restaurar.

## Memoria Java para 4 GB

O ApexTweaker usa somente estes patamares no alvo de 4 GB:

| RAM fisica livre antes do jogo | Heap maximo |
|---|---:|
| Menos de 2.75 GB | `-Xms512M -Xmx2048M` |
| De 2.75 GB ate menos de 3.25 GB | `-Xms512M -Xmx2304M` |
| 3.25 GB ou mais | `-Xms512M -Xmx2560M` |

Comece por 2048 MB. Use 2304 MB somente se o jogo mostrar falta de heap e o
Windows ainda tiver reserva. Use 2560 MB apenas como teste agressivo. Se o
pagefile crescer continuamente, o sistema travar ou o FPS minimo piorar, volte
ao patamar anterior. Nunca use `-Xmx4G` no PC de 4 GB.

## Decisao sobre Mega Showdown

Arquivos encontrados:

```text
mega_showdown-fabric-1.7.3+1.7.3+1.21.1.jar
mega_showdown-fabric-1.8.4+1.7.3+1.21.1.jar
```

- Lado: `CLIENT_AND_SERVER_POSSIBLE` nos dois JARs.
- Motivo do alerta: ambos declaram o mesmo ID `mega_showdown`.
- Entrada no servidor: risco alto; o servidor pode exigir uma versao exata.
- Cobblemon: impacto alto; o addon adiciona mecanicas e depende de Cobblemon.
- RAM/FPS: a duplicidade precisa ser resolvida para o loader iniciar; ganho de
  FPS nao e o motivo da quarentena.
- Recomendacao: **investigar o manifesto e manter exatamente uma versao ativa**.
- Se o servidor confirmar `1.8.4`, teste a quarentena manual da `1.7.3`.
- Se o servidor confirmar `1.7.3`, nao mova a `1.7.3`; investigue por que a
  `1.8.4` foi adicionada.

O ApexTweaker exige confirmacao normal e uma segunda confirmacao de manifesto
para mover qualquer uma dessas versoes. Sem o manifesto, cancele.

## Decisao sobre Indium

Arquivo encontrado:

```text
indium-1.0.35+mc1.21.jar
```

- Lado: `CLIENT_ONLY` segundo o metadata do JAR.
- Motivo do alerta: ele exige Sodium `0.5.11`, enquanto o pacote usa Sodium
  `0.6.13`, que ja fornece o ID/API `indium`.
- Entrada no servidor: risco baixo; normalmente nao e requisito do servidor.
- Cobblemon: risco baixo no pacote atual, mas resource packs devem ser testados.
- RAM/FPS: pode adicionar carga redundante e incompatibilidade; o ganho ao
  retirar pode ser pequeno.
- Recomendacao: **testar sem e quarentenar somente se o cliente continuar
  estavel e entrar no servidor**.

O teste deve ser feito na copia da instancia, nao na pasta original de 88 mods.

## Adicionar ImmediatelyFast

Versao validada para este baseline:

```text
Projeto: ImmediatelyFast
Versao: 1.6.11+1.21.1-fabric
Loader: Fabric (tambem declara Quilt)
Minecraft: 1.21 e 1.21.1
Lado: client-side
Arquivo: ImmediatelyFast-Fabric-1.6.11+1.21.1.jar
```

Pagina da versao:
`https://modrinth.com/mod/immediatelyfast/version/1.6.11%2B1.21.1-fabric`

1. Baixe somente a versao acima de uma fonte oficial.
2. Feche o jogo e o launcher.
3. Copie o JAR para `.minecraft\mods` da instancia de teste.
4. Nao altere a pasta original de 88 mods nesta etapa.
5. Inicie o jogo e confira `logs\latest.log` para confirmar carregamento sem erro.
6. Valide menu, inventario, mapas, HUD, tela de batalha e entrada no servidor.
7. Repita o benchmark no mesmo local e com o mesmo heap/FPS.
8. Se houver crash, remova somente esse JAR e compare o novo `latest.log`.
9. Se houver artefato de HUD sem crash, teste `hud_batching=false` na config do
   mod. O ApexTweaker preserva essa chave e nao a reativa automaticamente.

## Benchmark operacional

Use sempre a mesma resolucao, servidor, local, rota e duracao.

1. Reinicie o PC ou feche navegador, Discord e outros processos pesados.
2. Confirme que o pagefile esta ativo:

```powershell
Get-CimInstance Win32_PageFileUsage | Select-Object Name,AllocatedBaseSize,CurrentUsage,PeakUsage
```

3. Registre a hora e a RAM livre antes de abrir o launcher.
4. Cronometre do clique em **Launch** ate o menu.
5. Cronometre do menu ate entrar no mundo ou servidor.
6. Depois de entrar, fique no mesmo local e clique **Benchmark 60 s**.
7. O ApexTweaker registra processo Java, RAM, CPU, pagefile, mods, configs,
   `latest.log`, crash reports e evidencia de `OutOfMemoryError`.
8. Registre FPS medio e minimo manualmente por PresentMon, Spark ou amostragem
   do F3. Se nao medir, deixe os campos vazios; o relatorio mostrara
   `NAO MEDIDO`.
9. Marque na tela se houve jogo aberto, menu, mundo, servidor, 720p, quedas,
   crash ou falta de memoria.
10. Clique **Salvar resultado real**.
11. Repita cada cenario duas vezes antes de promove-lo.

Matriz recomendada:

| Rodada | Mods | Xmx | FPS | Objetivo |
|---|---|---:|---:|---|
| A | Pacote confirmado, sem quarentena, sem ImmediatelyFast | 2048M | 30 | Baseline |
| B | Rodada A + ImmediatelyFast | 2048M | 30 | Isolar ganho/risco do mod |
| C | Melhor pacote da rodada B | 2304M, se permitido | 45 | Testar heap e fluidez |
| D | Melhor pacote | 2560M, se permitido | 45 | Teste agressivo de memoria |
| E | Melhor pacote | Melhor Xmx | 60 | Apenas se 45 FPS estiver estavel |
| F | Melhor pacote sem Indium | Melhor Xmx/FPS | Igual anterior | Validar redundancia |
| G | Uma unica versao Mega confirmada | Melhor Xmx/FPS | Igual anterior | Validar servidor |

Status final:

- `Approved`: abriu, chegou ao menu, entrou no mundo/servidor, sem crash/OOM,
  jogavel em 720p, FPS medio >= 30, minimo >= 15 e sem quedas severas.
- `Unstable`: iniciou, mas FPS, minimo, 720p ou stutter ficaram abaixo do alvo.
- `Failed`: nao abriu, nao entrou no alvo ou apresentou crash/OOM.
- `NotTested`: nenhuma evidencia operacional foi registrada.

## Comandos CLI equivalentes

Estes comandos sao para desenvolvimento e exigem .NET 10 instalado. No ZIP
portatil, prefira a interface grafica self-contained.

Auditoria real e checklist, ambos somente leitura:

```powershell
dotnet ApexTweaker.dll --minecraft-audit `
  --mods "C:\Users\igor.silva\Downloads\mods\mods" `
  --output ".\artifacts\operational"

dotnet ApexTweaker.dll --minecraft-operational-checklist `
  --mods "C:\Users\igor.silva\Downloads\mods\mods" `
  --instance "C:\caminho\da\instancia" `
  --fps 30 --output ".\artifacts\operational"
```

Dry-run, apply e rollback do perfil:

```powershell
dotnet ApexTweaker.dll --minecraft-profile-dry-run `
  --instance "C:\caminho\da\instancia" `
  --profile EXTREME_4GB --fps 30

dotnet ApexTweaker.dll --minecraft-apply-profile `
  --instance "C:\caminho\da\instancia" `
  --profile EXTREME_4GB --fps 30 --yes

dotnet ApexTweaker.dll --minecraft-rollback `
  --instance "C:\caminho\da\instancia" --yes
```

A quarentena de mod com risco de servidor exige as duas confirmacoes:

```powershell
dotnet ApexTweaker.dll --minecraft-quarantine-apply `
  --mods "C:\caminho\da\instancia\.minecraft\mods" `
  --files "mega_showdown-fabric-1.7.3+1.7.3+1.21.1.jar" `
  --yes --server-manifest-confirmed
```

Nao execute esse comando sem confirmar a versao do servidor. Para reverter:

```powershell
dotnet ApexTweaker.dll --minecraft-quarantine-rollback `
  --mods "C:\caminho\da\instancia\.minecraft\mods" --yes
```

Benchmark e registro da observacao:

```powershell
dotnet ApexTweaker.dll --minecraft-benchmark `
  --instance "C:\caminho\da\instancia" `
  --seconds 60 --wait-seconds 30

dotnet ApexTweaker.dll --minecraft-homologation-report `
  --instance "C:\caminho\da\instancia" `
  --game-opened --menu-reached --menu-seconds 45 `
  --server-entered --join-seconds 70 --playable-720p `
  --average-fps 32 --minimum-fps 18 `
  --notes "Rodada A, 1280x720, mesmo local do servidor"
```

## Regras de seguranca

- Nunca excluir JARs.
- Nunca mover JAR sem selecao e confirmacao explicitas.
- Sempre copiar e validar SHA-256 antes de mover.
- Sempre gerar relatorio antes/depois.
- Sempre manter backup e rollback separados para perfil e quarentena.
- Nunca alterar um hash sem registrar a operacao no manifesto.
- Nunca desativar Defender ou Windows Update permanentemente.
- Nunca desativar o pagefile.
- Nunca aplicar tweak de Registro, servico ou BCD para esta homologacao.
- Nunca executar o launcher ou Minecraft como administrador.
- Nunca mudar dois mods ou dois parametros na mesma rodada A/B.

## Riscos restantes e trabalho manual

1. Obter o manifesto exato do servidor, especialmente a versao de Mega Showdown.
2. Criar a instancia real no PC i3/4 GB.
3. Instalar e validar Java 21 x64 no launcher.
4. Copiar o pacote confirmado para a instancia, preservando a origem.
5. Baixar ImmediatelyFast 1.6.11 da fonte oficial.
6. Desativar shaders e resource packs pesados que nao sejam obrigatorios.
7. Executar as rodadas A-G e anexar os relatorios.
8. Confirmar entrada no servidor depois de cada quarentena.
9. Se houver HD, migrar para SSD. Upgrade para 8 GB, idealmente 2x4 GB, continua
   sendo a recomendacao de maior impacto.

O projeto esta pronto para conduzir o teste, mas nao pode afirmar que o PC alvo
esta homologado enquanto essas etapas manuais nao forem executadas nele.
