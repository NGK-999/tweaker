using System;
using Renomeador.Infrastructure;
using Renomeador.Models;
using Renomeador.Services;

namespace ApexTweaker.Core.Pipeline;

internal sealed class ProcessorIdleStatesTweakCommand : ISystemMutationCommand
{
    private const string SchemeCurrent = "SCHEME_CURRENT";
    private const string SubProcessor = "SUB_PROCESSOR";
    private const string IdleDisableAlias = "IDLEDISABLE";
    private const string IdleDisableGuid = "5d76a2ca-e8c0-402f-a133-2158492d58ad";

    private readonly CommandRunner commandRunner = new();
    private readonly BackupService readBackService = new();

    public string Name => "powercfg IDLEDISABLE=1";

    public string SuccessMessage =>
        "Processor idle states desativados no plano de energia atual com snapshot real do valor anterior.";

    public string FailurePrefix => "Falha ao aplicar Processor Idle States";

    public void Validate()
    {
        var activeScheme = readBackService.ReadActivePowerScheme();
        if (string.IsNullOrWhiteSpace(activeScheme))
        {
            throw new InvalidOperationException("Nao foi possivel identificar o plano de energia ativo.");
        }

        if (!readBackService.TryReadPowerSettingValue(
                SchemeCurrent,
                SubProcessor,
                IdleDisableGuid,
                isAcValue: true,
                out _,
                out var error))
        {
            throw new InvalidOperationException(error);
        }
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        backupService.CapturePowerSettingValue(
            session,
            SchemeCurrent,
            SubProcessor,
            IdleDisableGuid,
            isAcValue: true);
    }

    public void Execute()
    {
        RunPowercfg($"/setacvalueindex {SchemeCurrent} {SubProcessor} {IdleDisableAlias} 1");
        RunPowercfg($"/setactive {SchemeCurrent}");
    }

    public void Verify()
    {
        if (!readBackService.TryReadPowerSettingValue(
                SchemeCurrent,
                SubProcessor,
                IdleDisableGuid,
                isAcValue: true,
                out var actualValue,
                out var error))
        {
            throw new InvalidOperationException(error);
        }

        if (actualValue != 1)
        {
            throw new InvalidOperationException($"Read-back divergente para IDLEDISABLE. Esperado=1, Atual={actualValue}.");
        }
    }

    private void RunPowercfg(string arguments)
    {
        var result = commandRunner.Run("powercfg", arguments);
        if (result.ExitCode == 0)
        {
            return;
        }

        if (arguments.Contains(IdleDisableAlias, StringComparison.OrdinalIgnoreCase))
        {
            var fallback = arguments.Replace(IdleDisableAlias, IdleDisableGuid, StringComparison.OrdinalIgnoreCase);
            result = commandRunner.Run("powercfg", fallback);
            if (result.ExitCode == 0)
            {
                return;
            }
        }

        throw new InvalidOperationException(result.Output);
    }
}
