#pragma once

#include <windows.h>
#include <cstdint>

#ifdef APEXTWEAKER_NATIVE_EXPORTS
#define AT_API extern "C" __declspec(dllexport)
#else
#define AT_API extern "C" __declspec(dllimport)
#endif

constexpr std::uint32_t AT_MAX_GROUP_AFFINITY_ENTRIES = 8;

enum AT_STATUS : std::int32_t
{
    AT_STATUS_SUCCESS = 0,
    AT_STATUS_INVALID_ARGUMENT = 1,
    AT_STATUS_BUFFER_TOO_SMALL = 2,
    AT_STATUS_NOT_SUPPORTED = 3,
    AT_STATUS_WIN32_FAILURE = 4
};

enum AT_CPU_TOPOLOGY_FLAGS : std::uint32_t
{
    AT_CPU_TOPOLOGY_FLAG_HYBRID = 0x00000001,
    AT_CPU_TOPOLOGY_FLAG_AMD = 0x00000002,
    AT_CPU_TOPOLOGY_FLAG_INTEL = 0x00000004
};

#pragma pack(push, 8)
struct AT_GROUP_AFFINITY_ENTRY
{
    std::uint16_t Group;
    std::uint16_t Reserved;
    std::uint32_t Reserved2;
    std::uint64_t Mask;
};

struct AT_CPU_TOPOLOGY
{
    std::uint32_t StructSize;
    std::uint32_t Flags;
    std::uint32_t PerformanceCoreCount;
    std::uint32_t EfficiencyCoreCount;
    std::uint32_t PerformanceGroupCount;
    std::uint32_t EfficiencyGroupCount;
    AT_GROUP_AFFINITY_ENTRY PerformanceGroups[AT_MAX_GROUP_AFFINITY_ENTRIES];
    AT_GROUP_AFFINITY_ENTRY EfficiencyGroups[AT_MAX_GROUP_AFFINITY_ENTRIES];
};
#pragma pack(pop)

AT_API AT_STATUS __stdcall AT_GetCpuTopology(AT_CPU_TOPOLOGY* topology);
AT_API AT_STATUS __stdcall AT_BuildPreferredGameAffinityMask(
    const AT_CPU_TOPOLOGY* topology,
    AT_GROUP_AFFINITY_ENTRY* outputEntries,
    std::uint32_t outputCapacity,
    std::uint32_t* writtenEntries);
AT_API std::uint32_t __stdcall AT_GetLastWin32ErrorCode();

#ifdef __cplusplus
namespace apex_native_internal
{
    extern thread_local DWORD g_lastWin32Error;
    void SetLastNativeError(DWORD errorCode) noexcept;
}
#endif
