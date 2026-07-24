// Shared inventory contract. Keep the namespace stable during the assembly split.
namespace ApexTweaker.Models;

internal sealed record GpuInfo(string Name, string Vendor, string DriverVersion);
