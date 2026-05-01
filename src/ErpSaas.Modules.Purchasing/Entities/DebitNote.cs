using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.DebitNote")]
public class DebitNote : TenantEntity
{
    public string DebitNoteNumber { get; set; } = default!;
    public long SupplierId { get; set; }
    public string SupplierNameSnapshot { get; set; } = default!;
    public long? PurchaseReturnId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DebitNoteStatus Status { get; set; } = DebitNoteStatus.Draft;
    public decimal Amount { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = default!;
}
