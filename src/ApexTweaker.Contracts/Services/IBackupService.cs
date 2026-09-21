using System;
using System.Collections.Generic;
using System.Threading;

namespace ApexTweaker.Contracts.Services;

/// <summary>
/// Operações de backup e restauração que podem ser compostas pelo host sem
/// conhecer o formato dos snapshots persistidos.
/// </summary>
internal interface IBackupService
{
    string BackupDirectory { get; }

    IReadOnlyList<string> CreateBackup();

    IReadOnlyList<string> RestoreLatestBackup();

    IReadOnlyList<string> RestoreLatestMutationSession();

    IReadOnlyList<string> RestoreAllPendingMutationSessions(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    string CreateDiagnosticReport(IEnumerable<string> lines);
}
