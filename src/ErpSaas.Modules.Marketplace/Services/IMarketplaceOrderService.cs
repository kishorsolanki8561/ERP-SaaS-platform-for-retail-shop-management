using ErpSaas.Modules.Marketplace.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Marketplace.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record MarketplaceOrderDto(
    long Id, long MarketplaceAccountId, string MarketplaceOrderId,
    DateTime OrderDate, string CustomerName, string? CustomerPhone,
    decimal OrderTotal, MarketplaceOrderStatus Status, long? GeneratedInvoiceId);

public record MarketplaceOrderListRequest(
    long? AccountId, MarketplaceOrderStatus? Status,
    DateTime? From, DateTime? To,
    int Page = 1, int PageSize = 20);

public record IngestOrderDto(
    long MarketplaceAccountId, string MarketplaceOrderId,
    DateTime OrderDate, string CustomerName, string? CustomerPhone,
    string ShippingAddressJson, decimal OrderTotal, string RawPayloadJson);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IMarketplaceOrderService
{
    Task<IReadOnlyList<MarketplaceOrderDto>> ListAsync(MarketplaceOrderListRequest request, CancellationToken ct = default);
    Task<Result<long>> IngestAsync(IngestOrderDto dto, CancellationToken ct = default);
    Task<Result<long>> ConvertToInvoiceAsync(long orderId, CancellationToken ct = default);
}
