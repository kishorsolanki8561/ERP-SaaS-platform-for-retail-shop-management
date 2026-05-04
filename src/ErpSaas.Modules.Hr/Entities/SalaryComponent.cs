using ErpSaas.Modules.Hr.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Hr.Entities;

public class SalaryComponent : TenantEntity
{
    public long EmployeeId { get; set; }
    public string ComponentCode { get; set; } = default!;
    public ComponentType Type { get; set; }
    public decimal Amount { get; set; }
    public bool IsRecurring { get; set; } = true;

    public Employee Employee { get; set; } = default!;
}
