using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Metering;

public class UsageEvent : TenantEntity
{
    public string MeterCode { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
    public long Delta { get; set; }
    public string? SourceEntityType { get; set; }
    public long? SourceEntityId { get; set; }
    public long? TriggeredByUserId { get; set; }
}
