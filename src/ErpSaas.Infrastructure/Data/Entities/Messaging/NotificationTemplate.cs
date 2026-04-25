using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;

namespace ErpSaas.Infrastructure.Data.Entities.Messaging;

public sealed class NotificationTemplate
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
