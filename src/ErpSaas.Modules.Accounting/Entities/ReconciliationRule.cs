using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.ReconciliationRule")]
public class ReconciliationRule : TenantEntity
{
    public string Name { get; set; } = default!;
    public string PatternContains { get; set; } = default!;
    public long AccountId { get; set; }
    public VoucherType VoucherType { get; set; }
    public bool IsActive { get; set; } = true;
    public Account Account { get; set; } = default!;
}
