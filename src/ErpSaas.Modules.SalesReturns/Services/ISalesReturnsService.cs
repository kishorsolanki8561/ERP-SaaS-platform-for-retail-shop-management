using ErpSaas.Modules.SalesReturns.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.SalesReturns.Services;

public record SalesReturnLineDto(long InvoiceLineId, long ProductId, long ProductUnitId, decimal Quantity);

public record CreateSalesReturnDto(
    long InvoiceId, DateTime ReturnDate, RefundMethod RefundMethod,
    string? Reason, IReadOnlyList<SalesReturnLineDto> Lines);

public record ApproveSalesReturnDto(long SalesReturnId);

public record IssueCreditNoteDto(long CustomerId, decimal Amount, string? Notes, DateTime? ExpiryDate);

public record ApplyCreditNoteDto(long CreditNoteId, long InvoiceId, decimal AmountToApply);

public interface ISalesReturnsService
{
    Task<Result<long>> CreateSalesReturnAsync(CreateSalesReturnDto dto, CancellationToken ct = default);
    Task<Result<bool>> ApproveSalesReturnAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> CancelSalesReturnAsync(long id, CancellationToken ct = default);

    Task<Result<long>> IssueCreditNoteAsync(IssueCreditNoteDto dto, CancellationToken ct = default);
    Task<Result<bool>> ApplyCreditNoteAsync(ApplyCreditNoteDto dto, CancellationToken ct = default);
    Task<Result<bool>> CancelCreditNoteAsync(long id, CancellationToken ct = default);
}
