namespace ErpSaas.Infrastructure.Data.Entities.Messaging;

public sealed class NotificationTemplate
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty; // Email | Sms | Push | WhatsApp
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
