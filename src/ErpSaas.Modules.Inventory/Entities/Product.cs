using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Inventory.Entities;

/// <summary>Product master for the shop. ProductCode is unique per shop.</summary>
[Auditable("Inventory.ProductChanged")]
public class Product : TenantEntity
{
    /// <summary>System-generated unique code per shop (e.g. PRD00001).</summary>
    public string ProductCode { get; set; } = "";

    public string Name { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>DDL catalog key: PRODUCT_CATEGORY (Electrical, Electronics, etc.).</summary>
    public string CategoryCode { get; set; } = "";

    /// <summary>8-digit HSN code or 6-digit SAC code for GST compliance.</summary>
    public string? HsnSacCode { get; set; }

    /// <summary>GST rate in percent (0, 5, 12, 18, 28).</summary>
    public decimal GstRate { get; set; } = 0m;

    /// <summary>Base unit code, e.g. "PCS". Must match the ProductUnit with IsBaseUnit = true.</summary>
    public string BaseUnitCode { get; set; } = "";

    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal? MrpPrice { get; set; }

    /// <summary>Threshold for low-stock alerts (in base units).</summary>
    public decimal MinStockLevel { get; set; } = 0m;

    public bool IsActive { get; set; } = true;

    /// <summary>EAN-13 or EAN-8 barcode for POS scanning.</summary>
    public string? BarcodeEan { get; set; }

    // Navigations
    public ICollection<ProductUnit> Units { get; set; } = [];
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
