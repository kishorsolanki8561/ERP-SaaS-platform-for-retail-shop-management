using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.BankStatement")]
public class BankStatement : TenantEntity
{
    public long BankAccountId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? CompletedByUserId { get; set; }
    public string? Notes { get; set; }
    public BankAccount BankAccount { get; set; } = default!;
    public ICollection<BankStatementLine> Lines { get; set; } = [];
}
