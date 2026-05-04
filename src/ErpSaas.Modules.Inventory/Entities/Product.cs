using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Inventory.Entities;

[Auditable("Inventory.ProductChanged")]
public class Product : TenantEntity
{
    [AuditField("Product Code")]
    public string ProductCode { get; set; } = "";

    [AuditField("Name")]
    public string Name { get; set; } = "";

    [AuditField("Description")]
    public string? Description { get; set; }

    [AuditField("Category")]
    public string CategoryCode { get; set; } = "";

    [AuditField("HSN/SAC Code")]
    public string? HsnSacCode { get; set; }

    [AuditField("GST Rate")]
    public decimal GstRate { get; set; } = 0m;

    [AuditField("Base Unit")]
    public string BaseUnitCode { get; set; } = "";

    [AuditField("Sale Price")]
    public decimal SalePrice { get; set; }

    [AuditField("Purchase Price")]
    public decimal PurchasePrice { get; set; }

    [AuditField("MRP")]
    public decimal? MrpPrice { get; set; }

    [AuditField("Min Stock Level")]
    public decimal MinStockLevel { get; set; } = 0m;

    [AuditField("Active")]
    public bool IsActive { get; set; } = true;

    [AuditField("Barcode")]
    public string? BarcodeEan { get; set; }

    public ICollection<ProductUnit> Units { get; set; } = [];
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
