# ApexTweaker v2.3.0

## Homologacao operacional Cobblemon

- Seletor de 30, 45 ou 60 FPS no perfil e na interface.
- Heap de 4 GB restrito aos patamares 2048, 2304 e 2560 MB conforme RAM livre.
- Checklist operacional exportado em JSON, Markdown e TXT.
- Registro manual de abertura, menu, mundo, servidor, tempos, 720p, FPS, quedas,
  crash e falta de memoria.
- Resultado separado em `Approved`, `Unstable`, `Failed` ou `NotTested`.
- Benchmark automatico pode ser associado ao resultado manual sem fabricar FPS.
- Iris e desativado somente pela chave reconhecida `enableShaders=false`, com
  backup e rollback.
- `hud_batching` do ImmediatelyFast e preservado para evitar regressao visual.

## Quarentena

- Nenhum JAR e preselecionado ou movido automaticamente.
- Apply exige confirmacao explicita no backend, na CLI e na interface.
- Mods possivelmente exigidos pelo servidor exigem uma segunda confirmacao de
  que o manifesto foi comparado.
- O relatorio inclui lado, impacto no servidor, impacto no Cobblemon, impacto de
  performance e recomendacao operacional.
- Mega Showdown: investigar o manifesto e manter exatamente uma versao ativa.
- Indium: client-only; testar sem em copia porque Sodium 0.6.x ja fornece a API.

## Pacote real auditado

- Pasta: `C:\Users\igor.silva\Downloads\mods\mods`.
- 88 JARs Fabric.
- Um ID duplicado e dois conflitos possiveis.
- Zero dependencias obrigatorias ausentes.
- ImmediatelyFast ausente; recomendada a versao
  `1.6.11+1.21.1-fabric` para teste isolado.
- Impressao agregada dos nomes e SHA-256 antes/depois:
  `debf68ca0762e31af47c834648c361adbfc2f307763014749865484da895f935`.
- Zero JARs movidos e zero pastas de quarentena criadas.

## Validacao automatizada

- Build Release: zero avisos e zero erros.
- Scanner, relatorios e matriz de recomendacoes.
- Perfil dry-run/apply e rollback byte a byte em instancia Prism temporaria.
- FPS 30/45/60 e bloqueio de valores fora dessa lista.
- Xmx2048M/Xmx2304M/Xmx2560M pela RAM livre.
- Confirmacoes de quarentena e manifesto.
- Iris, Sodium, ImmediatelyFast, EntityCulling e `options.txt`.
- Checklist e quatro estados de homologacao.
- XAML da aba Cobblemon.

## Limite da entrega

O fluxo esta pronto para o PC real, mas o hardware i3/4 GB e a entrada no
servidor ainda nao foram executados nesta maquina de desenvolvimento. O estado
operacional permanece `NotTested` ate a rodada descrita em
`docs/HOMOLOGACAO_OPERACIONAL_COBBLEMON.md`.

O repositorio permanece privado. Releases e assets retornam `404` para usuarios
sem uma sessao GitHub autorizada.

## SHA-256 dos artefatos

| Asset | SHA-256 |
|---|---|
| `ApexTweaker-Portable-v2.3.0.zip` | `04129ecbd1ac8cb5a6865c811d07bd706d99f79c06b6b17e89c3e67dc78b50e1` |
| `ApexTweaker-Setup.exe` | `e87f88a89f6cb11773e872116218597ddcb7364b5bd3fdedcfda765a702a909d` |
| `ApexTweaker.exe` | `1789606c537c43e31086886c03a429963fd130311b73ce54f5de344fd288e363` |
| `ApexTweaker.Native.dll` | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |
