# ApexTweaker v3.0.0

## Minecraft Scientific Optimization Engine

Esta versao transforma o modulo Cobblemon em um motor de experimentos
persistentes. O fluxo recomendado passa a ser diagnosticar, medir baseline,
aplicar um candidato com backup, repetir a cena, comparar e manter ou reverter.

## Principais mudancas

- maquina de estados cientifica com JSON atomico;
- hipotese, metricas esperadas e risco registrados antes do apply;
- evidencia da instancia com SHA-256 de configs e mods;
- diagnostico de RAM, CPU, GPU, disco, heap, pagefile, configs e conflitos;
- perfis `GPU_LIMITED`, `RAM_LIMITED`, `CPU_LIMITED` e
  `SERVER_ENTRY_COMPATIBLE`;
- telemetria de bytes lidos/escritos pelo processo Java;
- resultados detalhados de benchmark e falha;
- comparacao ponderada com regressao critica;
- decisoes `KEEP`, `REVERT`, `RETEST` e `INSUFFICIENT_DATA`;
- rollback pelo backup exato do experimento;
- bloqueio de drift pre-apply e deteccao de alteracao fora da hipotese;
- politica conservadora: somente `KEEP` permanece aplicado;
- contratos documentados de configuracao de mods;
- classificacao multitag de mods;
- CLI completa para criar, medir, aplicar, comparar, listar e finalizar;
- novo cartao `Minecraft Scientific Optimization Engine` na GUI;
- self-test com ciclos completos de melhoria e regressao.

## Seguranca

- baseline obrigatorio antes de qualquer apply;
- nenhuma movimentacao automatica de mods;
- nenhuma chave de config inventada;
- confirmacao explicita para apply e rollback;
- conjunto de mods deve permanecer inalterado no experimento;
- Defender, Windows Update e pagefile nao sao alterados;
- FPS e GPU nao sao inventados quando nao medidos.

## Validacao

- build Release: 0 erros e 0 avisos;
- self-test: scanner, configs, backup, rollback, diagnostico, `KEEP`, `REVERT`
  drift, contaminacao e XAML aprovados;
- auditoria real da pasta de 88 mods permanece somente leitura;
- agregado SHA-256 dos 88 JARs preservado:
  `debf68ca0762e31af47c834648c361adbfc2f307763014749865484da895f935`;
- benchmark real do PC alvo permanece `NOT_TESTED` ate existir uma instancia
  completa e uma rodada executada pelo usuario.

## Artefatos

Hashes SHA-256 conferidos depois da compilacao final e do self-test do EXE:

| Arquivo | SHA-256 |
|---|---|
| `ApexTweaker.exe` | `a41f802e97180b34be6fc1eb6eb567b71252dd39bfbc8e77310c31588de66a81` |
| `ApexTweaker.Native.dll` | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |
| `ApexTweaker-Setup.exe` | `4d2df01787f697701e539a7570667ee7b5f8925b6346c841312454c60f0b61b2` |
| `ApexTweaker-Portable-v3.0.0.zip` | `40c6d824ae7b0826f4f9e02ab2de46c649b320186ac34fe01a042c8ab790c64c` |

## Uso

- [Motor cientifico e CLI](SCIENTIFIC_ENGINE.md)
- [Arquitetura v3](ARCHITECTURE_V3.md)
- [Cobblemon Low-End](COBBLEMON_LOW_END.md)
- [Homologacao operacional](HOMOLOGACAO_OPERACIONAL_COBBLEMON.md)
