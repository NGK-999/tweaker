# Quality gates — ApexTweaker

**Data:** 2026-07-25  
**Fase:** 1

Uma alteração só integra no `main` quando **todos** os itens aplicáveis passam.

## Gate checklist (orquestrador)

| # | Gate | Evidência |
|---|------|-----------|
| 1 | Critérios de aceitação da task | handoff + diff |
| 2 | Diff revisado (não confiar no relato do agente) | orquestrador leu) |
| 3 | Build Release | `dotnet build ApexTweaker.sln -c Release` |
| 4 | Lint/analyzers do projeto (quando configurados) | saída do build |
| 5 | Testes relevantes | self-test / unit / script |
| 6 | Contratos compatíveis | sem breaking em Contracts/Models sem aprovação |
| 7 | Logs adequados | correlationId / outcome quando no escopo |
| 8 | Erro + recuperação definidos | taxonomy / UI states |
| 9 | Docs atualizados | coordination + architecture se afetar |
| 10 | Rollback existe se mutação | sessão / restore |
| 11 | Sem alteração fora do escopo | diff limpo |
| 12 | Ownership respeitada | FE/BE files separados |
| 13 | Zero mutação real em teste estrutural | demo gate / VM |

## Bloqueadores absolutos

- tocar arquivos do outro agente sem autorização;
- mutar máquina de desenvolvimento em teste estrutural;
- Apply Dangerous sem confirmação;
- silenciar falha de backup e seguir apply;
- expandir `MainWindow` com nova lógica de domínio (preferir fachada).

## Integração

1. Agent termina em worktree + handoff.  
2. Orquestrador copia só `Allowed files`.  
3. Roda verification commands.  
4. Atualiza `master-plan.md` + graphify se código mudou.  
5. Commit só com pedido explícito do usuário.
