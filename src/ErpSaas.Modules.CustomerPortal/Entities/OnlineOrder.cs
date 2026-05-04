using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.CustomerPortal.Entities;

[Auditable("CustomerPortal.OnlineOrder")]
public sealed class OnlineOrder : TenantEntity
{
    public string OrderNumber { get; set; } = default!;
    public long PlatformCustomerId { get; set; }
    public long TenantCustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = default!;
    public string CustomerPhoneSnapshot { get; set; } = default!;
    public OnlineOrderStatus Status { get; set; } = OnlineOrderStatus.Pending;
    public string? RejectionReason { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountApplied { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal GrandTotal { get; set; }
    public string? DeliveryAddressJson { get; set; }
    public string DeliveryPreference { get; set; } = "Pickup";
    public long? InvoiceId { get; set; }
    public long? DeliveryChallanId { get; set; }
    public long? PaymentTransactionId { get; set; }
    public string? Notes { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }

    public ICollection<OnlineOrderLine> Lines { get; set; } = [];
}
