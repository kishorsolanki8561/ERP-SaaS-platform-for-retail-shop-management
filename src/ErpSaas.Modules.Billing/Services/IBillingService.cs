using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Billing.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record InvoiceListDto(
    long Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string CustomerName,
    string Status,
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
    string Status,
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
    string? Notes);

public record AddInvoiceLineDto(
    long ProductId,
    long ProductUnitId,
    decimal QuantityInBilledUnit,
    decimal UnitPrice,
    decimal DiscountPercent);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IBillingService
{
    Task<IReadOnlyList<InvoiceListDto>> ListInvoicesAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default);

    Task<InvoiceDetailDto?> GetInvoiceAsync(long id, CancellationToken ct = default);

    Task<Result<long>> CreateDraftInvoiceAsync(CreateInvoiceDto dto, CancellationToken ct = default);

    Task<Result<bool>> AddLineAsync(long invoiceId, AddInvoiceLineDto dto, CancellationToken ct = default);

    Task<Result<bool>> FinalizeInvoiceAsync(long id, CancellationToken ct = default);

    Task<Result<bool>> CancelInvoiceAsync(long id, string reason, CancellationToken ct = default);
}
