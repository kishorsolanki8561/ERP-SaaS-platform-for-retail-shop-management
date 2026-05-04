using System.Text.Json.Serialization;

namespace ErpSaas.Modules.Payment.Connectors.Razorpay;

internal sealed class RazorpayWebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public RazorpayPayloadWrapper? Payload { get; set; }
}

internal sealed class RazorpayPayloadWrapper
{
    [JsonPropertyName("payment")]
    public RazorpayPaymentEntity? Payment { get; set; }

    [JsonPropertyName("refund")]
    public RazorpayRefundEntity? Refund { get; set; }
}

internal sealed class RazorpayPaymentEntity
{
    [JsonPropertyName("entity")]
    public RazorpayPaymentItem? Entity { get; set; }
}

internal sealed class RazorpayPaymentItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; }  // in paise

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}

internal sealed class RazorpayRefundEntity
{
    [JsonPropertyName("entity")]
    public RazorpayRefundItem? Entity { get; set; }
}

internal sealed class RazorpayRefundItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; }  // in paise
}
