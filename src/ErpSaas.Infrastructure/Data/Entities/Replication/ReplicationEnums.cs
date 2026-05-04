namespace ErpSaas.Infrastructure.Data.Entities.Replication;

public enum ReplicationMode
{
    Disabled,
    CloudToOnPrem,
    OnPremToCloud,
    Bidirectional,
}

public enum OnPremDeploymentStatus
{
    Active,
    Paused,
    Degraded,
    Unreachable,
}

public enum ReplicationDirection
{
    Upload,
    Download,
}

public enum ReplicationStatus
{
    Running,
    Success,
    PartialFailure,
    Failed,
}

public enum ConflictResolutionStrategy
{
    LastWriteWins,
    FieldLevelMerge,
    ManualResolution,
}

public enum ConflictResolutionOutcome
{
    Pending,
    AutoResolved,
    ManuallyResolved,
    Rejected,
}
