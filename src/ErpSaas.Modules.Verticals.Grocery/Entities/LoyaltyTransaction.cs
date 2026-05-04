using ErpSaas.Modules.Verticals.Grocery.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Verticals.Grocery.Entities;

public class LoyaltyTransaction : TenantEntity
{
    public long CustomerId { get; set; }
    public long LoyaltyProgramId { get; set; }
    public LoyaltyTransactionType TransactionType { get; set; }
    public decimal Points { get; set; }
    public decimal BalanceAfter { get; set; }
    public long? InvoiceId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
