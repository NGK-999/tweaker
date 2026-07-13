# ApexTweaker v3.2.1 - Cobblemon Facil UX Polish

Patch pequeno de clareza e fluxo visual da aba Cobblemon Facil. A arquitetura,
o motor cientifico e os formatos de backup permanecem os mesmos da v3.2.0.

## Correcoes

- status inicial agora aguarda a deteccao da instancia;
- fluxo orientado por estados: detectar, analisar, otimizar, testar e corrigir;
- CTA principal renomeado para `Detectar Instancia Agora`;
- cards distinguem pronto, executando, concluido, atencao, falha e bloqueio;
- acesso duplicado ao Modo Avancado removido do rodape;
- rodape nao cobre os ultimos cards em 1180x780 e 980x680;
- scrollbar escura alinhada ao tema;
- botoes desativados explicam o requisito em tooltip;
- erros tecnicos conhecidos recebem textos humanos no modo facil;
- hashes, JSON, diffs, logs longos e decisoes internas continuam restritos ao
  modo avancado.

## Seguranca preservada

- manifesto do executavel: `asInvoker`;
- nenhuma operacao automatica move ou exclui JARs;
- alteracoes de configuracao continuam exigindo backup e oferecem rollback;
- o motor cientifico continua cobrindo `KEEP`, `REVERT`, `RETEST` e
  `INSUFFICIENT_DATA` sem expor esses nomes sem explicacao no modo facil.

## Validacao

- build Release: 0 avisos e 0 erros;
- self-test do projeto e do executavel publicado: `SELF_TEST_OK`;
- XAML carregado em thread STA;
- verificacao visual: 1180x780 e 980x680;
- auditoria NuGet: nenhum pacote vulneravel conhecido;
- teste de release: versao 3.2.1 e manifesto `asInvoker`.

## Assets e SHA-256

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `ApexTweaker-Setup.exe` | 74959516 | `0574a647bcc3e24aa7f72dac0976bc588d515843d1886a10d1204188bfffce87` |
| `ApexTweaker-Portable-v3.2.1.zip` | 74377101 | `3bb8dc420c0b7b24bcd83754fa11ac335b06d495f6ac072f8d37d8ebec5248db` |
| `ApexTweaker.exe` | 80483940 | `3e26602ea731d392d1856af828ab4923842251b2c0a13d02c740252e030db0bc` |
| `ApexTweaker.Native.dll` | 19456 | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |

Os executaveis nao possuem assinatura Authenticode. O ZIP nao e um arquivo
assinavel e deve ser validado pelo SHA-256.

## Risco restante

A experiencia final ainda precisa ser homologada no i3 de quarta geracao com
4 GB de RAM e no servidor real. Build e self-test nao medem FPS nem comprovam
compatibilidade de todos os mods exigidos pelo servidor.
