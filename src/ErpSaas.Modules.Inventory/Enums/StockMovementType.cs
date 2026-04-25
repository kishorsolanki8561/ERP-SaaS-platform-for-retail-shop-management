namespace ErpSaas.Modules.Inventory.Enums;

/// <summary>
/// Classifies a stock ledger row. Stored as string in DB via EF HasConversion.
/// Inbound: Purchase, Return, Adjustment (positive), Opening.
/// Outbound: Sale, Transfer, Adjustment (negative handled by sign of quantity).
/// </summary>
public enum StockMovementType
{
    Purchase,
    Sale,
    Adjustment,
    Transfer,
    Return,
    Opening,
}
