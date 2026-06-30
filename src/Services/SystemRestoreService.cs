using System;
using System.Collections.Generic;
using System.Management;
using System.Security.Principal;

namespace ApexTweaker.Services;

internal static class SystemRestoreService
{
    private const string RestorePointDescription = "Apex Tweaker - Pré-Otimizacao";
    private const int ModifySettingsRestorePointType = 12;
    private const int BeginSystemChangeEventType = 100;

    public static IReadOnlyList<string> CreatePreOptimizationRestorePoint()
    {
        var log = new List<string>();

        if (!IsRunningAsAdministrator())
        {
            log.Add("[AVISO] Ponto de restauração ignorado: execute o ApexTweaker como Administrador.");
            return log;
        }

        try
        {
            var scope = new ManagementScope(@"\\.\root\default");
            scope.Connect();

            using var systemRestore = new ManagementClass(scope, new ManagementPath("SystemRestore"), options: null);
            using var inParams = systemRestore.GetMethodParameters("CreateRestorePoint");
            inParams["Description"] = RestorePointDescription;
            inParams["RestorePointType"] = ModifySettingsRestorePointType;
            inParams["EventType"] = BeginSystemChangeEventType;

            using var outParams = systemRestore.InvokeMethod("CreateRestorePoint", inParams, null);
            var returnValue = Convert.ToInt32(outParams?["ReturnValue"] ?? -1);

            if (returnValue == 0)
            {
                log.Add($"Ponto de restauração criado: {RestorePointDescription}");
                return log;
            }

            log.Add($"[AVISO] O Windows recusou criar o ponto de restauração. Codigo WMI={returnValue}.");
        }
        catch (UnauthorizedAccessException ex)
        {
            log.Add($"[AVISO] Permissão insuficiente para criar ponto de restauração: {ex.Message}");
        }
        catch (ManagementException ex)
        {
            log.Add($"[AVISO] WMI/System Restore indisponível: {ex.Message}");
        }
        catch (Exception ex)
        {
            log.Add($"[AVISO] Falha inesperada ao criar ponto de restauração: {ex.Message}");
        }

        return log;
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
