using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ApexTweaker.Infrastructure;

internal enum CommandIntent
{
    ReadOnly = 0,
    Mutation = 1,
    Unknown = 2
}

internal readonly record struct TrustedCommandResolution(
    bool IsTrusted,
    string? CanonicalPath,
    string ExecutableName);

/// <summary>
/// Fail-closed command intent classifier for Demo/Unknown runtime modes.
/// Bare names are resolved to System32/SysWOW64 before trust and classification.
/// </summary>
internal static class CommandClassifier
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private static readonly HashSet<string> KnownSystemTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "powercfg", "powercfg.exe",
        "bcdedit", "bcdedit.exe",
        "sc", "sc.exe",
        "reg", "reg.exe",
        "netsh", "netsh.exe",
        "dism", "dism.exe",
        "sfc", "sfc.exe",
        "defrag", "defrag.exe",
        "gpupdate", "gpupdate.exe",
        "schtasks", "schtasks.exe",
        "cmd", "cmd.exe",
        "powershell", "powershell.exe",
        "pwsh", "pwsh.exe"
    };

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

    /// <summary>
    /// Resolve to a trusted System32/SysWOW64 binary. Bare names never rely on PATH/cwd.
    /// </summary>
    public static TrustedCommandResolution Resolve(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new TrustedCommandResolution(false, null, string.Empty);
        }

        var trimmed = fileName.Trim().Trim('"');
        var executable = NormalizeExecutable(trimmed);
        if (executable.Length == 0)
        {
            return new TrustedCommandResolution(false, null, string.Empty);
        }

        var hasDirectory = trimmed.Contains('\\', StringComparison.Ordinal) ||
                           trimmed.Contains('/', StringComparison.Ordinal) ||
                           trimmed.Contains(':', StringComparison.Ordinal);

        if (!hasDirectory)
        {
            if (!KnownSystemTools.Contains(executable))
            {
                return new TrustedCommandResolution(false, null, executable);
            }

            var canonical = TryMapBareNameToSystemBinary(executable);
            return canonical is null
                ? new TrustedCommandResolution(false, null, executable)
                : new TrustedCommandResolution(true, canonical, executable);
        }

        try
        {
            var fullPath = Path.GetFullPath(trimmed);
            if (!IsUnderTrustedSystemDirectory(fullPath))
            {
                return new TrustedCommandResolution(false, null, executable);
            }

            var file = Path.GetFileName(fullPath);
            if (!FileNameMatchesExecutable(file, executable))
            {
                return new TrustedCommandResolution(false, null, executable);
            }

            if (!KnownSystemTools.Contains(executable) && !KnownSystemTools.Contains(file))
            {
                return new TrustedCommandResolution(false, null, executable);
            }

            return new TrustedCommandResolution(true, fullPath, executable);
        }
        catch
        {
            return new TrustedCommandResolution(false, null, executable);
        }
    }

    public static CommandIntent Classify(string fileName, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return CommandIntent.Unknown;
        }

        var resolution = Resolve(fileName);
        var executable = resolution.ExecutableName.Length > 0
            ? resolution.ExecutableName
            : NormalizeExecutable(fileName);
        var args = NormalizeArguments(arguments);

        if (executable is "cmd" or "cmd.exe" or "powershell" or "powershell.exe" or "pwsh" or "pwsh.exe")
        {
            // Shells are always mutation even when resolved under System32.
            return CommandIntent.Mutation;
        }

        if (!resolution.IsTrusted || string.IsNullOrWhiteSpace(resolution.CanonicalPath))
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

    private static string? TryMapBareNameToSystemBinary(string executable)
    {
        var exeFile = executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? executable
            : executable + ".exe";

        foreach (var directory in GetTrustedSystemDirectories())
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, exeFile));
            if (File.Exists(candidate) && IsUnderTrustedSystemDirectory(candidate))
            {
                return candidate;
            }
        }

        // Fail closed if the official binary is missing — do not fall back to PATH/cwd.
        return null;
    }

    private static IEnumerable<string> GetTrustedSystemDirectories()
    {
        var system = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.System));
        yield return system;

        var systemX86 = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86));
        if (!systemX86.Equals(system, StringComparison.OrdinalIgnoreCase))
        {
            yield return systemX86;
        }

        var windows = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        yield return Path.Combine(windows, "System32");
        yield return Path.Combine(windows, "SysWOW64");
    }

    private static bool IsUnderTrustedSystemDirectory(string fullPath)
    {
        foreach (var directory in GetTrustedSystemDirectories())
        {
            if (IsUnderDirectory(fullPath, directory))
            {
                return true;
            }
        }

        return false;
    }

    private static bool FileNameMatchesExecutable(string file, string normalizedExecutable)
    {
        return file.Equals(normalizedExecutable, StringComparison.OrdinalIgnoreCase) ||
               file.Equals(normalizedExecutable + ".exe", StringComparison.OrdinalIgnoreCase) ||
               (normalizedExecutable.EndsWith(".exe", StringComparison.Ordinal) &&
                file.Equals(normalizedExecutable, StringComparison.OrdinalIgnoreCase));
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
