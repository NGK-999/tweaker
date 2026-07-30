# Error messages — ApexTweaker (UX)

**Data:** 2026-07-25  
**Alinha a:** `docs/reliability/error-taxonomy.md`

Toda mensagem de erro visível deve permitir expandir:

1. Título compreensível  
2. Causa / limitação  
3. Impacto  
4. O que foi aplicado  
5. O que não foi aplicado  
6. Ação recomendada  
7. Detalhes técnicos  
8. correlationId  

## Exemplos (copy alvo)

| Situação | Título | Ação |
|----------|--------|------|
| Sem admin | Sem permissão de administrador | Executar como admin |
| Backup falhou | Não foi possível criar backup | Nenhuma alteração feita — liberar disco e tentar |
| Timeout comando | O Windows demorou demais | Verificar antivirus; tentar de novo |
| Partial | Otimização parcial | Ver o que falhou / Reverter |
| VBS | Reinício necessário | Reiniciar antes de medir |

Proibido como mensagem única: “Erro inesperado.”
