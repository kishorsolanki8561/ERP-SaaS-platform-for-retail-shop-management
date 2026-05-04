namespace ErpSaas.Modules.Payment.Enums;

public enum PaymentGatewayStatus
{
    Initiated,
    Pending,
    Success,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded,
}
