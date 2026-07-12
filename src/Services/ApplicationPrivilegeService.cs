using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace ApexTweaker.Services;

internal enum ApplicationOperation
{
    HardwareDiagnostics,
    MinecraftFiles,
    MinecraftBenchmark,
    UserTelemetry,
    WindowsMutation,
    WindowsRollback,
    WindowsCleanup,
    KernelEtw
}

internal static class ApplicationPrivilegeService
{
    public static bool IsAdministrator
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static bool RequiresAdministrator(ApplicationOperation operation)
    {
        return operation is ApplicationOperation.WindowsMutation or
            ApplicationOperation.WindowsRollback or
            ApplicationOperation.WindowsCleanup or
            ApplicationOperation.KernelEtw;
    }

    public static void RestartElevated()
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new InvalidOperationException("Executavel atual nao foi identificado para elevacao.");
        }

        try
        {
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "--admin-mode",
                UseShellExecute = true,
                Verb = "runas"
            }) ?? throw new InvalidOperationException("O Windows nao iniciou o processo elevado.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            throw new OperationCanceledException("Elevacao cancelada pelo usuario.", ex);
        }
    }
}
