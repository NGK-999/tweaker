using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ApexTweaker.Infrastructure;

internal enum CommandIntent
{
    ReadOnly = 0,
    Mutation = 1,
    Unknown = 2
}

/// <summary>
/// Fail-closed command intent classifier for Demo/Unknown runtime modes.
/// Only an explicit allowlist of read-only operations on trusted binaries returns ReadOnly.
/// Mixed read+mutation tokens, untrusted paths, and unknowns are blocked outside Standard.
/// </summary>
internal static class CommandClassifier
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private static readonly string[] PowerCfgReadTokens = ["/list", "/query", "/aliases", "/getactivescheme"];
    private static readonly string[] PowerCfgMutationPrefixes = ["/set", "/change", "/hibernate", "/energy"];

    private static readonly string[] BcdReadTokens = ["/enum"];
    private static readonly string[] BcdMutationTokens = ["/set", "/delete", "/deletevalue", "/create", "/import", "/export"];

    private static readonly string[] ScReadTokens = ["query", "queryex", "qc"];
    private static readonly string[] ScMutationTokens = ["config", "start", "stop", "create", "delete", "failure"];

    private static readonly string[] RegReadTokens = ["query"];
    private static readonly string[] RegMutationTokens = ["add", "delete", "copy", "import", "restore", "load", "unload", "save"];

    private static readonly string[] NetshMutationTokens = ["set", "add", "delete", "reset"];

    private static readonly string[] DismReadFragments =
    [
        "/get-features", "/get-packages", "/get-providers", "/checkhealth", "/get-currentedition"
    ];

    private static readonly string[] DismMutationFragments =
    [
        "/enable-feature", "/disable-feature", "/add-package", "/remove-package", "/apply-image"
    ];

    public static CommandIntent Classify(string fileName, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return CommandIntent.Unknown;
        }

        var executable = NormalizeExecutable(fileName);
        var args = NormalizeArguments(arguments);

        if (executable is "cmd" or "cmd.exe" or "powershell" or "powershell.exe" or "pwsh" or "pwsh.exe")
        {
            return CommandIntent.Mutation;
        }

        // Path present but not a trusted System32/SysWOW64 binary → never ReadOnly.
        if (!IsTrustedSystemExecutable(fileName, executable))
        {
            return CommandIntent.Unknown;
        }

        return executable switch
        {
            "powercfg" or "powercfg.exe" => ClassifyByTokens(args, PowerCfgReadTokens, PowerCfgMutationPrefixes, mutationIsPrefix: true),
            "bcdedit" or "bcdedit.exe" => ClassifyByTokens(args, BcdReadTokens, BcdMutationTokens, mutationIsPrefix: false),
            "sc" or "sc.exe" => ClassifyByTokens(args, ScReadTokens, ScMutationTokens, mutationIsPrefix: false),
            "reg" or "reg.exe" => ClassifyByTokens(args, RegReadTokens, RegMutationTokens, mutationIsPrefix: false),
            "netsh" or "netsh.exe" => ClassifyNetsh(args),
            "dism" or "dism.exe" => ClassifyDism(args),
            "sfc" or "sfc.exe" => CommandIntent.Mutation,
            "defrag" or "defrag.exe" => CommandIntent.Mutation,
            "gpupdate" or "gpupdate.exe" => CommandIntent.Mutation,
            "schtasks" or "schtasks.exe" => CommandIntent.Mutation,
            _ => CommandIntent.Unknown
        };
    }

    internal static string NormalizeExecutable(string fileName)
    {
        var trimmed = fileName.Trim().Trim('"');
        try
        {
            return Path.GetFileName(trimmed).ToLowerInvariant();
        }
        catch
        {
            return trimmed.ToLowerInvariant();
        }
    }

    internal static string NormalizeArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return string.Empty;
        }

        return Whitespace.Replace(arguments.Trim(), " ").ToLowerInvariant();
    }

    /// <summary>
    /// Bare tool names (PATH) are accepted. Absolute/relative paths must resolve under System or SystemX86.
    /// </summary>
    internal static bool IsTrustedSystemExecutable(string fileName, string normalizedFileName)
    {
        var trimmed = fileName.Trim().Trim('"');
        if (trimmed.Length == 0)
        {
            return false;
        }

        var hasDirectory = trimmed.Contains('\\', StringComparison.Ordinal) ||
                           trimmed.Contains('/', StringComparison.Ordinal) ||
                           trimmed.Contains(':', StringComparison.Ordinal);
        if (!hasDirectory)
        {
            return true;
        }

        try
        {
            var fullPath = Path.GetFullPath(trimmed);
            var file = Path.GetFileName(fullPath);
            if (!file.Equals(normalizedFileName, StringComparison.OrdinalIgnoreCase) &&
                !file.Equals(normalizedFileName + ".exe", StringComparison.OrdinalIgnoreCase) &&
                !(normalizedFileName.EndsWith(".exe", StringComparison.Ordinal) &&
                  file.Equals(normalizedFileName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var system = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.System));
            var systemX86 = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86));
            var windows = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows));

            return IsUnderDirectory(fullPath, system) ||
                   IsUnderDirectory(fullPath, systemX86) ||
                   IsUnderDirectory(fullPath, Path.Combine(windows, "System32")) ||
                   IsUnderDirectory(fullPath, Path.Combine(windows, "SysWOW64"));
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUnderDirectory(string fullPath, string directory)
    {
        var prefix = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static CommandIntent ClassifyByTokens(
        string args,
        string[] readTokens,
        string[] mutationTokens,
        bool mutationIsPrefix)
    {
        var hasRead = false;
        foreach (var token in readTokens)
        {
            if (StartsWithToken(args, token))
            {
                hasRead = true;
                break;
            }
        }

        var hasMutation = false;
        foreach (var token in mutationTokens)
        {
            if (mutationIsPrefix)
            {
                // Prefix may appear after a read token (mixed → Unknown).
                if (args.StartsWith(token, StringComparison.Ordinal) ||
                    args.Contains(" " + token, StringComparison.Ordinal))
                {
                    hasMutation = true;
                    break;
                }
            }
            else if (StartsWithToken(args, token) || ContainsToken(args, token))
            {
                hasMutation = true;
                break;
            }
        }

        if (hasMutation && hasRead)
        {
            return CommandIntent.Unknown;
        }

        if (hasMutation)
        {
            return CommandIntent.Mutation;
        }

        if (hasRead)
        {
            return CommandIntent.ReadOnly;
        }

        return CommandIntent.Unknown;
    }

    private static CommandIntent ClassifyNetsh(string args)
    {
        var hasShow = StartsWithToken(args, "show") || ContainsToken(args, "show");
        var hasMutation = false;
        foreach (var token in NetshMutationTokens)
        {
            if (StartsWithToken(args, token) || ContainsToken(args, token))
            {
                hasMutation = true;
                break;
            }
        }

        if (hasMutation && hasShow)
        {
            return CommandIntent.Unknown;
        }

        if (hasMutation)
        {
            return CommandIntent.Mutation;
        }

        if (hasShow)
        {
            return CommandIntent.ReadOnly;
        }

        return CommandIntent.Unknown;
    }

    private static CommandIntent ClassifyDism(string args)
    {
        var hasRead = false;
        foreach (var fragment in DismReadFragments)
        {
            if (args.Contains(fragment, StringComparison.Ordinal))
            {
                hasRead = true;
                break;
            }
        }

        var hasMutation = false;
        foreach (var fragment in DismMutationFragments)
        {
            if (args.Contains(fragment, StringComparison.Ordinal))
            {
                hasMutation = true;
                break;
            }
        }

        if (hasMutation && hasRead)
        {
            return CommandIntent.Unknown;
        }

        if (hasMutation)
        {
            return CommandIntent.Mutation;
        }

        if (hasRead)
        {
            return CommandIntent.ReadOnly;
        }

        return CommandIntent.Unknown;
    }

    private static bool StartsWithToken(string args, string token)
    {
        if (string.IsNullOrEmpty(args))
        {
            return false;
        }

        if (args.Equals(token, StringComparison.Ordinal))
        {
            return true;
        }

        return args.StartsWith(token + " ", StringComparison.Ordinal);
    }

    private static bool ContainsToken(string args, string token)
    {
        return args.Equals(token, StringComparison.Ordinal) ||
               args.StartsWith(token + " ", StringComparison.Ordinal) ||
               args.EndsWith(" " + token, StringComparison.Ordinal) ||
               args.Contains(" " + token + " ", StringComparison.Ordinal);
    }
}
