using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Messaging;
using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Seeds;

public sealed class NotificationTemplateSeeder(
    NotificationsDbContext db,
    ILogger<NotificationTemplateSeeder> logger) : IDataSeeder
{
    public int Order => 25;

    private static readonly (string Code, NotificationChannel Channel, string Subject, string Body)[] Templates =
    [
        (
            Constants.NotificationCodes.InvoiceFinalized,
            NotificationChannel.Sms,
            "Invoice {{InvoiceNumber}} - ShopEarth",
            "Dear {{CustomerName}}, your invoice {{InvoiceNumber}} for ₹{{GrandTotal}} has been finalized. Thank you!"
        ),
        (
            Constants.NotificationCodes.InvoiceCancelled,
            NotificationChannel.Sms,
            "Invoice {{InvoiceNumber}} Cancelled",
            "Dear {{CustomerName}}, invoice {{InvoiceNumber}} has been cancelled. Contact us for queries."
        ),
        (
            Constants.NotificationCodes.WalletCredited,
            NotificationChannel.Sms,
            "Wallet Credited - ShopEarth",
            "Dear {{CustomerName}}, ₹{{Amount}} credited to your wallet. New balance: ₹{{Balance}}. Receipt: {{ReceiptNumber}}"
        ),
        (
            Constants.NotificationCodes.WalletDebited,
            NotificationChannel.Sms,
            "Wallet Debited - ShopEarth",
            "Dear {{CustomerName}}, ₹{{Amount}} debited from your wallet. New balance: ₹{{Balance}}."
        ),
        (
            Constants.NotificationCodes.UserInvite,
            NotificationChannel.Email,
            "You've been invited to ShopEarth",
            "Hello {{DisplayName}}, you have been invited to join ShopEarth ERP. Click here to set up your account: {{InviteLink}}"
        ),
        (
            Constants.NotificationCodes.PasswordReset,
            NotificationChannel.Email,
            "Reset your ShopEarth password",
            "Hello {{DisplayName}}, click the link below to reset your password: {{ResetLink}} (valid for 30 minutes)"
        ),
        (
            Constants.NotificationCodes.LowStock,
            NotificationChannel.Sms,
            "Low Stock Alert - ShopEarth",
            "Alert: Product '{{ProductName}}' ({{ProductCode}}) is below minimum stock level. Current qty: {{CurrentQty}}."
        ),
        (
            Constants.NotificationCodes.ShiftClosed,
            NotificationChannel.Sms,
            "Shift Closed - ShopEarth",
            "Hi {{CashierName}}, your shift has been closed. Sales: ₹{{TotalSales}} | Cash variance: ₹{{CashVariance}}"
        ),
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var (code, channel, subject, body) in Templates)
            {
                if (!await db.NotificationTemplates.AnyAsync(
                        t => t.Code == code && t.Channel == channel, ct))
                {
                    db.NotificationTemplates.Add(new NotificationTemplate
                    {
                        Code = code,
                        Channel = channel,
                        SubjectTemplate = subject,
                        BodyTemplate = body,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeded notification template: {Code}/{Channel}", code, channel);
                }
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
