using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Inventory.Entities;

/// <summary>Unit of measure for a product (e.g. PCS, BOX, DOZEN). One unit per product is the base unit.</summary>
public class ProductUnit : TenantEntity
{
    public long ProductId { get; set; }

    /// <summary>Short code, e.g. "PCS", "BOX", "DOZEN".</summary>
    public string UnitCode { get; set; } = "";

    /// <summary>Human-readable label, e.g. "Pieces".</summary>
    public string UnitLabel { get; set; } = "";

    /// <summary>How many base units equal one of this unit. Base unit always = 1.0.</summary>
    public decimal ConversionFactor { get; set; } = 1m;

    public bool IsBaseUnit { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Product? Product { get; set; }
}
