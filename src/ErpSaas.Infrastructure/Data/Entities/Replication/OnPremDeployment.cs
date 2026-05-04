using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Infrastructure.Data.Entities.Replication;

[Auditable("OnPremDeployment")]
public class OnPremDeployment : TenantEntity
{
    public string DeploymentId { get; set; } = default!;
    public string ShopLocalEndpoint { get; set; } = string.Empty;
    public string PublicKey { get; set; } = default!;
    public DateTime InstalledAtUtc { get; set; }
    public DateTime LastReplicationAtUtc { get; set; }
    public ReplicationMode Mode { get; set; } = ReplicationMode.Bidirectional;
    public DateTime? LastFullReplicationAtUtc { get; set; }
    public string SoftwareVersion { get; set; } = default!;
    public OnPremDeploymentStatus Status { get; set; } = OnPremDeploymentStatus.Active;
}
