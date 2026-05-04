namespace ErpSaas.Shared.Services;

public record AuditEvent(
    string EventType,
    string EntityName,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    long? UserId,
    long? ShopId,
    string? CorrelationId = null,
    string? ParentEntityName = null,
    string? ParentEntityId = null);

[AttributeUsage(AttributeTargets.Class)]
public sealed class AuditableAttribute(string eventType) : Attribute
{
    public string EventType { get; } = eventType;
    public string? ParentEntityType { get; init; }
    public string? ParentIdProperty { get; init; }
}

/// <summary>
/// Marks an entity property as visible in audit log diffs.
/// Only properties with this attribute appear in the before/after change display.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditFieldAttribute(string displayName) : Attribute
{
    public string DisplayName { get; } = displayName;
}

public interface IAuditLogger
{
    Task LogAsync(AuditEvent ev, CancellationToken ct = default);
}
