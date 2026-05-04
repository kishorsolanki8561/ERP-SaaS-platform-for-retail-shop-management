namespace ErpSaas.Shared.Services;

/// <summary>
/// Cross-module contract: SalesReturns routes a refund through the Wallet module
/// without depending on it directly. Implemented by WalletRefundOrchestrator.
/// </summary>
public interface IWalletRefundOrchestrator
{
    /// <summary>
    /// Credits the wallet portion of a refund and records cash amount on the sales return row.
    /// Caller is responsible for committing the outer transaction.
    /// </summary>
    Task<Result<bool>> ProcessRefundAsync(
        long salesReturnId,
        long customerId,
        string customerName,
        string? customerPhone,
        string returnNumber,
        decimal refundToWallet,
        decimal refundToCash,
        CancellationToken ct = default);
}
