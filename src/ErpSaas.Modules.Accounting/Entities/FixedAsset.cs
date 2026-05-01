using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.FixedAsset")]
public class FixedAsset : TenantEntity
{
    public string AssetCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string CategoryCode { get; set; } = default!;
    public DateTime PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public long? SupplierId { get; set; }
    public long? PurchaseInvoiceFileId { get; set; }
    public DepreciationMethod Method { get; set; }
    public decimal UsefulLifeYears { get; set; }
    public decimal SalvageValue { get; set; }
    public decimal RateOfDepreciation { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue { get; set; }
    public FixedAssetStatus Status { get; set; } = FixedAssetStatus.InUse;
    public DateTime? DisposalDate { get; set; }
    public decimal? DisposalValue { get; set; }
    public string? LocationNotes { get; set; }
    public long? AssignedToEmployeeId { get; set; }

    public ICollection<DepreciationEntry> DepreciationEntries { get; set; } = [];
}
