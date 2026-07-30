# Gates de revisão — rodada BE-DEMO-OUTCOME-P0 + FE-FEEDBACK-SHELL-P0

**Data:** 2026-07-25  
**Status:** critérios aprovados pelo produto; agentes em curso (não interromper BE produtivo)

## Veredito de execução

Isolamento OK: worktrees, branches, ownership, contratos congelados, integração centralizada, P0 pequenos, fallbacks. Continuar.

## Quatro pontos críticos

### 1. OperationOutcome com Models/Contracts congelados

Aceitável nesta rodada:

- tipo **interno** restrito ao pipeline;
- estrutura provisória claramente marcada;
- **proposta** de contrato no `backend-handoff.md`.

Não aceitar como arquitetura final: `OperationOutcome` público em `Services` só para contornar freeze.

Handoff BE deve conter:

```text
Tipo interno criado:
Localização:
Consumidores:
Pretende virar contrato compartilhado:
Mudança proposta:
```

### 2. Gate CommandRunner ≠ única barreira

Além do runner, examinar bypasses: Registry APIs, arquivos, Process.Start, powercfg/bcdedit/reg/sc/dism/gpupdate/LGPO.

Evidência mínima no handoff / revisão:

```powershell
rg -n "Process\.Start|Registry\.|SetValue|DeleteValue|powercfg|bcdedit|reg\.exe|sc\.exe|dism|gpupdate|LGPO" .
```

### 3. CatalogFeedbackSelfTest sem harness

Handoff FE deve marcar cada teste:

```text
COMPILADO
EXECUTADO
NÃO EXECUTADO — AGUARDA HARNESS
```

Não aceitar “self-test passou” se só compilou. Orquestrador na integração: `--catalog-feedback-self-test` em commit próprio.

### 4. Captura de agentes

Registrar por processo na conclusão:

```text
PID principal / PIDs filhos
Código de saída
Início / término
stdout / stderr paths
Branch / HEAD final / git status
Workers encerrados? (launcher ≠ workers)
```

Nota operacional: `codex exec` neste ambiente emite transcript útil em **stderr**; stdout pode ficar vazio. Claude deve ter stdout+stderr não vazios; se zerados, relançar FE.

## Gate aceitar BACKEND

- RuntimeMode ausente/inválido bloqueia mutação (fail-closed)
- Demo: leitura, inventário, análise, simulação OK
- Tentativa de mutação → outcome explícito
- Zero mutação real nos testes
- Cancel ≠ erro genérico; timeout classificado à parte
- correlationId nos caminhos testados
- Legado examinado não contorna gate
- `--gaming-fps-probe-self-test` PASS
- Sem arquivos congelados / fora de escopo

Além dos self-tests:

```powershell
git status --short
git diff --stat
git diff --name-only
git diff --check
git log -1 --oneline
```

## Gate aceitar FRONTEND

- `SnackbarKind.Error` explícito
- Sem Classify por Contains/includes/regex/substring
- Empty / Partial / Error distintos
- CTA = navegação para Auto (não apply iniciado)
- Sem contrato OperationOutcome compartilhado
- Foco / AutomationProperties considerados
- MainWindow só pontos autorizados; sem redesign amplo
- Build PASS
- Self-test: EXECUTADO ou NÃO EXECUTADO — AGUARDA HARNESS (honesto)

Busca anti-heurística:

```powershell
rg -n -i 'contains\(.*erro|includes\(.*erro|contains\(.*error|includes\(.*error|IndexOf\(.*erro|IndexOf\(.*error' .
```

## Revisão cruzada

1. Orquestrador: ownership + diffs + saídas reais  
2. Claude: só semântica pública/mensagens do outcome BE  
3. Codex: só fluxo de estado/cancel/integração potencial do FE  
4. Nenhum executor corrige a branch do outro  
5. Correções → dono ou commit de integração do orquestrador  

## Ordem de integração

```text
1. Backend demo gate + outcome interno
2. Verificação completa BE
3. Frontend feedback shell
4. Harness CatalogFeedbackSelfTest
5. Testes de integração
6. Consolidação documental
```

Revisão sempre sobre **diffs e saídas reais**, não só resumos.
