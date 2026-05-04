using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Replication;

public class ConflictArchive : TenantEntity
{
    public string DeploymentId { get; set; } = default!;
    public string EntityName { get; set; } = default!;
    public long EntityId { get; set; }
    public string CloudSnapshotJson { get; set; } = default!;
    public string OnPremSnapshotJson { get; set; } = default!;
    public ConflictResolutionStrategy Strategy { get; set; }
    public ConflictResolutionOutcome Outcome { get; set; } = ConflictResolutionOutcome.Pending;
    public string? ResolutionNote { get; set; }
    public long? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
