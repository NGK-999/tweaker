using System.Runtime.InteropServices;

namespace ApexTweaker.NativeInterop;

[Flags]
internal enum CpuTopologyFlags : uint
{
    None = 0,
    Hybrid = 0x00000001,
    Amd = 0x00000002,
    Intel = 0x00000004
}

internal enum NativeStatus : int
{
    Success = 0,
    InvalidArgument = 1,
    BufferTooSmall = 2,
    NotSupported = 3,
    Win32Failure = 4
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct GroupAffinityEntry
{
    public ushort Group;
    public ushort Reserved;
    public uint Reserved2;
    public ulong Mask;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal unsafe struct CpuTopologyNative
{
    internal const int MaxGroupEntries = 8;
    private const int GroupAffinityEntrySize = 16;
    private const int GroupBufferSize = MaxGroupEntries * GroupAffinityEntrySize;

    public uint StructSize;
    public CpuTopologyFlags Flags;
    public uint PerformanceCoreCount;
    public uint EfficiencyCoreCount;
    public uint PerformanceGroupCount;
    public uint EfficiencyGroupCount;
    public fixed byte PerformanceGroups[GroupBufferSize];
    public fixed byte EfficiencyGroups[GroupBufferSize];

    public readonly bool IsHybrid => (Flags & CpuTopologyFlags.Hybrid) != 0;
    public readonly bool IsAmd => (Flags & CpuTopologyFlags.Amd) != 0;
    public readonly bool IsIntel => (Flags & CpuTopologyFlags.Intel) != 0;

    public GroupAffinityEntry[] GetPerformanceGroups()
    {
        fixed (byte* buffer = PerformanceGroups)
        {
            return CopyEntries(buffer, PerformanceGroupCount);
        }
    }

    public GroupAffinityEntry[] GetEfficiencyGroups()
    {
        fixed (byte* buffer = EfficiencyGroups)
        {
            return CopyEntries(buffer, EfficiencyGroupCount);
        }
    }

    private static unsafe GroupAffinityEntry[] CopyEntries(byte* source, uint count)
    {
        var boundedCount = (int)Math.Min(count, MaxGroupEntries);
        if (boundedCount <= 0)
        {
            return Array.Empty<GroupAffinityEntry>();
        }

        var entries = new GroupAffinityEntry[boundedCount];
        fixed (GroupAffinityEntry* destination = entries)
        {
            Buffer.MemoryCopy(
                source,
                destination,
                boundedCount * (long)sizeof(GroupAffinityEntry),
                boundedCount * (long)sizeof(GroupAffinityEntry));
        }

        return entries;
    }
}

internal static unsafe class NativeMethods
{
    private const string DllName = "ApexTweaker.Native.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern NativeStatus AT_GetCpuTopology(out CpuTopologyNative topology);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern NativeStatus AT_BuildPreferredGameAffinityMask(
        in CpuTopologyNative topology,
        [Out] GroupAffinityEntry[] outputEntries,
        uint outputCapacity,
        out uint writtenEntries);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern uint AT_GetLastWin32ErrorCode();

    internal static NativeStatus GetCpuTopology(out CpuTopologyNative topology, out int lastWin32Error)
    {
        topology = default;
        topology.StructSize = (uint)sizeof(CpuTopologyNative);

        var status = AT_GetCpuTopology(out topology);
        lastWin32Error = unchecked((int)AT_GetLastWin32ErrorCode());
        return status;
    }

    internal static NativeStatus BuildPreferredGameAffinityMask(
        in CpuTopologyNative topology,
        out GroupAffinityEntry[] preferredEntries,
        out int lastWin32Error)
    {
        preferredEntries = new GroupAffinityEntry[CpuTopologyNative.MaxGroupEntries];
        var status = AT_BuildPreferredGameAffinityMask(
            topology,
            preferredEntries,
            CpuTopologyNative.MaxGroupEntries,
            out var writtenEntries);

        lastWin32Error = unchecked((int)AT_GetLastWin32ErrorCode());

        if (status != NativeStatus.Success)
        {
            if (status != NativeStatus.BufferTooSmall)
            {
                preferredEntries = Array.Empty<GroupAffinityEntry>();
            }

            return status;
        }

        if (writtenEntries < preferredEntries.Length)
        {
            Array.Resize(ref preferredEntries, (int)writtenEntries);
        }

        return status;
    }
}
