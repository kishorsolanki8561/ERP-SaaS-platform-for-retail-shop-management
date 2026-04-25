using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Metering;

public class UsageMeter : TenantEntity
{
    public string MeterCode { get; set; } = "";
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public long Used { get; set; }
    public long Quota { get; set; }
    public bool HardCapEnforced { get; set; }
    public long OverageCount { get; set; }
    public decimal OverageChargeRate { get; set; }
}
