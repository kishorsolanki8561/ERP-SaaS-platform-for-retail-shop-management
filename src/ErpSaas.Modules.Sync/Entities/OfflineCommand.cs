using ErpSaas.Modules.Sync.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Sync.Entities;

public class OfflineCommand : TenantEntity
{
    public Guid ClientCommandId { get; set; }
    public string DeviceId { get; set; } = default!;
    public string CommandType { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime ClientTimestampUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public OfflineCommandStatus Status { get; set; } = OfflineCommandStatus.Received;
    public string? RejectionReason { get; set; }
    public string? WarningNote { get; set; }
    public long? ResultingEntityId { get; set; }
}
