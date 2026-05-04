using ErpSaas.Modules.Marketplace.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Marketplace.Entities;

[Auditable("Marketplace.Order")]
public class MarketplaceOrder : TenantEntity
{
    public long MarketplaceAccountId { get; set; }
    public string MarketplaceOrderId { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? CustomerPhone { get; set; }
    public string ShippingAddressJson { get; set; } = default!;
    public decimal OrderTotal { get; set; }
    public MarketplaceOrderStatus Status { get; set; } = MarketplaceOrderStatus.New;
    public long? GeneratedInvoiceId { get; set; }
    public string RawPayloadJson { get; set; } = default!;

    public MarketplaceAccount Account { get; set; } = default!;
}
