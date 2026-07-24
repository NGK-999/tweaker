using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using ApexTweaker.Contracts.Inventory;
using ApexTweaker.Models;
using ApexTweaker.Services;
using Microsoft.Win32;

namespace ApexTweaker.Windows.Inventory;

internal sealed class WindowsOptimizationInventoryService : IWindowsOptimizationInventory
{
    private const string WindowsVersionPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string MdmAccountsPath = @"SOFTWARE\Microsoft\Provisioning\OMADM\Accounts";
    private const string UserShellFoldersPath =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders";

    public WindowsOptimizationContext Capture(WindowsUsageProfile? usage = null)
    {
        var operatingSystem = ReadOperatingSystem();
        var computer = ReadComputerSystem();
        var processor = ReadProcessor();
        var gpus = ReadGpus();
        var topology = HardwareEnvironmentDetector.Detect();
        var deviceGuard = ReadDeviceGuard();
        var hasOneDriveRedirection = DetectOneDriveFolderRedirection();
        var mdmManaged = DetectMdmEnrollment();
        var inferredUsage = usage ?? InferUsage(
            computer.IsDomainJoined,
            mdmManaged,
            hasOneDriveRedirection);

        return new WindowsOptimizationContext(
            operatingSystem.ProductName,
            operatingSystem.Edition,
            operatingSystem.Build,
            operatingSystem.Revision,
            computer.DeviceKind,
            ReadPowerSource(),
            computer.Manufacturer,
            computer.Model,
            processor.Vendor,
            processor.Name,
            processor.PhysicalCoreCount,
            processor.LogicalCoreCount,
            topology.IsHybrid,
            computer.TotalMemoryGb,
            gpus,
            computer.IsDomainJoined,
            mdmManaged,
            deviceGuard.VbsEnabled,
            deviceGuard.MemoryIntegrityEnabled,
            deviceGuard.HypervisorPresent,
            hasOneDriveRedirection,
            inferredUsage);
    }

    private static OperatingSystemInventory ReadOperatingSystem()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WindowsVersionPath);
            var productName = ReadString(key, "ProductName", "Windows");
            var edition = ReadString(key, "EditionID", "Unknown");
            var build = ReadInt(key, "CurrentBuildNumber", Environment.OSVersion.Version.Build);
            var revision = ReadInt(key, "UBR", 0);

            if (build >= 22000 &&
                productName.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
            {
                productName = "Windows 11" + productName["Windows 10".Length..];
            }

            return new OperatingSystemInventory(productName, edition, build, revision);
        }
        catch
        {
            return new OperatingSystemInventory(
                "Windows",
                "Unknown",
                Environment.OSVersion.Version.Build,
                0);
        }
    }

    private static ComputerInventory ReadComputerSystem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Manufacturer, Model, PCSystemType, TotalPhysicalMemory, PartOfDomain " +
                "FROM Win32_ComputerSystem");
            using var results = searcher.Get();
            var computer = results.Cast<ManagementObject>().FirstOrDefault();
            if (computer is null)
            {
                return ComputerInventory.Unknown;
            }

            var systemType = ConvertToInt(computer["PCSystemType"]);
            var memoryBytes = ConvertToUInt64(computer["TotalPhysicalMemory"]);
            var deviceKind = systemType is 2 or 8 or 9 or 10
                ? WindowsDeviceKind.Laptop
                : systemType > 0
                    ? WindowsDeviceKind.Desktop
                    : WindowsDeviceKind.Unknown;

            return new ComputerInventory(
                computer["Manufacturer"]?.ToString()?.Trim() ?? "Unknown",
                computer["Model"]?.ToString()?.Trim() ?? "Unknown",
                deviceKind,
                Math.Round((decimal)memoryBytes / 1024m / 1024m / 1024m, 2),
                ConvertToBool(computer["PartOfDomain"]));
        }
        catch
        {
            return ComputerInventory.Unknown;
        }
    }

    private static ProcessorInventory ReadProcessor()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Manufacturer, Name, NumberOfCores, NumberOfLogicalProcessors " +
                "FROM Win32_Processor");
            using var results = searcher.Get();
            var processors = results.Cast<ManagementObject>().ToArray();
            if (processors.Length == 0)
            {
                return ProcessorInventory.Unknown;
            }

            var first = processors[0];
            return new ProcessorInventory(
                first["Manufacturer"]?.ToString()?.Trim() ?? "Unknown",
                first["Name"]?.ToString()?.Trim() ?? "Unknown",
                processors.Sum(processor => ConvertToInt(processor["NumberOfCores"])),
                processors.Sum(processor => ConvertToInt(processor["NumberOfLogicalProcessors"])));
        }
        catch
        {
            return ProcessorInventory.Unknown;
        }
    }

    private static IReadOnlyList<GpuInfo> ReadGpus()
    {
        var gpus = new List<GpuInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, AdapterCompatibility, DriverVersion FROM Win32_VideoController");
            using var results = searcher.Get();
            foreach (var gpu in results.Cast<ManagementObject>())
            {
                var name = gpu["Name"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                gpus.Add(new GpuInfo(
                    name,
                    gpu["AdapterCompatibility"]?.ToString()?.Trim() ?? DetectGpuVendor(name),
                    gpu["DriverVersion"]?.ToString()?.Trim() ?? "Unknown"));
            }
        }
        catch
        {
            // Inventory is best effort and remains read-only.
        }

        return gpus;
    }

    private static DeviceGuardInventory ReadDeviceGuard()
    {
        try
        {
            var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\DeviceGuard");
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery(
                    "SELECT VirtualizationBasedSecurityStatus, SecurityServicesRunning " +
                    "FROM Win32_DeviceGuard"));
            using var results = searcher.Get();
            var deviceGuard = results.Cast<ManagementObject>().FirstOrDefault();
            if (deviceGuard is null)
            {
                return DeviceGuardInventory.Unknown;
            }

            var vbsStatus = ConvertToInt(deviceGuard["VirtualizationBasedSecurityStatus"]);
            var runningServices = (deviceGuard["SecurityServicesRunning"] as Array)?
                .Cast<object>()
                .Select(ConvertToInt)
                .ToHashSet() ?? [];

            return new DeviceGuardInventory(
                vbsStatus > 0,
                runningServices.Contains(2),
                vbsStatus > 0);
        }
        catch
        {
            return DeviceGuardInventory.Unknown;
        }
    }

    private static bool DetectMdmEnrollment()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(MdmAccountsPath);
            return key?.GetSubKeyNames().Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool DetectOneDriveFolderRedirection()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(UserShellFoldersPath);
            if (key is null)
            {
                return false;
            }

            foreach (var name in new[] { "Desktop", "Personal", "My Pictures" })
            {
                var path = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames)?
                    .ToString();
                if (!string.IsNullOrWhiteSpace(path) &&
                    path.Contains("OneDrive", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Treat an unreadable location as unknown/not detected, never as permission to disable OneDrive.
        }

        return false;
    }

    private static WindowsPowerSource ReadPowerSource()
    {
        try
        {
            return GetSystemPowerStatus(out var status)
                ? status.AcLineStatus switch
                {
                    0 => WindowsPowerSource.Battery,
                    1 => WindowsPowerSource.Ac,
                    _ => WindowsPowerSource.Unknown
                }
                : WindowsPowerSource.Unknown;
        }
        catch
        {
            return WindowsPowerSource.Unknown;
        }
    }

    private static WindowsUsageProfile InferUsage(
        bool domainJoined,
        bool mdmManaged,
        bool hasOneDriveRedirection)
    {
        return WindowsUsageProfile.Unknown with
        {
            IsCorporateComputer = domainJoined || mdmManaged ? UsageAnswer.Yes : UsageAnswer.Unknown,
            UsesOneDrive = hasOneDriveRedirection ? UsageAnswer.Yes : UsageAnswer.Unknown
        };
    }

    private static string ReadString(RegistryKey? key, string name, string fallback) =>
        key?.GetValue(name)?.ToString()?.Trim() is { Length: > 0 } value ? value : fallback;

    private static int ReadInt(RegistryKey? key, string name, int fallback)
    {
        var value = key?.GetValue(name);
        return int.TryParse(
            value?.ToString(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : fallback;
    }

    private static int ConvertToInt(object? value)
    {
        try
        {
            return value is null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    private static ulong ConvertToUInt64(object? value)
    {
        try
        {
            return value is null ? 0UL : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0UL;
        }
    }

    private static bool ConvertToBool(object? value)
    {
        try
        {
            return value is not null && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return false;
        }
    }

    private static string DetectGpuVendor(string name)
    {
        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            return "NVIDIA";
        }

        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("RADEON", StringComparison.OrdinalIgnoreCase))
        {
            return "AMD";
        }

        if (name.Contains("INTEL", StringComparison.OrdinalIgnoreCase))
        {
            return "Intel";
        }

        return "Unknown";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    private sealed record OperatingSystemInventory(
        string ProductName,
        string Edition,
        int Build,
        int Revision);

    private sealed record ComputerInventory(
        string Manufacturer,
        string Model,
        WindowsDeviceKind DeviceKind,
        decimal TotalMemoryGb,
        bool IsDomainJoined)
    {
        public static ComputerInventory Unknown { get; } =
            new("Unknown", "Unknown", WindowsDeviceKind.Unknown, 0m, false);
    }

    private sealed record ProcessorInventory(
        string Vendor,
        string Name,
        int PhysicalCoreCount,
        int LogicalCoreCount)
    {
        public static ProcessorInventory Unknown { get; } =
            new("Unknown", "Unknown", 0, Environment.ProcessorCount);
    }

    private sealed record DeviceGuardInventory(
        bool VbsEnabled,
        bool MemoryIntegrityEnabled,
        bool HypervisorPresent)
    {
        public static DeviceGuardInventory Unknown { get; } = new(false, false, false);
    }
}
