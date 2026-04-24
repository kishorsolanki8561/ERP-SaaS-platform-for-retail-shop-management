namespace ErpSaas.Infrastructure.Data.Entities.Log;

public class AuditLog
{
    public long Id { get; set; }
    public string EventType { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public long? UserId { get; set; }
    public long? ShopId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
