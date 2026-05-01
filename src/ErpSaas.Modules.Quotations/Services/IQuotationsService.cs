using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record QuotationLineInput(
    long ProductId, string ProductNameSnapshot,
    long ProductUnitId, string UnitCodeSnapshot, decimal ConversionFactor,
    decimal QuantityInBilledUnit, decimal UnitPrice, decimal DiscountAmount,
    decimal GstRate);

public record CreateQuotationDto(
    long CustomerId, string CustomerNameSnapshot,
    DateTime ValidUntil, string? Notes,
    IReadOnlyList<QuotationLineInput> Lines);

public record QuotationSummaryDto(
    long Id, string QuotationNumber, long CustomerId, string CustomerNameSnapshot,
    QuotationStatus Status, DateTime QuotationDate, DateTime ValidUntil, decimal GrandTotal);

public record SalesOrderLineInput(
    long ProductId, string ProductNameSnapshot,
    long ProductUnitId, string UnitCodeSnapshot, decimal ConversionFactor,
    decimal QuantityInBilledUnit, decimal UnitPrice, decimal DiscountAmount,
    decimal GstRate);

public record CreateSalesOrderDto(
    long CustomerId, string CustomerNameSnapshot,
    long? QuotationId,
    DateTime? ExpectedDeliveryDate,
    string? ShippingAddress, string? Notes,
    IReadOnlyList<SalesOrderLineInput> Lines);

public record SalesOrderSummaryDto(
    long Id, string SoNumber, long CustomerId, string CustomerNameSnapshot,
    SalesOrderStatus Status, DateTime OrderDate, decimal GrandTotal);

public record CreateDeliveryChallanDto(
    long SalesOrderId, DateTime ChallanDate,
    string? DeliveryAddress, string? TransporterName, string? VehicleNumber, string? Notes);

public record DeliveryChallanSummaryDto(
    long Id, string DcNumber, long SalesOrderId, DeliveryChallanStatus Status, DateTime ChallanDate);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IQuotationsService
{
    Task<IReadOnlyList<QuotationSummaryDto>> ListQuotationsAsync(CancellationToken ct = default);
    Task<Result<long>> CreateQuotationAsync(CreateQuotationDto dto, CancellationToken ct = default);
    Task<Result<bool>> SendQuotationAsync(long id, CancellationToken ct = default);
    Task<Result<long>> ConvertQuotationToSalesOrderAsync(long quotationId, CancellationToken ct = default);
    Task<Result<bool>> RejectQuotationAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<SalesOrderSummaryDto>> ListSalesOrdersAsync(CancellationToken ct = default);
    Task<Result<long>> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken ct = default);
    Task<Result<bool>> CancelSalesOrderAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<DeliveryChallanSummaryDto>> ListDeliveryChallansAsync(CancellationToken ct = default);
    Task<Result<long>> CreateDeliveryChallanAsync(CreateDeliveryChallanDto dto, CancellationToken ct = default);
    Task<Result<bool>> DispatchDeliveryChallanAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> MarkDeliveryChallanDeliveredAsync(long id, CancellationToken ct = default);
}
