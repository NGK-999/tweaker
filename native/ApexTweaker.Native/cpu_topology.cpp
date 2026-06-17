#include "exports.h"

#include <intrin.h>
#include <memory>
#include <vector>
#include <algorithm>
#include <cstring>

namespace
{
    struct GroupMaskDescriptor
    {
        WORD Group = 0;
        KAFFINITY Mask = 0;
        DWORD CacheSize = 0;
    };

    void ResetTopology(AT_CPU_TOPOLOGY* topology)
    {
        if (topology == nullptr)
        {
            return;
        }

        std::memset(topology, 0, sizeof(AT_CPU_TOPOLOGY));
        topology->StructSize = sizeof(AT_CPU_TOPOLOGY);
    }

    bool AddOrMergeEntry(
        AT_GROUP_AFFINITY_ENTRY* entries,
        std::uint32_t& count,
        WORD group,
        KAFFINITY mask)
    {
        if (mask == 0)
        {
            return true;
        }

        for (std::uint32_t index = 0; index < count; ++index)
        {
            if (entries[index].Group == group)
            {
                entries[index].Mask |= static_cast<std::uint64_t>(mask);
                return true;
            }
        }

        if (count >= AT_MAX_GROUP_AFFINITY_ENTRIES)
        {
            return false;
        }

        entries[count].Group = group;
        entries[count].Reserved = 0;
        entries[count].Reserved2 = 0;
        entries[count].Mask = static_cast<std::uint64_t>(mask);
        ++count;
        return true;
    }

    bool MergeMaskSets(
        AT_GROUP_AFFINITY_ENTRY* destination,
        std::uint32_t& destinationCount,
        const std::vector<GroupMaskDescriptor>& source)
    {
        for (const GroupMaskDescriptor& descriptor : source)
        {
            if (!AddOrMergeEntry(destination, destinationCount, descriptor.Group, descriptor.Mask))
            {
                apex_native_internal::SetLastNativeError(ERROR_INSUFFICIENT_BUFFER);
                return false;
            }
        }

        return true;
    }

    bool QueryLogicalProcessorBuffer(
        LOGICAL_PROCESSOR_RELATIONSHIP relationship,
        std::unique_ptr<BYTE[]>& buffer,
        DWORD& bufferLength)
    {
        bufferLength = 0;
        if (GetLogicalProcessorInformationEx(relationship, nullptr, &bufferLength) ||
            GetLastError() != ERROR_INSUFFICIENT_BUFFER ||
            bufferLength == 0)
        {
            apex_native_internal::SetLastNativeError(GetLastError());
            return false;
        }

        buffer.reset(new (std::nothrow) BYTE[bufferLength]);
        if (!buffer)
        {
            apex_native_internal::SetLastNativeError(ERROR_OUTOFMEMORY);
            return false;
        }

        if (!GetLogicalProcessorInformationEx(
                relationship,
                reinterpret_cast<PSYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(buffer.get()),
                &bufferLength))
        {
            apex_native_internal::SetLastNativeError(GetLastError());
            return false;
        }

        return true;
    }

    bool DetectCpuVendor(bool& isIntel, bool& isAmd)
    {
        int cpuInfo[4] = {};
        __cpuid(cpuInfo, 0);

        char vendor[13] = {};
        std::memcpy(vendor + 0, &cpuInfo[1], sizeof(int));
        std::memcpy(vendor + 4, &cpuInfo[3], sizeof(int));
        std::memcpy(vendor + 8, &cpuInfo[2], sizeof(int));
        vendor[12] = '\0';

        isIntel = std::strcmp(vendor, "GenuineIntel") == 0;
        isAmd = std::strcmp(vendor, "AuthenticAMD") == 0;
        return isIntel || isAmd;
    }

    bool CollectAllCoreMasks(
        std::vector<GroupMaskDescriptor>& allCoreMasks,
        BYTE& minEfficiencyClass,
        BYTE& maxEfficiencyClass,
        std::uint32_t& logicalPerformanceCoreCount,
        std::uint32_t& logicalEfficiencyCoreCount,
        bool& isHybrid)
    {
        std::unique_ptr<BYTE[]> buffer;
        DWORD bufferLength = 0;
        if (!QueryLogicalProcessorBuffer(RelationProcessorCore, buffer, bufferLength))
        {
            return false;
        }

        BYTE* cursor = buffer.get();
        BYTE* end = cursor + bufferLength;
        bool hasEfficiency = false;
        minEfficiencyClass = 0xFF;
        maxEfficiencyClass = 0x00;

        while (cursor < end)
        {
            auto* item = reinterpret_cast<PSYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(cursor);
            if (item->Relationship == RelationProcessorCore)
            {
                const PROCESSOR_RELATIONSHIP& processor = item->Processor;
                const BYTE efficiencyClass = processor.EfficiencyClass;
                minEfficiencyClass = std::min(minEfficiencyClass, efficiencyClass);
                maxEfficiencyClass = std::max(maxEfficiencyClass, efficiencyClass);
                hasEfficiency = true;

                for (WORD groupIndex = 0; groupIndex < processor.GroupCount; ++groupIndex)
                {
                    const GROUP_AFFINITY& groupMask = processor.GroupMask[groupIndex];
                    if (groupMask.Mask == 0)
                    {
                        continue;
                    }

                    GroupMaskDescriptor descriptor;
                    descriptor.Group = groupMask.Group;
                    descriptor.Mask = groupMask.Mask;
                    allCoreMasks.push_back(descriptor);
                }
            }

            cursor += item->Size;
        }

        if (!hasEfficiency)
        {
            minEfficiencyClass = 0;
            maxEfficiencyClass = 0;
        }

        isHybrid = hasEfficiency && minEfficiencyClass != maxEfficiencyClass;

        if (isHybrid)
        {
            cursor = buffer.get();
            while (cursor < end)
            {
                auto* item = reinterpret_cast<PSYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(cursor);
                if (item->Relationship == RelationProcessorCore)
                {
                    const PROCESSOR_RELATIONSHIP& processor = item->Processor;
                    if (processor.EfficiencyClass == maxEfficiencyClass)
                    {
                        ++logicalPerformanceCoreCount;
                    }
                    else
                    {
                        ++logicalEfficiencyCoreCount;
                    }
                }

                cursor += item->Size;
            }
        }
        else
        {
            logicalPerformanceCoreCount = static_cast<std::uint32_t>(allCoreMasks.size());
            logicalEfficiencyCoreCount = 0;
        }

        return true;
    }

    bool BuildHybridTopology(
        AT_CPU_TOPOLOGY* topology,
        BYTE targetPerformanceEfficiencyClass,
        std::uint32_t& performanceCoreCount,
        std::uint32_t& efficiencyCoreCount)
    {
        std::unique_ptr<BYTE[]> buffer;
        DWORD bufferLength = 0;
        if (!QueryLogicalProcessorBuffer(RelationProcessorCore, buffer, bufferLength))
        {
            return false;
        }

        BYTE* cursor = buffer.get();
        BYTE* end = cursor + bufferLength;
        while (cursor < end)
        {
            auto* item = reinterpret_cast<PSYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(cursor);
            if (item->Relationship == RelationProcessorCore)
            {
                const PROCESSOR_RELATIONSHIP& processor = item->Processor;
                const bool isPerformanceCore = processor.EfficiencyClass == targetPerformanceEfficiencyClass;

                for (WORD groupIndex = 0; groupIndex < processor.GroupCount; ++groupIndex)
                {
                    const GROUP_AFFINITY& groupMask = processor.GroupMask[groupIndex];
                    if (groupMask.Mask == 0)
                    {
                        continue;
                    }

                    if (isPerformanceCore)
                    {
                        if (!AddOrMergeEntry(
                                topology->PerformanceGroups,
                                topology->PerformanceGroupCount,
                                groupMask.Group,
                                groupMask.Mask))
                        {
                            apex_native_internal::SetLastNativeError(ERROR_INSUFFICIENT_BUFFER);
                            return false;
                        }
                    }
                    else
                    {
                        if (!AddOrMergeEntry(
                                topology->EfficiencyGroups,
                                topology->EfficiencyGroupCount,
                                groupMask.Group,
                                groupMask.Mask))
                        {
                            apex_native_internal::SetLastNativeError(ERROR_INSUFFICIENT_BUFFER);
                            return false;
                        }
                    }
                }

                if (isPerformanceCore)
                {
                    ++performanceCoreCount;
                }
                else
                {
                    ++efficiencyCoreCount;
                }
            }

            cursor += item->Size;
        }

        return true;
    }

    bool TryBuildAmdPreferredCcd(
        const std::vector<GroupMaskDescriptor>& allCoreMasks,
        AT_CPU_TOPOLOGY* topology,
        std::uint32_t& performanceCoreCount,
        std::uint32_t& efficiencyCoreCount)
    {
        std::unique_ptr<BYTE[]> buffer;
        DWORD bufferLength = 0;
        if (!QueryLogicalProcessorBuffer(RelationCache, buffer, bufferLength))
        {
            return false;
        }

        std::vector<GroupMaskDescriptor> l3Descriptors;
        BYTE* cursor = buffer.get();
        BYTE* end = cursor + bufferLength;
        while (cursor < end)
        {
            auto* item = reinterpret_cast<PSYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(cursor);
            if (item->Relationship == RelationCache)
            {
                const CACHE_RELATIONSHIP& cache = item->Cache;
                if (cache.Level == 3 && cache.GroupMask.Mask != 0)
                {
                    GroupMaskDescriptor descriptor;
                    descriptor.Group = cache.GroupMask.Group;
                    descriptor.Mask = cache.GroupMask.Mask;
                    descriptor.CacheSize = cache.CacheSize;
                    l3Descriptors.push_back(descriptor);
                }
            }

            cursor += item->Size;
        }

        if (l3Descriptors.empty())
        {
            return false;
        }

        DWORD largestL3Bytes = 0;
        for (const GroupMaskDescriptor& descriptor : l3Descriptors)
        {
            largestL3Bytes = std::max(largestL3Bytes, descriptor.CacheSize);
        }

        std::vector<GroupMaskDescriptor> preferredMasks;
        std::vector<GroupMaskDescriptor> remainingMasks;
        for (const GroupMaskDescriptor& descriptor : l3Descriptors)
        {
            if (descriptor.CacheSize == largestL3Bytes)
            {
                preferredMasks.push_back(descriptor);
            }
        }

        if (preferredMasks.empty())
        {
            return false;
        }

        if (!MergeMaskSets(topology->PerformanceGroups, topology->PerformanceGroupCount, preferredMasks))
        {
            return false;
        }

        for (const GroupMaskDescriptor& descriptor : allCoreMasks)
        {
            KAFFINITY remainingMask = descriptor.Mask;
            for (const GroupMaskDescriptor& preferred : preferredMasks)
            {
                if (preferred.Group == descriptor.Group)
                {
                    remainingMask &= ~preferred.Mask;
                }
            }

            if (remainingMask != 0)
            {
                GroupMaskDescriptor spill;
                spill.Group = descriptor.Group;
                spill.Mask = remainingMask;
                remainingMasks.push_back(spill);
            }
        }

        if (!MergeMaskSets(topology->EfficiencyGroups, topology->EfficiencyGroupCount, remainingMasks))
        {
            return false;
        }

        performanceCoreCount = 0;
        for (const GroupMaskDescriptor& preferred : preferredMasks)
        {
            performanceCoreCount += static_cast<std::uint32_t>(__popcnt64(static_cast<unsigned __int64>(preferred.Mask)));
        }

        efficiencyCoreCount = 0;
        for (const GroupMaskDescriptor& remaining : remainingMasks)
        {
            efficiencyCoreCount += static_cast<std::uint32_t>(__popcnt64(static_cast<unsigned __int64>(remaining.Mask)));
        }

        return true;
    }
}

namespace apex_native_internal
{
    thread_local DWORD g_lastWin32Error = ERROR_SUCCESS;

    void SetLastNativeError(DWORD errorCode) noexcept
    {
        g_lastWin32Error = errorCode;
    }
}

AT_API std::uint32_t __stdcall AT_GetLastWin32ErrorCode()
{
    return static_cast<std::uint32_t>(apex_native_internal::g_lastWin32Error);
}

AT_API AT_STATUS __stdcall AT_GetCpuTopology(AT_CPU_TOPOLOGY* topology)
{
    ResetTopology(topology);
    apex_native_internal::SetLastNativeError(ERROR_SUCCESS);

    if (topology == nullptr)
    {
        apex_native_internal::SetLastNativeError(ERROR_INVALID_PARAMETER);
        return AT_STATUS_INVALID_ARGUMENT;
    }

    bool isIntel = false;
    bool isAmd = false;
    DetectCpuVendor(isIntel, isAmd);

    std::vector<GroupMaskDescriptor> allCoreMasks;
    BYTE minEfficiencyClass = 0;
    BYTE maxEfficiencyClass = 0;
    std::uint32_t logicalPerformanceCoreCount = 0;
    std::uint32_t logicalEfficiencyCoreCount = 0;
    bool isHybrid = false;
    if (!CollectAllCoreMasks(
            allCoreMasks,
            minEfficiencyClass,
            maxEfficiencyClass,
            logicalPerformanceCoreCount,
            logicalEfficiencyCoreCount,
            isHybrid))
    {
        return AT_STATUS_WIN32_FAILURE;
    }

    if (allCoreMasks.empty())
    {
        apex_native_internal::SetLastNativeError(ERROR_NOT_SUPPORTED);
        return AT_STATUS_NOT_SUPPORTED;
    }

    if (isIntel)
    {
        topology->Flags |= AT_CPU_TOPOLOGY_FLAG_INTEL;
    }
    if (isAmd)
    {
        topology->Flags |= AT_CPU_TOPOLOGY_FLAG_AMD;
    }
    if (isHybrid)
    {
        topology->Flags |= AT_CPU_TOPOLOGY_FLAG_HYBRID;
    }

    if (isHybrid)
    {
        if (!BuildHybridTopology(
                topology,
                maxEfficiencyClass,
                topology->PerformanceCoreCount,
                topology->EfficiencyCoreCount))
        {
            return AT_STATUS_BUFFER_TOO_SMALL;
        }
    }
    else if (isAmd)
    {
        if (!TryBuildAmdPreferredCcd(
                allCoreMasks,
                topology,
                topology->PerformanceCoreCount,
                topology->EfficiencyCoreCount))
        {
            topology->PerformanceGroupCount = 0;
            if (!MergeMaskSets(topology->PerformanceGroups, topology->PerformanceGroupCount, allCoreMasks))
            {
                return AT_STATUS_BUFFER_TOO_SMALL;
            }

            topology->PerformanceCoreCount = logicalPerformanceCoreCount;
            topology->EfficiencyCoreCount = 0;
        }
    }
    else
    {
        if (!MergeMaskSets(topology->PerformanceGroups, topology->PerformanceGroupCount, allCoreMasks))
        {
            return AT_STATUS_BUFFER_TOO_SMALL;
        }

        topology->PerformanceCoreCount = logicalPerformanceCoreCount;
        topology->EfficiencyCoreCount = 0;
    }

    return AT_STATUS_SUCCESS;
}
