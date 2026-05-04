using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Entities;

[Auditable("Quotations.SalesOrder")]
public class SalesOrder : TenantEntity
{
    [AuditField("SO Number")]
    public string SoNumber { get; set; } = default!;

    public long? QuotationId { get; set; }
    public long CustomerId { get; set; }

    [AuditField("Customer Name")]
    public string CustomerNameSnapshot { get; set; } = default!;

    [AuditField("Status")]
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Pending;

    [AuditField("Order Date")]
    public DateTime OrderDate { get; set; }

    [AuditField("Expected Delivery Date")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [AuditField("Sub Total")]
    public decimal SubTotal { get; set; }

    [AuditField("Total Discount")]
    public decimal TotalDiscount { get; set; }

    [AuditField("Total Tax")]
    public decimal TotalTax { get; set; }

    [AuditField("Grand Total")]
    public decimal GrandTotal { get; set; }

    [AuditField("Shipping Address")]
    public string? ShippingAddress { get; set; }

    [AuditField("Notes")]
    public string? Notes { get; set; }

    public long? BranchId { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = [];
    public ICollection<DeliveryChallan> DeliveryChallans { get; set; } = [];
}
