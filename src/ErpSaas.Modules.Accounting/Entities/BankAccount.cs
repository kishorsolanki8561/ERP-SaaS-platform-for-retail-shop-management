using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.BankAccount")]
public class BankAccount : TenantEntity
{
    public long AccountId { get; set; }
    public string BankName { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;
    public string IfscCode { get; set; } = default!;
    public string BranchName { get; set; } = default!;
    public string AccountHolderName { get; set; } = default!;
    public bool IsActive { get; set; }

    public Account Account { get; set; } = default!;
}
