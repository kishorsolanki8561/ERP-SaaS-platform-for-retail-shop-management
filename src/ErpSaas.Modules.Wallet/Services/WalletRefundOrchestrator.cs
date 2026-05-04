using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Wallet.Services;

/// <summary>
/// Implements <see cref="IWalletRefundOrchestrator"/>. Called by SalesReturnsService
/// after approval to credit the wallet portion of the refund split.
/// </summary>
public sealed class WalletRefundOrchestrator(
    TenantDbContext db,
    IErrorLogger errorLogger,
    IWalletService walletService)
    : BaseService<TenantDbContext>(db, errorLogger), IWalletRefundOrchestrator
{
    public async Task<Result<bool>> ProcessRefundAsync(
        long salesReturnId,
        long customerId,
        string customerName,
        string? customerPhone,
        string returnNumber,
        decimal refundToWallet,
        decimal refundToCash,
        CancellationToken ct = default)
    {
        return await ExecuteAsync("Wallet.Refund.Process", async () =>
        {
            if (refundToWallet < 0 || refundToCash < 0)
                return Result<bool>.Failure(Errors.Wallet.RefundSplitMismatch);

            if (refundToWallet > 0)
            {
                var creditResult = await walletService.CreditAsync(new WalletCreditDto(
                    customerId,
                    customerName,
                    refundToWallet,
                    "REFUND",
                    salesReturnId,
                    returnNumber,
                    $"Refund for sales return {returnNumber}",
                    customerPhone), ct);

                if (!creditResult.IsSuccess)
                    return Result<bool>.Failure(creditResult.Errors.FirstOrDefault() ?? "WALLET_ERR");
            }

            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }
}
