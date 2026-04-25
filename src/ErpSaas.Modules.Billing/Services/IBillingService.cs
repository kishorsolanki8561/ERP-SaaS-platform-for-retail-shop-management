using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Billing.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record InvoiceListDto(
    long Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string CustomerName,
    InvoiceStatus Status,
    decimal GrandTotal);

public record InvoiceLineDto(
    long Id,
    long ProductId,
    string ProductName,
    string UnitCode,
    decimal Qty,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxableAmount,
    decimal GstRate,
    decimal LineTotal);

public record InvoiceDetailDto(
    long Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    long CustomerId,
    string CustomerName,
    InvoiceStatus Status,
    decimal SubTotal,
    decimal TotalDiscount,
    decimal TotalTaxAmount,
    decimal GrandTotal,
    IReadOnlyList<InvoiceLineDto> Lines);

public record CreateInvoiceDto(
    DateTime InvoiceDate,
    long CustomerId,
    long WarehouseId,
    long ShopId,
    string? Notes,
    string? CustomerName = null,
    string? CustomerPhone = null,
    long? ShiftId = null,
    long? BranchId = null);

public record AddInvoiceLineDto(
    long ProductId,
    long ProductUnitId,
    decimal QuantityInBilledUnit,
    decimal UnitPrice,
    decimal DiscountPercent);

public record PaymentAllocationDto(
    PaymentMode Mode,
    decimal Amount,
    string? ReferenceNumber = null,
    string? Notes = null);

public record PayInvoiceDto(
    IReadOnlyList<PaymentAllocationDto> Allocations,
    long? CustomerId = null);

public record SetPaymentTermsDto(
    string PaymentTerms,
    int DueDays);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IBillingService
{
    Task<PagedResult<InvoiceListDto>> ListInvoicesAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default);

    Task<InvoiceDetailDto?> GetInvoiceAsync(long id, CancellationToken ct = default);

    Task<Result<long>> CreateDraftInvoiceAsync(CreateInvoiceDto dto, CancellationToken ct = default);

    Task<Result<bool>> AddLineAsync(long invoiceId, AddInvoiceLineDto dto, CancellationToken ct = default);

    Task<Result<bool>> FinalizeInvoiceAsync(long id, CancellationToken ct = default);

    Task<Result<bool>> SetPaymentTermsAsync(long id, SetPaymentTermsDto dto, CancellationToken ct = default);

    Task<Result<bool>> PayInvoiceAsync(long id, PayInvoiceDto dto, CancellationToken ct = default);

    Task<Result<bool>> CancelInvoiceAsync(long id, string reason, CancellationToken ct = default);
}
