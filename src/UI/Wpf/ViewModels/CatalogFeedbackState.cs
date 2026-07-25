namespace ApexTweaker.UI.Wpf.ViewModels;

/// <summary>
/// UI-local catalog feedback states. Not a shared OperationOutcome contract.
/// </summary>
internal enum CatalogFeedbackKind
{
    /// <summary>Initial / before a successful analyze with rows.</summary>
    Idle,

    /// <summary>Analyze finished with zero rule rows.</summary>
    Empty,

    /// <summary>Analyze succeeded but usage profile is unknown (conservative recommendations).</summary>
    Partial,

    /// <summary>Analyze threw or could not produce a plan.</summary>
    Error,

    /// <summary>Analyze succeeded with rows and known-enough context.</summary>
    Ready
}

internal static class CatalogFeedbackState
{
    public static CatalogFeedbackKind Resolve(
        int rowCount,
        bool analyzeFailed,
        bool usageUnknown)
    {
        if (analyzeFailed)
        {
            return CatalogFeedbackKind.Error;
        }

        if (rowCount <= 0)
        {
            return CatalogFeedbackKind.Empty;
        }

        if (usageUnknown)
        {
            return CatalogFeedbackKind.Partial;
        }

        return CatalogFeedbackKind.Ready;
    }

    public static string Describe(CatalogFeedbackKind kind) => kind switch
    {
        CatalogFeedbackKind.Idle => "Idle",
        CatalogFeedbackKind.Empty => "Empty",
        CatalogFeedbackKind.Partial => "Partial",
        CatalogFeedbackKind.Error => "Error",
        CatalogFeedbackKind.Ready => "Ready",
        _ => kind.ToString()
    };
}
