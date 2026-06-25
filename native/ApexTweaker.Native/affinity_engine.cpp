#include "exports.h"

#include <cstring>

namespace
{
    bool CopyEntry(
        const AT_GROUP_AFFINITY_ENTRY& source,
        AT_GROUP_AFFINITY_ENTRY* destination,
        std::uint32_t outputCapacity,
        std::uint32_t& written)
    {
        if (source.Mask == 0)
        {
            return true;
        }

        if (written >= outputCapacity)
        {
            apex_native_internal::SetLastNativeError(ERROR_INSUFFICIENT_BUFFER);
            return false;
        }

        destination[written] = source;
        ++written;
        return true;
    }
}

AT_API AT_STATUS __stdcall AT_BuildPreferredGameAffinityMask(
    const AT_CPU_TOPOLOGY* topology,
    AT_GROUP_AFFINITY_ENTRY* outputEntries,
    std::uint32_t outputCapacity,
    std::uint32_t* writtenEntries)
{
    apex_native_internal::SetLastNativeError(ERROR_SUCCESS);

    if (topology == nullptr || outputEntries == nullptr || writtenEntries == nullptr)
    {
        apex_native_internal::SetLastNativeError(ERROR_INVALID_PARAMETER);
        return AT_STATUS_INVALID_ARGUMENT;
    }

    *writtenEntries = 0;
    std::memset(outputEntries, 0, sizeof(AT_GROUP_AFFINITY_ENTRY) * outputCapacity);

    const bool hasPreferredGroups =
        topology->PerformanceGroupCount > 0 &&
        topology->PerformanceCoreCount > 0;

    if (hasPreferredGroups)
    {
        for (std::uint32_t index = 0; index < topology->PerformanceGroupCount; ++index)
        {
            if (!CopyEntry(topology->PerformanceGroups[index], outputEntries, outputCapacity, *writtenEntries))
            {
                return AT_STATUS_BUFFER_TOO_SMALL;
            }
        }

        return *writtenEntries > 0 ? AT_STATUS_SUCCESS : AT_STATUS_NOT_SUPPORTED;
    }

    for (std::uint32_t index = 0; index < topology->EfficiencyGroupCount; ++index)
    {
        if (!CopyEntry(topology->EfficiencyGroups[index], outputEntries, outputCapacity, *writtenEntries))
        {
            return AT_STATUS_BUFFER_TOO_SMALL;
        }
    }

    return *writtenEntries > 0 ? AT_STATUS_SUCCESS : AT_STATUS_NOT_SUPPORTED;
}
