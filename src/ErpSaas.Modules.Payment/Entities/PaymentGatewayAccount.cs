using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Payment.Entities;

[Auditable("Payment.PaymentGatewayAccount")]
public class PaymentGatewayAccount : TenantEntity
{
    public string GatewayCode { get; set; } = default!;                   // DDL PAYMENT_GATEWAY
    public string CredentialsJsonEncrypted { get; set; } = default!;
    public string? WebhookSecretEncrypted { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
}
