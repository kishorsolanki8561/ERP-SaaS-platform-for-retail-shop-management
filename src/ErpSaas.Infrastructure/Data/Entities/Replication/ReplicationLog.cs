using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Replication;

public class ReplicationLog : TenantEntity
{
    public string DeploymentId { get; set; } = default!;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public ReplicationDirection Direction { get; set; }
    public int RowsTransferred { get; set; }
    public int RowsConflicted { get; set; }
    public int RowsFailed { get; set; }
    public ReplicationStatus Status { get; set; } = ReplicationStatus.Running;
    public string? ErrorSummary { get; set; }
    public long PayloadBytes { get; set; }
}
