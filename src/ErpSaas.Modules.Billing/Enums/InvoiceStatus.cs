namespace ErpSaas.Modules.Billing.Enums;

/// <summary>
/// State machine for an Invoice. Stored as string in DB via EF HasConversion.
/// Transitions: Draft → Finalized | Draft → Cancelled.
/// </summary>
public enum InvoiceStatus
{
    Draft,
    Finalized,
    Cancelled,
}
