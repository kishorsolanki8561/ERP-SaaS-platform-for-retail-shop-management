using ErpSaas.Modules.Wallet.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Wallet.Entities;

[Auditable("Wallet.TopUp")]
public class WalletTopUp : TenantEntity
{
    public long CustomerId { get; set; }

    public string CustomerNameSnapshot { get; set; } = "";

    public decimal Amount { get; set; }

    /// <summary>DDL key PAYMENT_GATEWAY (e.g. RAZORPAY) or PAYMENT_MODE (CASH for cash top-up).</summary>
    public string PaymentModeCode { get; set; } = "";

    public long? PaymentGatewayTransactionId { get; set; }

    public WalletTopUpStatus Status { get; set; } = WalletTopUpStatus.Pending;

    /// <summary>Issued from WALLET_TOP_UP sequence after success.</summary>
    public string? ReceiptNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime InitiatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? FailureReason { get; set; }

    /// <summary>Set to the WalletTransaction.Id created when the top-up is completed.</summary>
    public long? WalletTransactionId { get; set; }
}
