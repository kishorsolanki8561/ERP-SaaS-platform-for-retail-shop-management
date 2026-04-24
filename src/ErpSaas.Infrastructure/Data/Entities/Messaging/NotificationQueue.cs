namespace ErpSaas.Infrastructure.Data.Entities.Messaging;

public sealed class NotificationQueue
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending | Sent | Failed
    public int AttemptCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? TemplateCode { get; set; }
    public string? CorrelationId { get; set; }
}
