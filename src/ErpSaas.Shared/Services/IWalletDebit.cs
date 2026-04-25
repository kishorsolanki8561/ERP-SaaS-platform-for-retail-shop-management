namespace ErpSaas.Shared.Services;

/// <summary>
/// Cross-module contract: Billing debits a customer wallet without depending on the Wallet module.
/// </summary>
public interface IWalletDebit
{
    Task<Result<bool>> DebitForInvoiceAsync(
        long customerId,
        long invoiceId,
        string invoiceNumber,
        decimal amount,
        CancellationToken ct = default);
}
