using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ApexTweaker.Services;

internal sealed class MasterRollbackService
{
    private readonly BackupService backupService;

    public MasterRollbackService()
        : this(new BackupService())
    {
    }

    public MasterRollbackService(BackupService backupService)
    {
        this.backupService = backupService;
    }

    public Task<IReadOnlyList<string>> ExecuteAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => backupService.RestoreAllPendingMutationSessions(progress, cancellationToken), cancellationToken);
    }
}
