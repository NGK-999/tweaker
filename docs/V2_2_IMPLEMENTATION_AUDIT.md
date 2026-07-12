# Autoauditoria v2.1.0 -> v2.2.0

Data da revisao: 2026-07-12.

## Conclusao honesta sobre a v2.1.0

A v2.1.0 nao era somente uma tela ou documentacao. Scanner, relatorios,
`options.txt`, backup e rollback limitado eram funcionais. Ainda assim, a
critica era correta: a entrega era majoritariamente diagnostica e nao fechava o
ciclo operacional solicitado.

| Questao | Estado real na v2.1.0 |
|---|---|
| Scanner de JARs | Funcional e somente leitura |
| Relatorios JSON/Markdown/TXT | Funcionais |
| `EXTREME_4GB` | Alterava `options.txt`, mas era parcial |
| Config do Sodium e outros mods | Nao alterava |
| Argumentos Java | Apenas arquivo TXT de instrucao |
| Memoria Prism/MultiMC | Nao alterava |
| Deteccao de instancia | Somente raiz direta ou pasta `mods` |
| Criacao de instancia | Nao implementada |
| Quarentena | Apenas plano JSON; nao movia JARs |
| Benchmark | Media CPU/RAM de um processo Java, sem logs ou crash reports |
| Estado `NAO_TESTADO` | Nao existia; ausencia de Java virava erro |
| FPS | Nao media, corretamente |
| Rollback | Restaurava somente `options.txt` e o TXT de JVM |
| Validacao de abertura do Cobblemon | Nao implementada |
| Comparacao antes/depois | Parcial, sem inventario completo de configs |

O release v2.1.0 nao foi rebaixado e reexecutado nesta revisao. A autoauditoria
acima compara o codigo-fonte do commit `b24168f` com a implementacao atual.

## O que a v2.2.0 implementa de verdade

| Fluxo | Evidencia implementada |
|---|---|
| `Dry-run` do perfil | Lista arquivo, chave, valor anterior e valor proposto sem escrever |
| `Apply` do perfil | Captura backup, grava atomicamente e registra SHA-256 antes/depois |
| `Rollback` do perfil | Valida lista permitida e restaura o hash original |
| `options.txt` | Aplica render/simulation 4, 720p, fast, particulas, mipmap, VSync e efeitos |
| Sodium 0.6.13 | Altera somente chaves existentes confirmadas no source oficial |
| ImmediatelyFast 1.6.11 | Ativa somente opcoes regulares existentes; nao toca experimentais/debug |
| EntityCulling 1.10.5 | Mantem culling ativo e debug desativado em chaves existentes |
| Prism/MultiMC | Atualiza `OverrideMemory`, `MinMemAlloc` e `MaxMemAlloc` em `instance.cfg` |
| Outros launchers | Gera instrucao JVM; nao inventa formato privado |
| Quarentena | Exige selecao, copia, confere SHA-256, move e grava manifesto |
| Rollback da quarentena | Move de volta ou usa a copia, sem sobrescrever arquivo divergente |
| Benchmark | Ambiente antes/depois, processo Minecraft, mods, hashes, logs e crash reports |
| `NAO_TESTADO` | Resultado explicito quando nenhum processo Minecraft e detectado |
| Plano de Sobrevivencia | Veredito, JVM, mods, graficos, riscos e acoes manuais |
| CLI | Comandos separados para audit, dry-run, apply, rollback e benchmark |

## Limites mantidos de proposito

- O ApexTweaker nao mede FPS por injecao ou hook.
- O ApexTweaker nao cria automaticamente uma nova instancia Fabric.
- O ApexTweaker nao altera configuracoes desconhecidas ou arquivos ausentes.
- Sodium Extra, More Culling, Dynamic FPS, ModernFix e Noisium permanecem sem
  escrita automatica enquanto nao houver um contrato versionado validado.
- O launcher oficial, Modrinth e CurseForge recebem instrucao manual de JVM.
- O aplicativo nao conhece o manifesto privado do servidor. Confirmacao humana
  continua obrigatoria para JARs possivelmente exigidos.
- Defender, Windows Update, servicos criticos, registro e pagefile nao sao
  alterados por este modulo.

## Evidencias executadas

- Builds Debug e Release: zero avisos e zero erros.
- Self-test Release: 10 verificacoes aprovadas, cobrindo scanner, relatorios,
  dry-run, tres configs JSON, Prism, backup/rollback, manifesto legado,
  quarentena, benchmark, plano e XAML.
- Pasta real `C:\Users\igor.silva\Downloads\mods\mods`: 88 JARs auditados.
- Hashes SHA-256 antes/depois do dry-run real: 88 de 88 inalterados.
- Arquivos movidos no teste real: zero.
- Testes destrutivos foram executados somente em diretorio temporario.
