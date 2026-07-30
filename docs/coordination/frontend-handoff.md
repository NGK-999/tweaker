# Frontend handoff

Status: **AUDIT-PERF-FE concluido em 2026-07-25**

## Escopo executado

- Auditoria UI Desempenho (`PerformanceView` + wiring em `MainWindow`)
- Conferencia de refresh do probe, snackbars e risco
- Remocao de refresh duplicado na navegacao

## Achados

### Baixa / polish

- `PerformanceButton_OnClick` chamava `RefreshPerformanceProbe()` logo apos `ShowPageAsync(PerformancePageKey, ...)`, mas `ShowPageAsync` ja refresca o probe ao abrir Desempenho.
- Fix: removido o refresh duplicado; a pagina continua atualizando uma unica vez na navegacao.

### Sem bug confirmado na UI

- Status da pagina Desempenho continua vindo de `CaptureGamingPerformanceProbe()` via `RefreshPerformanceProbe()` / `ApplyProbe()`.
- Acoes de risco (VBS/HVCI) seguem com confirmacao e badge de risco; Auto nao aplica `fps.vbs-hvci`.

## Arquivos alterados nesta auditoria

- `src/UI/Wpf/MainWindow.xaml.cs`

## Pendencias

- Nenhuma pendencia frontend aberta dentro do escopo `AUDIT-PERF-FE`.
