using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Hr.Entities;

public class LeaveType : TenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int DefaultAnnualQuota { get; set; }
    public bool IsPaid { get; set; }
    public bool IsCarryForward { get; set; }
    public bool IsActive { get; set; } = true;
}
