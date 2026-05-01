using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.Account")]
public class Account : TenantEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public long AccountGroupId { get; set; }
    public decimal OpeningBalance { get; set; }
    public DebitCredit OpeningBalanceType { get; set; }
    public string? GstNumber { get; set; }
    public long? LinkedCustomerId { get; set; }
    public long? LinkedSupplierId { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    public AccountGroup AccountGroup { get; set; } = default!;
    public ICollection<VoucherEntry> VoucherEntries { get; set; } = [];
}
