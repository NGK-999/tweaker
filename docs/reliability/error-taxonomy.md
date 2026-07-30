# Taxonomia de erros — ApexTweaker

**Data:** 2026-07-25  
**Fase:** 1 (definição; implementação progressiva na Fase 3)

## Princípio

Nenhuma falha silenciosa. Nenhuma mensagem só “Erro inesperado” sem corpo.  
Toda falha de operação carrega `correlationId`.

## Categorias

| Código | Categoria | Exemplos | Recuperação típica |
|--------|-----------|----------|--------------------|
| `AUTHZ` | privilégio / UAC | HKLM negado, não-admin | elevar; ou pular com PARTIAL |
| `POLICY` | política SO / MDM | SecurityException, GPO | explicar bloqueio; não insistir |
| `TIMEOUT` | comando excedeu limite | powercfg/bcdedit travado | kill + CANCELLED/FAILED |
| `CANCEL` | cancelamento | token / usuário | CANCELLED; nada ou partial |
| `VERIFY` | pós-condição falhou | valor reg diferente | ROLLBACK_REQUIRED |
| `IO` | disco / path | backup falhou | **abortar mutação** |
| `STATE` | concorrência / busy | `isTweaking` | aguardar; não enfileirar cego |
| `COMPAT` | hardware/uso | ReBAR unknown, anti-cheat | checklist; sem mutação perigosa |
| `PIPE` | telemetria IPC/ETW | ETW hang | teardown timeout; app segue |
| `FATAL` | crash não recuperável | Dispatcher unhandled | MessageBox + export futuro |
| `UNKNOWN` | não classificado | Exception genérica | detalhes técnicos + id |

## Severidade de produto

| Nível | UI | Mutação |
|-------|----|---------|
| Info | snackbar Info | — |
| Warning | snackbar Warning | partial ok |
| Error | snackbar Error + painel | parar caminho |
| Critical | modal + bloquear apply | preservar SO |

**Gap atual:** `SnackbarKind` não tem `Error` (`src/UI/Wpf/Controls/Snackbar.cs`).

## Contrato `ErrorDescriptor` (alvo)

```text
Title            — linguagem humana
Category         — código acima
Cause            — causa conhecida ou limitação
Impact           — o que o usuário sente
Applied[]        — o que já mudou
NotApplied[]     — o que não mudou
RecommendedAction
TechnicalDetails — exception / comando
CorrelationId
Outcome          — enum oficial
```

## Mapeamento de exceções .NET (inicial)

| Exception | Category |
|-----------|----------|
| `UnauthorizedAccessException` | AUTHZ |
| `SecurityException` | POLICY |
| `OperationCanceledException` | CANCEL / TIMEOUT |
| `TimeoutException` / CommandResult.TimedOut | TIMEOUT |
| `IOException` / `UnauthorizedAccess` em backup | IO |
| `InvalidOperationException` (sem sessão) | STATE |
| demais | UNKNOWN |

## Anti-padrões proibidos

- engolir `catch { }` sem log em caminhos de mutação (teardown de close é exceção documentada);
- retornar só `IReadOnlyList<string>` sem outcome tipado em APIs novas;
- classificar erro só por substring de mensagem na UI (hoje: `ClassifySnackbarKind`).
