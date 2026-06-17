using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Renomeador.Services;

internal static class WindowsPowerModeService
{
    public const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    private static readonly Guid BestPerformanceModeGuid = new("ded574b5-45a0-4f42-8737-46345c09c238");

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerSetUserConfiguredACPowerMode(ref Guid powerModeGuid);

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerGetUserConfiguredACPowerMode(out Guid powerModeGuid);

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerSetUserConfiguredDCPowerMode(ref Guid powerModeGuid);

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerGetUserConfiguredDCPowerMode(out Guid powerModeGuid);

    public static string BestPerformanceGuidText => BestPerformanceModeGuid.ToString();

    public static bool TryApplyBestPerformanceOverlay(out string actualState, out string diagnostic)
    {
        actualState = string.Empty;
        diagnostic = string.Empty;

        try
        {
            var requestedAcMode = BestPerformanceModeGuid;
            var acSetStatus = PowerSetUserConfiguredACPowerMode(ref requestedAcMode);
            if (acSetStatus != 0)
            {
                diagnostic = $"PowerSetUserConfiguredACPowerMode retornou 0x{acSetStatus:X8}.";
                return false;
            }

            var acReadStatus = PowerGetUserConfiguredACPowerMode(out var actualAcMode);
            if (acReadStatus != 0)
            {
                diagnostic = $"PowerGetUserConfiguredACPowerMode retornou 0x{acReadStatus:X8}.";
                return false;
            }

            var requestedDcMode = BestPerformanceModeGuid;
            var dcSetStatus = PowerSetUserConfiguredDCPowerMode(ref requestedDcMode);
            var dcReadStatus = PowerGetUserConfiguredDCPowerMode(out var actualDcMode);

            actualState = dcReadStatus == 0
                ? $"AC={actualAcMode} | DC={actualDcMode}"
                : $"AC={actualAcMode}";

            if (actualAcMode != BestPerformanceModeGuid)
            {
                diagnostic = $"Power Mode AC retornou {actualAcMode}, esperado {BestPerformanceModeGuid}.";
                return false;
            }

            if (dcSetStatus == 0 && dcReadStatus == 0 && actualDcMode != BestPerformanceModeGuid)
            {
                diagnostic = $"Power Mode DC retornou {actualDcMode}, esperado {BestPerformanceModeGuid}.";
                return false;
            }

            diagnostic = dcSetStatus == 0
                ? "Windows 11 Power Mode ajustado para Best Performance em AC/DC."
                : $"Windows 11 Power Mode ajustado para Best Performance em AC. DC retornou 0x{dcSetStatus:X8}.";
            return true;
        }
        catch (DllNotFoundException ex)
        {
            diagnostic = ex.Message;
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            diagnostic = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            diagnostic = ex.Message;
            return false;
        }
    }

    public static bool IsLegacyPowercfgSettingUnsupported(string? output)
    {
        var normalized = NormalizePowercfgOutput(output);
        return normalized.Contains("SETTING SPECIFIED DOES NOT EXIST", StringComparison.Ordinal) ||
               normalized.Contains("POWER SCHEME, SUBGROUP OR SETTING SPECIFIED DOES NOT EXIST", StringComparison.Ordinal) ||
               normalized.Contains("THE POWER SCHEME, SUBGROUP OR SETTING SPECIFIED DOES NOT EXIST", StringComparison.Ordinal) ||
               normalized.Contains("ESQUEMA DE ENERGIA", StringComparison.Ordinal) ||
               normalized.Contains("SUBGRUPO", StringComparison.Ordinal) ||
               normalized.Contains("CONFIGURACAO ESPECIFICADO NAO EXISTE", StringComparison.Ordinal) ||
               normalized.Contains("CONFIGURACAO ESPECIFICADA NAO EXISTE", StringComparison.Ordinal);
    }

    private static string NormalizePowercfgOutput(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return string.Empty;
        }

        var decomposed = output.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }
}
