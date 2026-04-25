using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;
using ErpSaas.Shared.Messages;
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
    private static int BatchSize   => Constants.Pagination.NotificationBatch;
    private static int MaxAttempts => Constants.Notifications.MaxAttempts;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var pending = await db.NotificationQueues
            .Where(q => q.Status == NotificationStatus.Pending
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
                item.Status = NotificationStatus.Sent;
                item.SentAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                item.AttemptCount++;
                item.ErrorMessage = ex.Message;
                item.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, item.AttemptCount));
                if (item.AttemptCount >= MaxAttempts)
                    item.Status = NotificationStatus.Failed;
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
