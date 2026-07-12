# ApexTweaker v3.0.1

Patch de hardening da v3.0.0, sem mudanca de arquitetura.

## Correcoes

- manifesto alterado de `requireAdministrator` para `asInvoker`;
- UAC sob demanda apenas para mutacoes e rollback do Windows;
- Minecraft, benchmark, relatorios e backups executam em modo normal;
- dados Minecraft movidos para `%LOCALAPPDATA%\ApexTweaker`;
- migracao aditiva de relatorios/experimentos e fallback direto para backups
  legados de ProgramData, sem copiar manifestos com caminhos absolutos;
- `EXTREME_4GB` inicia em 30 FPS e `-Xmx2048M`;
- plano cientifico e congelado depois do baseline;
- apply rejeita plano divergente ou config alterada externamente;
- evidencias separam automatico, usuario, inferencia, recomendacao e ausente;
- relatorio declara FPS/GPU indisponiveis quando nao medidos;
- somente `KEEP` permanece aplicado;
- roteiro operacional dedicado ao primeiro teste de 4 GB;
- teste de release valida manifesto, versao e self-test sem bypass de UAC.

## Validacao

- build Release: `0` avisos e `0` erros;
- self-test do DLL e do EXE publicado: `SELF_TEST_OK`;
- teste do EXE: versao `3.0.1`, manifesto `asInvoker` e execucao sem bypass
  de UAC;
- auditoria real dos 88 JARs: somente leitura, 88 Fabric, uma duplicidade,
  dois conflitos possiveis e zero dependencias obrigatorias ausentes;
- agregado SHA-256 canonico `nome|hash` dos JARs preservado:
  `debf68ca0762e31af47c834648c361adbfc2f307763014749865484da895f935`;
- descoberta de instancias: nenhuma instancia completa encontrada nesta maquina;
- benchmark no PC i3/4 GB: `NOT_TESTED` ate a rodada fisica.

## Artefatos

| Arquivo | SHA-256 |
|---|---|
| `ApexTweaker.exe` | `0b3b605f27dec31dcfe97e7fb27b476099e82724e8e354e5ffb53e765415c5ba` |
| `ApexTweaker.Native.dll` | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |
| `ApexTweaker-Setup.exe` | `7deaef2cba9afcac49f444d88337391f6333cdc2fc4922616850562b282f60bb` |
| `ApexTweaker-Portable-v3.0.1.zip` | `832e4461d463500585c595e6b15c193c5b1d723b01b1a1a90663d0c262979869` |
