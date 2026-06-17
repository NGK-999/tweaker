using System;
using Renomeador.Models;

namespace Renomeador.Services;

internal interface ISystemMutationCommand
{
    string Name { get; }

    string SuccessMessage { get; }

    string FailurePrefix { get; }

    void Validate();

    void Snapshot(BackupService backupService, MutationSession session);

    void Execute();

    void Verify();
}

internal sealed class SystemMutationCommand : ISystemMutationCommand
{
    private readonly Action<BackupService, MutationSession> snapshotAction;
    private readonly Action executeAction;
    private readonly Action verifyAction;
    private readonly Action? validateAction;

    public SystemMutationCommand(
        string name,
        Action<BackupService, MutationSession> snapshotAction,
        Action executeAction,
        Action verifyAction,
        string successMessage,
        string failurePrefix,
        Action? validateAction = null)
    {
        Name = name;
        this.snapshotAction = snapshotAction;
        this.executeAction = executeAction;
        this.verifyAction = verifyAction;
        SuccessMessage = successMessage;
        FailurePrefix = failurePrefix;
        this.validateAction = validateAction;
    }

    public string Name { get; }

    public string SuccessMessage { get; }

    public string FailurePrefix { get; }

    public void Validate()
    {
        validateAction?.Invoke();
    }

    public void Snapshot(BackupService backupService, MutationSession session)
    {
        snapshotAction(backupService, session);
    }

    public void Execute()
    {
        executeAction();
    }

    public void Verify()
    {
        verifyAction();
    }
}
