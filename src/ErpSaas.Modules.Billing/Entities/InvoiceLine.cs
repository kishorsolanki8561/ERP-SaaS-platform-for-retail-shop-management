using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Billing.Entities;

public class InvoiceLine : TenantEntity
{
    public long InvoiceId { get; set; }

    public long ProductId { get; set; }

    /// <summary>Snapshot of product name at time of invoicing.</summary>
    public string ProductNameSnapshot { get; set; } = "";

    /// <summary>Snapshot of product SKU/code at time of invoicing.</summary>
    public string ProductCodeSnapshot { get; set; } = "";

    public string? HsnSacCodeSnapshot { get; set; }

    // ── Unit snapshot fields (CLAUDE.md §3.7) ────────────────────────────────

    public long ProductUnitId { get; set; }

    /// <summary>Unit code snapshot (e.g. "PCS", "BOX") at time of invoicing.</summary>
    public string UnitCodeSnapshot { get; set; } = "";

    /// <summary>How many base units make up one billed unit at time of invoicing.</summary>
    public decimal ConversionFactorSnapshot { get; set; } = 1m;

    /// <summary>Quantity entered by the user in the billed unit.</summary>
    public decimal QuantityInBilledUnit { get; set; }

    /// <summary>QuantityInBilledUnit * ConversionFactorSnapshot — always stored for stock math.</summary>
    public decimal QuantityInBaseUnit { get; set; }

    // ── Pricing ───────────────────────────────────────────────────────────────

    /// <summary>Unit price per billed unit.</summary>
    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; } = 0m;

    public decimal DiscountAmount { get; set; } = 0m;

    public decimal TaxableAmount { get; set; }

    public decimal GstRate { get; set; }

    public decimal CgstAmount { get; set; }

    public decimal SgstAmount { get; set; }

    public decimal IgstAmount { get; set; }

    public decimal LineTotal { get; set; }

    public int SortOrder { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public Invoice Invoice { get; set; } = null!;
}
