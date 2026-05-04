using ErpSaas.Modules.CustomerPortal.Entities;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.CustomerPortal.Services;

public interface IOnlineOrderService
{
    Task<PagedResult<OnlineOrderSummaryDto>> ListOrdersAsync(int page, int pageSize, OnlineOrderStatus? status, CancellationToken ct = default);
    Task<Result<OnlineOrderDetailDto?>> GetOrderAsync(long id, CancellationToken ct = default);
    Task<Result<long>> CreateOrderAsync(CreateOnlineOrderDto dto, long platformCustomerId, CancellationToken ct = default);
    Task<Result<bool>> AcceptOrderAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> RejectOrderAsync(long id, string reason, CancellationToken ct = default);
    Task<Result<bool>> MarkDispatchedAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> MarkDeliveredAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> CancelOrderAsync(long id, CancellationToken ct = default);
}

public record OnlineOrderSummaryDto(long Id, string OrderNumber, long PlatformCustomerId, string CustomerName, OnlineOrderStatus Status, decimal GrandTotal, DateTime CreatedAtUtc);
public record OnlineOrderDetailDto(long Id, string OrderNumber, string CustomerName, string CustomerPhone, OnlineOrderStatus Status, string? RejectionReason, string DeliveryPreference, string? DeliveryAddressJson, decimal SubTotal, decimal DiscountApplied, decimal ShippingCost, decimal GrandTotal, IReadOnlyList<OnlineOrderLineDto> Lines, DateTime CreatedAtUtc);
public record OnlineOrderLineDto(long ProductId, string ProductName, string UnitCode, decimal Qty, decimal UnitPrice, decimal LineTotal);
public record CreateOnlineOrderDto(long TenantCustomerId, string DeliveryPreference, string? DeliveryAddressJson, string? Notes, IReadOnlyList<CreateOnlineOrderLineDto> Lines);
public record CreateOnlineOrderLineDto(long ProductId, long? ProductUnitId, decimal Quantity);
