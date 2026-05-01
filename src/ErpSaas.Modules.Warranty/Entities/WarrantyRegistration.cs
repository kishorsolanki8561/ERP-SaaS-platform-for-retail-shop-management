using ErpSaas.Modules.Warranty.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Warranty.Entities;

[Auditable("Warranty.WarrantyRegistration")]
public class WarrantyRegistration : TenantEntity
{
    public long InvoiceId { get; set; }
    public long InvoiceLineId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public long CustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = default!;
    public string SerialNumber { get; set; } = default!;
    public DateTime PurchaseDate { get; set; }
    public DateTime WarrantyStartDate { get; set; }
    public DateTime WarrantyEndDate { get; set; }
    public int WarrantyMonths { get; set; }
    public WarrantyType Type { get; set; } = WarrantyType.Warranty;
    public string StatusCode { get; set; } = "Active";
    public string? TermsSnapshot { get; set; }
    public long? BranchId { get; set; }

    public ICollection<WarrantyClaim> Claims { get; set; } = [];
}
