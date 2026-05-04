using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Hr.Entities;

public class LeaveBalance : TenantEntity
{
    public long EmployeeId { get; set; }
    public long LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal EntitledDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal CarryForwardDays { get; set; }

    public Employee Employee { get; set; } = default!;
    public LeaveType LeaveType { get; set; } = default!;
}
