using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Messaging;

/// <summary>
/// Hangfire recurring job that drains the NotificationQueue, dispatching pending items.
/// Registered via RecurringJob.AddOrUpdate in AppInitializationExtensions.
/// </summary>
public sealed class NotificationDrainJob(
    NotificationsDbContext db,
    ILogger<NotificationDrainJob> logger)
{
    private const int BatchSize = 50;
    private const int MaxAttempts = 5;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var pending = await db.NotificationQueues
            .Where(q => q.Status == "Pending"
                        && q.AttemptCount < MaxAttempts
                        && q.NextRetryAtUtc <= DateTime.UtcNow)
            .OrderBy(q => q.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var item in pending)
        {
            try
            {
                await DispatchAsync(item, ct);
                item.Status = "Sent";
                item.SentAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                item.AttemptCount++;
                item.ErrorMessage = ex.Message;
                item.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, item.AttemptCount));
                if (item.AttemptCount >= MaxAttempts)
                    item.Status = "Failed";
                logger.LogWarning(ex, "Notification dispatch failed for queue item {Id}", item.Id);
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);
    }

    private static Task DispatchAsync(Data.Entities.Messaging.NotificationQueue item, CancellationToken ct)
    {
        // TODO Phase 3: wire real providers (SendGrid, Twilio, Firebase, WhatsApp Business API)
        // For Phase 0: stub — log only
        return Task.CompletedTask;
    }
}
