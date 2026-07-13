# ApexTweaker v3.2.0 - Cobblemon One-Click Mode

A v3.2.0 adiciona uma camada simples sobre o motor científico existente. O fluxo padrão agora é `Detectar -> Analisar -> Otimizar -> Preparar servidor -> Testar -> Corrigir/Restaurar`, sem remover o modo avançado.

## Cobblemon Fácil

- Detecta instâncias Prism Launcher, MultiMC, Modrinth App e pastas selecionadas manualmente.
- Resume mods essenciais, mods de performance, mods visuais pesados, duplicatas e riscos sem abrir uma tabela técnica por padrão.
- Aplica o perfil `POTATO_COBBLEMON_4GB` com backup, hashes e relatório antes/depois.
- Oferece a opção ainda mais extrema `POTATO_COBBLEMON_4GB_480P`.
- Prepara uma verificação de compatibilidade com servidor sem mover ou excluir JARs.
- Registra respostas simples do teste real e mantém FPS vazio quando ele não foi informado.
- Gera um plano de correção conservador para falta de memória, stutter, crash e recusa do servidor.
- Exporta um ZIP de diagnóstico com relatórios, hashes, configurações, logs e crash reports disponíveis.
- Mantém `Restaurar Tudo` visível e preserva o laboratório científico completo em `Modo Avançado`.

## Perfil padrão para 4 GB

- Resolução: `960x540`, com opção `854x480`.
- Render distance: `2`.
- Simulation distance: menor valor válido aceito pelo Minecraft atual.
- Entity distance: `0.30`.
- Limite de FPS: `24`, com opção `30`.
- VSync, nuvens, sombras de entidades, view bobbing e iluminação suave: desativados.
- Partículas: mínimas.
- Mipmap e biome blend: `0`.
- Shaders e resource packs pesados: desativados quando a configuração gerenciada permite.
- JVM: `-Xms512M -Xmx2048M` no primeiro teste. O ApexTweaker nunca configura `-Xmx4G`.

## Segurança

- O aplicativo executa com manifesto `asInvoker`, sem UAC global.
- Nenhum botão do modo fácil move, exclui ou coloca mods em quarentena.
- Toda escrita de configuração usa o mecanismo existente de backup e rollback.
- O pacote de diagnóstico lê evidências da instância, mas limita arquivos grandes e não altera a origem.
- FPS só aparece como medição manual quando o usuário o informa.
- Correções potencialmente incompatíveis com o servidor são recomendações, não alterações automáticas.

## Validação executada

- Build Release: `0` avisos e `0` erros.
- Self-test do projeto: `SELF_TEST_OK`.
- Self-test do executável publicado: aprovado.
- Manifesto do executável: `asInvoker` confirmado com `mt.exe`.
- ZIP portátil: extraído e executado com sucesso.
- EXE e DLL dentro do ZIP: hashes idênticos aos binários publicados.
- Smoke test de interface: aprovado em `1180x780` e `980x680`.
- Navegação: `Cobblemon Fácil -> Modo Avançado -> Cobblemon Fácil` aprovada.
- Botão `Corrigir Problemas`: oculto antes do primeiro teste, conforme o fluxo esperado.
- Testes automatizados: detecção, resumo de mods, preparação de servidor, perfil 480p, correção de OOM, diagnóstico ZIP, preservação de JARs e rollback exato.
- Auditoria NuGet: nenhuma dependência vulnerável conhecida encontrada.

## Assets e SHA-256

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `ApexTweaker-Setup.exe` | 74.945.161 | `e565f685d86236011ebe95a135bd6625bb5e28d6f35f10e0b8164576deda7e26` |
| `ApexTweaker-Portable-v3.2.0.zip` | 74.363.848 | `8bd211022dabfed7705d1d15a72a49087167c8e6ec625634dd38ed95bf021609` |
| `ApexTweaker.exe` | 80.473.628 | `4ad7569da50affef63e314a9a930c1ca4a2193461bee2d0ee66f984ed4920979` |
| `ApexTweaker.Native.dll` | 19.456 | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |

## Primeiro teste no i3 com 4 GB

1. Extraia `ApexTweaker-Portable-v3.2.0.zip` para uma pasta do usuário.
2. Execute `ApexTweaker.exe` sem administrador.
3. Abra `Cobblemon Fácil` e use `Detectar Instância`.
4. Confirme a instância Minecraft 1.21.1 Fabric que já foi aberta ao menos uma vez.
5. Use `Analisar Mods` e confirme que Cobblemon e os mods obrigatórios do servidor aparecem.
6. Use `Preparar para Servidor` e informe corretamente se o servidor exige Mega Showdown.
7. Use `Otimizar para PC Fraco`, inicialmente em `960x540`, `24 FPS` e `Xmx2048M`.
8. Use `Testar Jogo`, abra o Minecraft, entre no menu e tente acessar o servidor.
9. Informe somente o FPS que foi realmente observado e registre crash, stutter ou erro de mod.
10. Use `Corrigir Problemas` para obter o próximo teste seguro ou `Restaurar Tudo` para voltar ao backup.
11. Use `Exportar Diagnóstico` para gerar o ZIP caso ainda exista falha.

## Riscos restantes

- A execução física no i3 de 4ª geração, Intel HD e 4 GB ainda é obrigatória; fixtures não reproduzem pressão real de memória, pagefile, geração de chunks ou GPU integrada.
- A pasta `C:\Users\igor.silva\Downloads\mods\mods` não estava disponível na validação final da v3.2.0, portanto a auditoria real dos 88 JARs não foi repetida nesta etapa.
- Compatibilidade com o servidor depende da lista e das versões exigidas pelo servidor; o ApexTweaker não pode inferir esse contrato com certeza.
- Prism/MultiMC permitem aplicar heap automaticamente. Outros launchers podem exigir a alteração manual indicada pela interface.
- FPS permanece uma observação manual; o ApexTweaker não injeta contador no Minecraft.
- Os binários ainda não possuem assinatura Authenticode. Windows SmartScreen pode exibir aviso até existir assinatura de código confiável.
- Em 4 GB, SSD, pagefile ativo e upgrade para 8 GB em dual-channel continuam sendo as melhorias de hardware mais importantes.

## Documentação

Consulte `docs/V3_2_COBBLEMON_ONE_CLICK.md` para o comportamento de cada botão, limites de automação e recuperação.
