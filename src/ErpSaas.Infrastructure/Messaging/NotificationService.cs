using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Messaging;
using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Messaging;

public sealed class NotificationService(NotificationsDbContext db) : INotificationService
{
    public async Task EnqueueAsync(
        long shopId,
        NotificationChannel channel,
        string recipient,
        string templateCode,
        IDictionary<string, string> variables,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var template = await db.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Code == templateCode && t.Channel == channel && t.IsActive, ct);

        var subject = template != null ? Render(template.SubjectTemplate, variables) : templateCode;
        var body = template != null
            ? Render(template.BodyTemplate, variables)
            : string.Join("; ", variables.Select(kv => $"{kv.Key}={kv.Value}"));

        db.NotificationQueues.Add(new NotificationQueue
        {
            ShopId = shopId,
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            TemplateCode = templateCode,
            CorrelationId = correlationId,
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    private static string Render(string template, IDictionary<string, string> vars)
    {
        foreach (var (key, value) in vars)
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        return template;
    }
}
