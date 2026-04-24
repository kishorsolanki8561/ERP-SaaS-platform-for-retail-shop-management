namespace ErpSaas.Infrastructure.Data.Entities.Log;

public sealed class SlowQueryLog
{
    public long Id { get; set; }
    public string Sql { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public long? ShopId { get; set; }
    public long? UserId { get; set; }
    public string? CallerName { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
