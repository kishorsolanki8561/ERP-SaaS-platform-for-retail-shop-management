using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Messaging;
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
    IEmailProvider emailProvider,
    ISmsProvider smsProvider,
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
                item.Status = NotificationStatus.Sending;
                await db.SaveChangesAsync(ct);

                await DispatchAsync(item, ct);

                item.Status    = NotificationStatus.Sent;
                item.SentAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                item.AttemptCount++;
                item.ErrorMessage    = ex.Message[..Math.Min(ex.Message.Length, 1000)];
                item.NextRetryAtUtc  = DateTime.UtcNow.AddMinutes(Math.Pow(2, item.AttemptCount));
                item.Status          = item.AttemptCount >= MaxAttempts
                    ? NotificationStatus.Failed
                    : NotificationStatus.Pending;
                logger.LogWarning(ex, "Notification dispatch failed for queue item {Id} (attempt {N})",
                    item.Id, item.AttemptCount);
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private async Task DispatchAsync(NotificationQueue item, CancellationToken ct)
    {
        switch (item.Channel)
        {
            case NotificationChannel.Email:
                await emailProvider.SendAsync(item.Recipient, item.Subject, item.Body, ct);
                break;

            case NotificationChannel.Sms:
            case NotificationChannel.WhatsApp:
                await smsProvider.SendAsync(item.Recipient, item.Body, ct);
                break;

            case NotificationChannel.Push:
                logger.LogInformation(
                    "Push notification to {Recipient} — Firebase not yet configured, skipping",
                    item.Recipient);
                break;

            default:
                throw new NotSupportedException($"Channel {item.Channel} is not supported.");
        }
    }
}
