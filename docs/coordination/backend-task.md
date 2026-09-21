# Backend task — Transição Arquitetural & Backlog P1

**Task ID:** ARCH-REFACTOR-P1  
**Origem:** Auditoria de Arquitetura Nemotron 120B + Backlog `master-plan.md`  
**Executor:** Codex (`gpt-5.6-luna` / `gpt-5.6-terra`)  
**Status:** READY FOR CODEX  
**Branch:** `import-comandos-hooks-tweaker`  
**Orquestrador:** Antigravity  

---

## 1. Contexto & Diagnóstico (Auditoria Nemotron)

A auditoria arquitetural identificou:
1. **Acoplamento UI → Infraestrutura**: `MainWindow.xaml.cs` (>2.100 linhas) instancia serviços diretamente sem abstrações nem injeção de dependência.
2. **Serviço Fachada Monolítico**: `TweakService.cs` (~1.310 linhas) concentra presets, validação e mutações como ponto único de falha.
3. **Duplicidade de Modelos**: Coexistência de `PresetKind` (legado) e `WindowsOptimizationPreset` (novo catálogo) sem adaptador.
4. **Pendências do Master Plan**:
   - `DEMO-INVENTORY-P1`: Coletar Secure Boot e TPM em modo Demo sem invocar PowerShell.
   - `UI-OUTCOME-P1`: Backend preparar o contrato e a propagação de `OperationOutcome` tipado.

---

## 2. Escopo Permitido para o Codex

| Área | Arquivos / Pastas Permitidas | Ação Esperada |
|---|---|---|
| **Contratos & Interfaces** | `src/ApexTweaker.Contracts/**` | Definir interfaces dos serviços de domínio: `ITweakService`, `IBackupService`, `IMutationExecutor`, `IOptimizationEngine`. |
| **Implementação de Serviços** | `src/Services/**`, `src/ApexTweaker.Application/**` | Fazer com que os serviços implementem as novas interfaces de contrato. |
| **Demo Inventory (P1)** | `src/Services/SystemDiagnosticsService.cs`, `src/ApexTweaker.Windows/**` | Substituir chamadas PowerShell por WMI/APIs C# tipadas para Secure Boot/TPM quando em `RuntimeMode.IsDemo`. |
| **Testes Unitários** | `tests/` ou self-tests em `src/App/` | Adicionar validações automatizadas cobrindo os novos contratos e o coletor de inventário. |

---

## 3. Fora de Escopo / Proibições Absolutas

- **NÃO alterar** arquivos em `src/UI/**` (pertence ao Claude Code / Frontend).
- **NÃO executar** comandos que mutem o Windows real (registro, BCD, desativação de serviços reais, GPO apply).
- **NÃO usar** `--dangerously-skip-permissions` ou bypass de sandbox.
- **NÃO apagar** código legado sem comprovação de que a interface substituta cobre 100% dos fluxos.

---

## 4. Comandos de Verificação Obrigatórios

```bash
dotnet build ApexTweaker.sln -c Release
dotnet run --project ApexTweaker.csproj -c Release --no-build --no-restore -- --demo-self-test
dotnet run --project ApexTweaker.csproj -c Release --no-build --no-restore -- --gaming-fps-probe-self-test
```

---

## 5. Critérios de Entrega (Handoff)

1. Código compilando sem erros em Release.
2. Auto-testes executando com status `ALL PASS`.
3. Preenchimento do relatório em `docs/coordination/backend-handoff.md`.
