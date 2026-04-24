namespace ErpSaas.Shared.Services;

public record AuditEvent(
    string EventType,
    string EntityName,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    long? UserId,
    long? ShopId,
    string? CorrelationId = null);

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AuditableAttribute(string eventType) : Attribute
{
    public string EventType { get; } = eventType;
}

public interface IAuditLogger
{
    Task LogAsync(AuditEvent ev, CancellationToken ct = default);
}
