using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Billing.Entities;

[Auditable("InvoicePayment")]
public class InvoicePayment : TenantEntity
{
    public long InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public PaymentMode Mode { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime PaidAtUtc { get; set; }
}
