# Prompt de Execução para o Codex — ARCH-REFACTOR-P1

Você é o agente **Codex**, executor de backend do ApexTweaker.

## Instruções

Leia e execute as diretrizes especificadas em `docs/coordination/backend-task.md`.

### Objetivo Principal:
1. **Definir Contratos**: Criar interfaces limpas em `src/ApexTweaker.Contracts/` para os serviços centrais:
   - `ITweakService`
   - `IBackupService`
   - `IMutationExecutor`
   - `IOptimizationEngine`
2. **Implementar Interfaces**: Conectar essas interfaces às classes existentes em `src/Services/` sem quebrar compatibilidade com a UI atual.
3. **DEMO-INVENTORY-P1**: Em `SystemDiagnosticsService.cs`, garantir que em modo Demo a detecção de TPM/Secure Boot não invoque processos PowerShell.
4. **Validar**: Rodar o build e os self-tests.
5. **Handoff**: Atualizar `docs/coordination/backend-handoff.md`.

### Regras Críticas:
- NÃO edite arquivos em `src/UI/**`.
- NÃO execute mutações reais no Windows da máquina de desenvolvimento.
- Responda em português no relatório de handoff.
