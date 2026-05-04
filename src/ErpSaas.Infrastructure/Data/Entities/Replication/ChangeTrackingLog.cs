using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Replication;

public class ChangeTrackingLog : TenantEntity
{
    public string EntityName { get; set; } = default!;
    public long EntityId { get; set; }
    public string Operation { get; set; } = default!;
    public string PatchJson { get; set; } = default!;
    public long VersionNumber { get; set; }
    public string? OriginDeploymentId { get; set; }
}
