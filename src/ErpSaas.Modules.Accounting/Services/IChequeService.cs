using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ReceiveChequeDto(
    ChequeDirection Direction,
    string ChequeNumber,
    DateTime ChequeDate,
    decimal Amount,
    long BankAccountId,
    string DrawerName,
    string DrawerBankName,
    long? RelatedInvoiceId = null,
    long? RelatedSupplierBillId = null);

public record ChequeListDto(
    long Id,
    ChequeDirection Direction,
    string ChequeNumber,
    DateTime ChequeDate,
    decimal Amount,
    string DrawerName,
    ChequeStatus Status,
    DateTime? DepositedDate,
    DateTime? ClearedDate);

public record BounceChequeDtoRequest(
    string BounceReasonCode,
    decimal BounceCharges,
    long BankChargesAccountId);

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IChequeService
{
    Task<PagedResult<ChequeListDto>> ListChequesAsync(ChequeStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<Result<long>> ReceiveChequeAsync(ReceiveChequeDto dto, CancellationToken ct = default);
    Task<Result<bool>> DepositChequeAsync(long id, DateTime depositedDate, CancellationToken ct = default);
    Task<Result<bool>> ClearChequeAsync(long id, DateTime clearedDate, CancellationToken ct = default);
    Task<Result<bool>> BounceChequeAsync(long id, BounceChequeDtoRequest dto, CancellationToken ct = default);
    Task<Result<bool>> CancelChequeAsync(long id, CancellationToken ct = default);
    Task MarkStaleDatedAsync(CancellationToken ct = default);
}
