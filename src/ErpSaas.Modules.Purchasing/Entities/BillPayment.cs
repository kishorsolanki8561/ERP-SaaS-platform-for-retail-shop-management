using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.BillPayment")]
public class BillPayment : TenantEntity
{
    public long BillId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentModeCode { get; set; } = default!;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public long? VoucherId { get; set; }

    public Bill Bill { get; set; } = default!;
}
