using ErpSaas.Modules.Wallet.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Wallet.Entities;

[Auditable("Wallet.Transaction")]
public class WalletTransaction : TenantEntity
{
    public long CustomerId { get; set; }

    public string CustomerNameSnapshot { get; set; } = "";

    public WalletTransactionType TransactionType { get; set; }

    /// <summary>Always positive; direction is in TransactionType.</summary>
    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }

    public decimal BalanceAfter { get; set; }

    /// <summary>DDL key: WALLET_REFERENCE_TYPE. Values: Invoice, Manual, Refund.</summary>
    public string ReferenceType { get; set; } = "Manual";

    public long? ReferenceId { get; set; }

    public string? ReferenceNumber { get; set; }

    /// <summary>Populated for Credit transactions via ISequenceService.</summary>
    public string? ReceiptNumber { get; set; }

    public string? Notes { get; set; }
}
