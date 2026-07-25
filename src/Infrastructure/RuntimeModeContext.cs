using System;

namespace ApexTweaker.Infrastructure;

internal enum RuntimeMode
{
    Unknown = 0,
    Standard = 1,
    Demo = 2
}

internal readonly record struct RuntimeMutationDecision(
    bool Allowed,
    RuntimeMode Mode,
    string Reason)
{
    public static RuntimeMutationDecision Allow(RuntimeMode mode) => new(true, mode, string.Empty);

    public static RuntimeMutationDecision Block(RuntimeMode mode, string reason) => new(false, mode, reason);
}

internal static class RuntimeModeContext
{
    private static readonly object Sync = new();
    private static RuntimeMode current = RuntimeMode.Unknown;

    public static RuntimeMode Current
    {
        get
        {
            lock (Sync)
            {
                return current;
            }
        }
    }

    public static void Configure(RuntimeMode mode)
    {
        lock (Sync)
        {
            current = mode;
        }
    }

    public static void ResetForTests()
    {
        Configure(RuntimeMode.Unknown);
    }

    public static RuntimeMutationDecision EvaluateMutation(string subject)
    {
        var mode = Current;
        return mode switch
        {
            RuntimeMode.Standard => RuntimeMutationDecision.Allow(mode),
            RuntimeMode.Demo => RuntimeMutationDecision.Block(
                mode,
                $"Mutacao bloqueada em modo demo na fronteira central: {subject}."),
            _ => RuntimeMutationDecision.Block(
                mode,
                $"Mutacao bloqueada por RuntimeMode incerto na fronteira central: {subject}.")
        };
    }
}
