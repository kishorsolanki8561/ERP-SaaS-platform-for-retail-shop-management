using ErpSaas.Modules.Inventory.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Inventory.Entities;

/// <summary>
/// Immutable ledger row for every stock in/out event.
/// Quantity math is always in base unit; QuantityInBaseUnit = QuantityInBilledUnit * ConversionFactorSnapshot.
/// </summary>
public class StockMovement : TenantEntity
{
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }

    public StockMovementType MovementType { get; set; }

    /// <summary>Originating document type, e.g. "Invoice", "PurchaseOrder".</summary>
    public string? ReferenceType { get; set; }

    /// <summary>FK to the originating document (Invoice.Id, PurchaseOrder.Id, etc.).</summary>
    public long? ReferenceId { get; set; }

    // ── Unit snapshot fields (§3.7 compliance) ───────────────────────────────

    /// <summary>FK to the ProductUnit used for this movement.</summary>
    public long ProductUnitId { get; set; }

    /// <summary>Snapshot of the unit code at the time of movement (e.g. "BOX").</summary>
    public string UnitCodeSnapshot { get; set; } = "";

    /// <summary>Snapshot of the conversion factor at the time of movement.</summary>
    public decimal ConversionFactorSnapshot { get; set; } = 1m;

    /// <summary>Quantity expressed in the billed/transacted unit.</summary>
    public decimal QuantityInBilledUnit { get; set; }

    /// <summary>Canonical quantity in base unit = QuantityInBilledUnit * ConversionFactorSnapshot.</summary>
    public decimal QuantityInBaseUnit { get; set; }

    // ─────────────────────────────────────────────────────────────────────────

    public string? Remarks { get; set; }

    public DateTime MovedAtUtc { get; set; }

    // Navigations
    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
}
