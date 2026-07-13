# Minecraft geral e hooks de sessao - v3.3.0

## Objetivo

A pagina principal deixou de exigir Cobblemon. Ela agora aceita Minecraft
vanilla ou modded depois que a instancia foi aberta uma vez e possui
`options.txt`. A pasta `mods` e opcional.

Cobblemon continua suportado como perfil de conteudo detectado. Quando ele
existe, as verificacoes especificas de Fabric API, Mega Showdown e dependencias
do servidor continuam disponiveis. Essas regras nao sao aplicadas a uma
instancia vanilla, Forge ou NeoForge.

Fluxo visivel:

`Detectar -> Analisar -> Otimizar -> Validar Multiplayer -> Testar -> Corrigir ou Restaurar`

## Perfis

- `EXTREME_4GB`: primeiro teste conservador em 1280x720, 30 FPS e Xmx2048M.
- `POTATO_4GB`: 960x540, 24 FPS, render 2, simulation 5 e Xmx2048M.
- `POTATO_4GB_480P`: 854x480 com os mesmos limites conservadores.
- aliases `POTATO_COBBLEMON_4GB` e `POTATO_COBBLEMON_4GB_480P`: preservados
  somente para ler scripts e manifestos antigos.

Os aliases legados nao aparecem na lista principal para evitar duplicidade.
Backups antigos continuam desserializando pelo nome original.

## Destino do teste

O modo facil separa `Mundo local` de `Servidor`. Nao entrar em um servidor nao
e falha quando o destino selecionado e mundo local. A homologacao registra o
que foi realmente informado pelo usuario; FPS continua indisponivel quando nao
foi medido externamente.

## Hooks de sessao

Os hooks sao temporarios, opt-in e aplicados apenas ao processo Java que possui
o caminho da instancia selecionada em sua linha de comando. Se WMI nao puder
confirmar esse processo, o ApexTweaker falha fechado e nao escolhe outro Java.
Boost, QoS e afinidade tambem falham fechado: se o estado anterior nao puder
ser capturado para rollback exato, aquela acao e ignorada.

### Desativado

- nenhuma alteracao de processo;
- benchmark e coleta de logs continuam funcionando.

### Seguro (padrao)

- prioridade `AboveNormal`;
- boost dinamico do scheduler permitido;
- HighQoS por `SetProcessInformation`;
- EcoQoS de velocidade desativado para a sessao;
- pedidos de resolucao de timer do processo continuam sendo honrados quando a
  versao do Windows oferece esse controle.

### Extremo

Inclui o modo Seguro e adiciona:

- prioridade `High`, nunca `RealTime`;
- afinidade para P-cores somente quando a DLL nativa confirma CPU hibrida e um
  unico grupo de processadores;
- Power Mode `Best Performance` no Windows 11, apenas quando o estado original
  AC/DC pode ser lido para rollback exato.

Em um i3 de quarta geracao nao existem P-cores/E-cores. A afinidade hibrida e
corretamente ignorada nesse hardware.

## Rollback e recuperacao

Antes do apply, o snapshot original e gravado em:

`%LOCALAPPDATA%\ApexTweaker\MinecraftSessionHooks\active-*.json`

Ao terminar ou cancelar o benchmark:

- prioridade, boost, afinidade, QoS e Power Mode sao restaurados;
- cada resultado e registrado em JSON;
- o diario ativo e removido somente depois de rollback confirmado;
- `Restore` e idempotente e serializado contra chamadas simultaneas.

Se o aplicativo for encerrado durante o teste, a proxima abertura tenta
restaurar o diario pendente. Em caso de falha, o diario permanece para uma nova
tentativa. O ultimo relatorio de hooks tambem entra no ZIP de diagnostico.

## O que nao foi implementado

- driver ou hook de kernel;
- injecao no Minecraft ou na JVM;
- prioridade `RealTime`;
- timer global permanente;
- alteracoes de BCD;
- desativacao de Defender, Windows Update, servicos ou pagefile;
- alteracoes permanentes de Registro para prometer FPS ou latencia.

O valor `SvcHostSplitThresholdInKB` mostrado em videos de tweak nao foi
adicionado. Esse valor controla agrupamento/isolamento de servicos conforme a
memoria do computador; nao e um controle documentado de latencia. Forcar o
agrupamento pode economizar alguma memoria, mas reduz isolamento e
confiabilidade. Sem benchmark reproduzivel e rollback operacional, ele nao
pertence ao fluxo Minecraft.

## CLI

```powershell
dotnet ApexTweaker.dll --minecraft-benchmark `
  --instance "C:\PrismLauncher\instances\Minha Instancia\.minecraft" `
  --seconds 60 `
  --wait-seconds 30 `
  --hooks safe
```

Modos aceitos: `off`, `safe` e `extreme`.

Para recuperar manualmente um diario pendente:

```powershell
dotnet ApexTweaker.dll --minecraft-recover-session-hooks
```

## Limites reais

Hooks de scheduler nao criam RAM, nao tornam uma Intel HD moderna e nao
compensam um HDD lento. No alvo de 4 GB, SSD, pagefile ativo e upgrade para 8
GB em dual-channel continuam tendo impacto maior. O ganho deve ser comprovado
no mesmo mundo/servidor, trajeto, resolucao e duracao.
