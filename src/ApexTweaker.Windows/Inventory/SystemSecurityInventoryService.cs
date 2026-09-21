using System.Management;
using Microsoft.Win32;

namespace ApexTweaker.Windows.Inventory;

/// <summary>
/// Leitura somente leitura de sinais de segurança do Windows. O coletor não
/// cria processos externos e pode ser usado durante o modo Demo.
/// </summary>
internal sealed class SystemSecurityInventoryService
{
    private const string SecureBootStatePath =
        @"SYSTEM\CurrentControlSet\Control\SecureBoot\State";

    public bool? ReadSecureBootEnabled()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(SecureBootStatePath);
            var value = key?.GetValue("UEFISecureBootEnabled");
            return value is null
                ? null
                : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) != 0;
        }
        catch
        {
            return null;
        }
    }

    public bool? ReadTpmPresent()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using var scope = new ManagementScope(
                @"\\.\root\CIMV2\Security\MicrosoftTpm");
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT IsEnabled_InitialValue FROM Win32_Tpm"));
            using var results = searcher.Get();

            // A Win32_Tpm instance means that a TPM is present. The detailed
            // enabled/activated state is intentionally left to the inventory
            // UI; this preserves the former TpmPresent contract.
            return results.Cast<ManagementObject>().Any();
        }
        catch
        {
            return null;
        }
    }
}
