using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Accounting.Entities;

public class BankStatementLine : BaseEntity
{
    public long BankStatementId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = default!;
    public string? Reference { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal DebitAmount { get; set; }
    public BankStatementLineStatus MatchStatus { get; set; } = BankStatementLineStatus.Unmatched;
    public long? MatchedVoucherId { get; set; }
    public long? MatchedByUserId { get; set; }
    public DateTime? MatchedAtUtc { get; set; }
    public BankStatement BankStatement { get; set; } = default!;
}
