using System.Runtime.InteropServices;
using ApexTweaker.NativeInterop;

namespace Renomeador.Services;

internal static class IntelHybridProbeStrategy
{
    public static HashSet<int> ResolvePerformanceCoreSensorIndexes(in CpuTopologyNative topology)
    {
        if (!topology.IsHybrid)
        {
            return [];
        }

        var preferredMasks = BuildPerformanceMaskMap(topology.GetPerformanceGroups());
        if (preferredMasks.Count == 0)
        {
            return [];
        }

        var mappings = ReadCoreMappings();
        if (mappings.Count == 0)
        {
            return [];
        }

        var performanceIndexes = new HashSet<int>();
        foreach (var mapping in mappings)
        {
            foreach (var affinity in mapping.GroupAffinities)
            {
                if (preferredMasks.TryGetValue(affinity.Group, out var preferredMask) &&
                    (affinity.Mask & preferredMask) != 0)
                {
                    performanceIndexes.Add(mapping.CoreIndex);
                    break;
                }
            }
        }

        return performanceIndexes;
    }

    private static Dictionary<ushort, ulong> BuildPerformanceMaskMap(GroupAffinityEntry[] entries)
    {
        var map = new Dictionary<ushort, ulong>();
        foreach (var entry in entries)
        {
            if (entry.Mask == 0)
            {
                continue;
            }

            map[entry.Group] = map.TryGetValue(entry.Group, out var currentMask)
                ? currentMask | entry.Mask
                : entry.Mask;
        }

        return map;
    }

    private static List<CoreGroupMapping> ReadCoreMappings()
    {
        var size = 0U;
        _ = GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, IntPtr.Zero, ref size);
        if (size == 0)
        {
            return [];
        }

        var buffer = Marshal.AllocHGlobal(checked((int)size));
        try
        {
            if (!GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, buffer, ref size))
            {
                return [];
            }

            var mappings = new List<CoreGroupMapping>();
            var cursor = buffer;
            var end = IntPtr.Add(buffer, checked((int)size));
            var coreIndex = 0;
            var headerSize = Marshal.SizeOf<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER>();
            var processorHeaderSize = Marshal.SizeOf<PROCESSOR_RELATIONSHIP_HEADER>();
            var affinitySize = Marshal.SizeOf<GroupAffinityEntry>();

            while (cursor.ToInt64() < end.ToInt64())
            {
                var header = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER>(cursor);
                if (header.Size <= 0)
                {
                    break;
                }

                if (header.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                {
                    var processorHeaderPtr = IntPtr.Add(cursor, headerSize);
                    var processorHeader = Marshal.PtrToStructure<PROCESSOR_RELATIONSHIP_HEADER>(processorHeaderPtr);
                    if (processorHeader.GroupCount > 0)
                    {
                        var groupMasks = new GroupAffinityEntry[processorHeader.GroupCount];
                        var groupMaskPtr = IntPtr.Add(processorHeaderPtr, processorHeaderSize);
                        for (var groupIndex = 0; groupIndex < processorHeader.GroupCount; groupIndex++)
                        {
                            groupMasks[groupIndex] = Marshal.PtrToStructure<GroupAffinityEntry>(
                                IntPtr.Add(groupMaskPtr, groupIndex * affinitySize));
                        }

                        mappings.Add(new CoreGroupMapping(coreIndex, groupMasks));
                    }

                    coreIndex++;
                }

                cursor = IntPtr.Add(cursor, header.Size);
            }

            return mappings;
        }
        catch
        {
            return [];
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLogicalProcessorInformationEx(
        LOGICAL_PROCESSOR_RELATIONSHIP relationshipType,
        IntPtr buffer,
        ref uint returnedLength);

    private enum LOGICAL_PROCESSOR_RELATIONSHIP
    {
        RelationProcessorCore = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_HEADER
    {
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public int Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESSOR_RELATIONSHIP_HEADER
    {
        public byte Flags;
        public byte EfficiencyClass;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Reserved;

        public ushort GroupCount;
    }

    private sealed record CoreGroupMapping(int CoreIndex, GroupAffinityEntry[] GroupAffinities);
}
