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
/// Only an explicit allowlist of read-only operations returns <see cref="CommandIntent.ReadOnly"/>.
/// When in doubt, returns <see cref="CommandIntent.Unknown"/> (blocked outside Standard).
/// </summary>
internal static class CommandClassifier
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static CommandIntent Classify(string fileName, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return CommandIntent.Unknown;
        }

        var executable = NormalizeExecutable(fileName);
        var args = NormalizeArguments(arguments);

        // Shells can wrap arbitrary mutation — never treat as confirmed read-only.
        if (executable is "cmd" or "cmd.exe" or "powershell" or "powershell.exe" or "pwsh" or "pwsh.exe")
        {
            return CommandIntent.Mutation;
        }

        return executable switch
        {
            "powercfg" or "powercfg.exe" => ClassifyPowerCfg(args),
            "bcdedit" or "bcdedit.exe" => ClassifyBcdEdit(args),
            "sc" or "sc.exe" => ClassifySc(args),
            "reg" or "reg.exe" => ClassifyReg(args),
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

        var collapsed = Whitespace.Replace(arguments.Trim(), " ");
        return collapsed.ToLowerInvariant();
    }

    private static CommandIntent ClassifyPowerCfg(string args)
    {
        if (StartsWithToken(args, "/list") ||
            StartsWithToken(args, "/query") ||
            StartsWithToken(args, "/aliases") ||
            StartsWithToken(args, "/getactivescheme"))
        {
            return CommandIntent.ReadOnly;
        }

        if (args.StartsWith("/set", StringComparison.Ordinal) ||
            StartsWithToken(args, "/change") ||
            StartsWithToken(args, "/hibernate") ||
            StartsWithToken(args, "/energy"))
        {
            return CommandIntent.Mutation;
        }

        return CommandIntent.Unknown;
    }

    private static CommandIntent ClassifyBcdEdit(string args)
    {
        if (StartsWithToken(args, "/enum"))
        {
            return CommandIntent.ReadOnly;
        }

        if (StartsWithToken(args, "/set") ||
            StartsWithToken(args, "/delete") ||
            StartsWithToken(args, "/deletevalue") ||
            StartsWithToken(args, "/create") ||
            StartsWithToken(args, "/import") ||
            StartsWithToken(args, "/export"))
        {
            return CommandIntent.Mutation;
        }

        return CommandIntent.Unknown;
    }

    private static CommandIntent ClassifySc(string args)
    {
        if (StartsWithToken(args, "query") ||
            StartsWithToken(args, "queryex") ||
            StartsWithToken(args, "qc"))
        {
            return CommandIntent.ReadOnly;
        }

        if (StartsWithToken(args, "config") ||
            StartsWithToken(args, "start") ||
            StartsWithToken(args, "stop") ||
            StartsWithToken(args, "create") ||
            StartsWithToken(args, "delete") ||
            StartsWithToken(args, "failure"))
        {
            return CommandIntent.Mutation;
        }

        return CommandIntent.Unknown;
    }

    private static CommandIntent ClassifyReg(string args)
    {
        if (StartsWithToken(args, "query"))
        {
            return CommandIntent.ReadOnly;
        }

        if (StartsWithToken(args, "add") ||
            StartsWithToken(args, "delete") ||
            StartsWithToken(args, "copy") ||
            StartsWithToken(args, "import") ||
            StartsWithToken(args, "restore") ||
            StartsWithToken(args, "load") ||
            StartsWithToken(args, "unload") ||
            StartsWithToken(args, "save"))
        {
            return CommandIntent.Mutation;
        }

        return CommandIntent.Unknown;
    }

    private static CommandIntent ClassifyNetsh(string args)
    {
        if (StartsWithToken(args, "show") || args.Contains(" show ", StringComparison.Ordinal))
        {
            return CommandIntent.ReadOnly;
        }

        if (StartsWithToken(args, "set") ||
            StartsWithToken(args, "add") ||
            StartsWithToken(args, "delete") ||
            StartsWithToken(args, "reset"))
        {
            return CommandIntent.Mutation;
        }

        return CommandIntent.Unknown;
    }

    private static CommandIntent ClassifyDism(string args)
    {
        if (args.Contains("/get-features", StringComparison.Ordinal) ||
            args.Contains("/get-packages", StringComparison.Ordinal) ||
            args.Contains("/get-providers", StringComparison.Ordinal) ||
            args.Contains("/checkhealth", StringComparison.Ordinal) ||
            args.Contains("/get-currentedition", StringComparison.Ordinal))
        {
            return CommandIntent.ReadOnly;
        }

        if (args.Contains("/enable-feature", StringComparison.Ordinal) ||
            args.Contains("/disable-feature", StringComparison.Ordinal) ||
            args.Contains("/add-package", StringComparison.Ordinal) ||
            args.Contains("/remove-package", StringComparison.Ordinal) ||
            args.Contains("/apply-image", StringComparison.Ordinal))
        {
            return CommandIntent.Mutation;
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
}
