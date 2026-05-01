namespace ErpSaas.Shared.Services;

/// <summary>
/// Cross-module contract: Billing/Wallet/Shift post accounting vouchers
/// without depending on the Accounting module directly.
/// </summary>
public interface IAutoVoucherService
{
    Task<Result<long>> PostSaleVoucherAsync(
        long shopId,
        long invoiceId,
        string invoiceNumber,
        decimal saleAmount,
        decimal taxAmount,
        CancellationToken ct = default);

    Task<Result<long>> PostPaymentReceivedVoucherAsync(
        long shopId,
        long invoiceId,
        string invoiceNumber,
        decimal amount,
        string paymentMode,
        CancellationToken ct = default);

    Task<Result<long>> PostExpenseVoucherAsync(
        long shopId,
        long expenseId,
        long expenseAccountId,
        long cashAccountId,
        decimal amount,
        string narration,
        CancellationToken ct = default);

    Task<Result<long>> PostShiftVarianceVoucherAsync(
        long shopId,
        long shiftId,
        decimal variance,
        CancellationToken ct = default);

    Task<Result<long>> PostPurchaseBillVoucherAsync(
        long shopId,
        long billId,
        string billNumber,
        decimal totalAmount,
        CancellationToken ct = default);

    Task<Result<long>> PostSalesReturnVoucherAsync(
        long shopId,
        long salesReturnId,
        string returnNumber,
        decimal totalRefundAmount,
        CancellationToken ct = default);
}
