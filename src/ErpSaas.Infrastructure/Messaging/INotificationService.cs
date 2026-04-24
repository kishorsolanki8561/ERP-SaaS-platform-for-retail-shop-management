namespace ErpSaas.Infrastructure.Messaging;

public interface INotificationService
{
    Task EnqueueAsync(
        long shopId,
        string channel,
        string recipient,
        string templateCode,
        IDictionary<string, string> variables,
        string? correlationId = null,
        CancellationToken ct = default);
}
