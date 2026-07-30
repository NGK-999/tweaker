# ROUTING DECISION — FE-FEEDBACK-SHELL-P0 (aprovado 2026-07-25)

```
Task ID: FE-FEEDBACK-SHELL-P0
Goal: Estados Empty/Partial/Error distintos no Catálogo; SnackbarKind.Error explícito (sem substring); CTA de navegação honesto para Auto; testes de estados/foco; sem redesign; sem consumir OperationOutcome BE.
Executor: Claude Code
Model: claude-sonnet-4-5 (Claude Code default do CLI local) — se indisponível, default do `claude`
Effort: medium
Skills: graphify (obrigatório), ponytail, impeccable (somente estados/copy, sem redesign)

Worktree: C:\projetos\Apextweaker-claude
Branch: agent/claude-fe-feedback-shell-p0

Allowed files:
  - src/UI/Wpf/Views/CatalogView.xaml
  - src/UI/Wpf/Views/CatalogView.xaml.cs
  - src/UI/Wpf/ViewModels/CatalogViewModel.cs
  - src/UI/Wpf/Controls/Snackbar.cs
  - src/UI/Wpf/Testing/CatalogFeedbackSelfTest.cs   (novo — asserts puros)
  - src/UI/Wpf/ViewModels/CatalogFeedbackState.cs     (novo — máquina de estado UI local)
  - src/UI/Wpf/MainWindow.xaml.cs  (SOMENTE: overload SetStatus com SnackbarKind; remover ClassifySnackbarKind; NÃO redesign)
  - docs/coordination/frontend-handoff.md
  - docs/coordination/frontend-task.md

Forbidden files:
  - src/Services/**, src/App/**, src/Infrastructure/**
  - src/Models/**, src/ApexTweaker.Contracts/**, src/ApexTweaker.Application/**
  - Program.cs (BE usa; CONGELADO para FE — propor wire do self-test no handoff)
  - Themes/MacTheme.xaml (sem renomear)
  - docs/coordination/backend-*, docs/product/**, docs/architecture/**

Contratos OperationOutcome BE: NÃO criar espelho definitivo no FE. Estado de catálogo = enum UI local.

Acceptance criteria:
  1. SnackbarKind.Error explícito; sem ClassifySnackbarKind por substring
  2. Empty, Partial, Error = estados diferentes (enum/UI)
  3. CTA descreve navegação (“Ir ao Dashboard para Auto-Optimize”), não apply imediato
  4. Sem contrato OperationOutcome no FE
  5. CatalogFeedbackSelfTest cobre: empty, falha, partial, Snackbar Error kind, foco/AutomationProperties no CTA
  6. Sem redesign amplo
  7. Build Release + handoff com diff, testes, pendências

Verification commands:
  - dotnet build ApexTweaker.sln -c Release
  - Invocar CatalogFeedbackSelfTest.Run() (harness no self-test; wire Program = proposta)

Fallback timeout:
  - Entregar enum estados + empty/error XAML + SnackbarKind.Error + remover substring classifier
  - Self-test mínimo (empty + error kind)
  - CTA copy correta mesmo se navegação for evento simples

Reviewer: Orquestrador (+ Codex: confirmar zero APIs BE novas)
```
