using ErpSaas.Modules.Verticals.Medical.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Verticals.Medical.Entities;

public class DrugBatch : TenantEntity
{
    public string BatchNumber { get; set; } = default!;
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public string? GenericName { get; set; }
    public string? Manufacturer { get; set; }
    public DrugSchedule Schedule { get; set; } = DrugSchedule.None;
    public DateTime ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsScheduleH => Schedule is DrugSchedule.H or DrugSchedule.H1;
    public bool IsNarcotic => Schedule is DrugSchedule.X;
    public string? SupplierNameSnapshot { get; set; }
    public long? PurchaseBillId { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PrescriptionRecord> PrescriptionRecords { get; set; } = [];
}
