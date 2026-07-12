# ApexTweaker v3.1.0

Frontend fluido e otimizacao extrema de Cobblemon, preservando o motor
cientifico, privilegio minimo, backup, hash e rollback da v3.0.1.

## Interface

- wizard MVVM em dez etapas;
- modo simples e avancado;
- timeline, progresso e estados visuais;
- benchmark cancelavel com grafico WPF leve;
- rollback persistente;
- layout minimo reduzido para 980x680;
- area tecnica recolhida por padrao.

## Minecraft

- perfil `POTATO_COBBLEMON_4GB`;
- simulation distance minima corrigida para 5;
- caps de 20 e 24 FPS;
- catalogo de experimentos isolados;
- heap 1792 MB somente como hipotese de pagefile;
- commit do Windows coletado por amostra;
- deteccao de `GC overhead limit exceeded`;
- cancelamento cientifico restaura o backup exato;
- ImmediatelyFast e EntityCulling preservados nos defaults.

## Dependencias

- adicionado `CommunityToolkit.Mvvm 8.4.2`;
- nenhum framework visual, Chromium ou biblioteca Skia adicionado.

## Validacao

- build Release: `0` avisos e `0` erros;
- self-test do DLL e do EXE publicado: `SELF_TEST_OK`;
- manifesto do EXE: `asInvoker`;
- smoke visual WPF: aprovado sem dispatcher exception;
- layout minimo 980x680: aprovado;
- working set self-contained: v3.0.1 `221,7 MB`; v3.1.0 `220,3 MB`;
- auditoria real: 88 JARs Fabric, uma duplicidade, dois conflitos possiveis e
  zero dependencias obrigatorias ausentes;
- agregado SHA-256 dos JARs preservado:
  `debf68ca0762e31af47c834648c361adbfc2f307763014749865484da895f935`;
- benchmark no i3/4 GB: `NOT_TESTED`.

## Artefatos

| Arquivo | SHA-256 |
|---|---|
| `ApexTweaker.exe` | `8b127851db8759c6cbff0266a924337aa358ad1fd565fca7435080b6185a2bc2` |
| `ApexTweaker.Native.dll` | `8e9d346f129efbbe8a28ac6d9081d086ac8130396af6b0b571dacf7a2a178b82` |
| `ApexTweaker-Setup.exe` | `4415d3880d8638391e593e955cc2c659a3b65628a259caa88eca24c10d2d8f8f` |
| `ApexTweaker-Portable-v3.1.0.zip` | `1bec127a6cfca08a9b542eab0c22ff2c26bccf22fbf86647454abee3207c73b7` |
