using ErpSaas.Modules.Sync.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Sync.Entities;

[Auditable("DeviceRegistration")]
public class DeviceRegistration : TenantEntity
{
    public string DeviceId { get; set; } = default!;
    public long BranchId { get; set; }
    public long AssignedUserId { get; set; }
    public DeviceType Type { get; set; }
    public string PlatformInfo { get; set; } = default!;
    public string AppVersion { get; set; } = default!;
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
