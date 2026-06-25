using System;
using System.Collections.Generic;
using System.IO;

namespace Renomeador.Services;

internal sealed class ValorantLocator
{
    private const string ValorantExeRelativePath = @"VALORANT\live\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe";

    public string? FindExecutable()
    {
        foreach (var path in GetCandidatePaths())
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        yield return Path.Combine(@"C:\Riot Games", ValorantExeRelativePath);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Riot Games", ValorantExeRelativePath);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Riot Games", ValorantExeRelativePath);
    }
}
