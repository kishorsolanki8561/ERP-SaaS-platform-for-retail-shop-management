using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Log;
using ErpSaas.Shared.Services;

namespace ErpSaas.Infrastructure.Services;

public sealed class AuditLogger(LogDbContext db) : IAuditLogger
{
    public async Task LogAsync(AuditEvent ev, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            EventType = ev.EventType,
            EntityName = ev.EntityName,
            EntityId = ev.EntityId,
            OldValues = ev.OldValues,
            NewValues = ev.NewValues,
            UserId = ev.UserId,
            ShopId = ev.ShopId,
            CorrelationId = ev.CorrelationId,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
