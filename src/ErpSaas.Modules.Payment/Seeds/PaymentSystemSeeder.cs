using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Payment.Seeds;

public sealed class PaymentSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<PaymentSystemSeeder> logger) : IDataSeeder
{
    public int Order => 90;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Payment.View",       "View payment transactions and gateway accounts"),
                ("Payment.Initiate",   "Initiate an online payment link"),
                ("Payment.Manage",     "Confirm, fail, or cancel payment transactions"),
                ("Payment.Refund",     "Refund a completed payment"),
                ("Payment.Configure",  "Add or update payment gateway accounts"),
                ("Payment.Reconcile",  "Trigger and resolve payment reconciliation exceptions"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Payment", Label = label,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeding permission: {Code}", code);
                }
            }
            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "PaymentSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "payment", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "payment", Label = "Payments",
                    Kind = MenuItemKind.Group, Icon = "pi pi-credit-card",
                    SortOrder = 65, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("payment.transactions", "Transactions",       "pi pi-list",          "/payment/transactions",  "Payment.View",      10),
                ("payment.exceptions",   "Reconciliation",     "pi pi-exclamation-triangle", "/payment/exceptions", "Payment.Reconcile", 20),
                ("payment.gateways",     "Gateway Accounts",   "pi pi-link",          "/payment/gateways",      "Payment.Configure", 30),
            };

            foreach (var (code, label, icon, route, perm, sort) in pages)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code, Label = label, Kind = MenuItemKind.Page,
                        Icon = icon, Route = route, ParentId = group.Id,
                        SortOrder = sort, RequiredPermission = perm,
                        IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                    });
                }
            }
            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "PaymentSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

}
