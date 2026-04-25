namespace ErpSaas.Modules.Billing.Enums;

/// <summary>Stored as string in DB via EF HasConversion.</summary>
public enum PaymentMode
{
    Cash,
    Card,
    Upi,
    Wallet,
    CreditNote,
}
