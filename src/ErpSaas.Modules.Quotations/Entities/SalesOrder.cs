using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Entities;

[Auditable("Quotations.SalesOrder")]
public class SalesOrder : TenantEntity
{
    public string SoNumber { get; set; } = default!;
    public long? QuotationId { get; set; }
    public long CustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = default!;
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Pending;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Notes { get; set; }
    public long? BranchId { get; set; }
    public ICollection<SalesOrderLine> Lines { get; set; } = [];
    public ICollection<DeliveryChallan> DeliveryChallans { get; set; } = [];
}
