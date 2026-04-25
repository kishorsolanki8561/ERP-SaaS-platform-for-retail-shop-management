using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;

namespace ErpSaas.Infrastructure.Messaging;

public interface INotificationService
{
    Task EnqueueAsync(
        long shopId,
        NotificationChannel channel,
        string recipient,
        string templateCode,
        IDictionary<string, string> variables,
        string? correlationId = null,
        CancellationToken ct = default);
}
