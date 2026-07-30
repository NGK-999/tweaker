# Estratégia de testes — ApexTweaker

**Data:** 2026-07-25  
**Fase:** 1

## Realidade atual

- Sem projeto xUnit/NUnit/MSTest.
- Evidência = `dotnet build` + self-tests CLI + scripts manuais.
- **Insuficiente** como único gate de qualidade.

## Pirâmide alvo

```text
        E2E (poucos, críticos)
       /                      \
  Integração / contratos FE↔BE
 /                              \
Unitários (regras, merge flags, taxonomia)
```

### Unitários

- merge AppCompat Layers / FSO;
- recommendation decisions (Blocked vs Recommended);
- `ErrorDescriptor` mapping;
- parse de CommandResult timedOut/cancelled.

### Integração

- `MutationExecutor` com filesystem temp + registry mocks;
- `BackupService` commit/restore em diretório isolado;
- `CommandRunner` com processo stub e timeout.

### Contratos

- fachada Analyze estável;
- envelope `OperationOutcome` consumível pela UI;
- breaking change = bloqueio do orquestrador.

### E2E (máquina dedicada ou VM)

- Disclaimer → Dashboard → Auto em **demo**;
- Catálogo Analyze → empty/partial;
- Rollback dry-run;
- Close durante telemetria (não hang).

### Falha / recuperação

- matar processo no meio do pipeline (ledger);
- negar admin;
- comando timeout;
- disco cheio simulado no path de backup.

### Plataforma

- Windows 10/11 builds relevantes;
- com e sem admin;
- notebook vs desktop (quando usage profile existir).

### A11y / visual

- AutomationProperties nas telas de fluxo;
- smoke de contraste (tokens);
- sem regressão de spinner infinito (busy flag).

## Self-tests CLI (manter e expandir)

| Flag | Papel |
|------|-------|
| `--gaming-fps-probe-self-test` | probe + FSO merge |
| `--market-coverage-self-test` | catálogo |
| futuro `--operation-envelope-self-test` | outcomes |
| futuro `--demo-self-test` | gate de mutação |

## Regra para agentes

Toda tarefa declara:

1. testes criados/atualizados;  
2. comandos de verificação;  
3. o que **não** foi testado.

**“Build passou” sozinho = rejeitado.**
