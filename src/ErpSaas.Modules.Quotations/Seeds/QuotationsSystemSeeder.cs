using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Quotations.Seeds;

public sealed class QuotationsSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<QuotationsSystemSeeder> logger) : IDataSeeder
{
    public int Order => 85;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
        await SeedSequencesAsync(ct);
        await SeedFeaturesAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Quotation.View",   "View quotations, sales orders and delivery challans"),
                ("Quotation.Create", "Create quotations, sales orders and delivery challans"),
                ("Quotation.Send",   "Send quotations to customers and dispatch delivery challans"),
                ("Quotation.Revise", "Revise an existing quotation"),
                ("Quotation.Accept", "Accept a quotation on behalf of the customer"),
                ("Quotation.Convert","Convert a quotation to a sales order"),
                ("Quotation.Delete", "Reject or cancel quotations, sales orders and delivery challans"),
            };
            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "Quotations", Label = label,
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
            logger.LogError(ex, "QuotationsSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "quotations", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "quotations", Label = "Quotations",
                    Kind = MenuItemKind.Group, Icon = "pi pi-file-edit",
                    SortOrder = 55, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("quotations.list",     "Quotations",        "pi pi-file-edit",   "/quotations",                  "Quotation.View", 10),
                ("quotations.so",       "Sales Orders",      "pi pi-shopping-bag","/quotations/sales-orders",     "Quotation.View", 20),
                ("quotations.dc",       "Delivery Challans", "pi pi-truck",       "/quotations/delivery-challans","Quotation.View", 30),
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
            logger.LogError(ex, "QuotationsSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedSequencesAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const long platformShopId = 0L;
            var sequences = new[]
            {
                (Constants.SequenceCodes.Quotation, Constants.SequencePrefixes.Quotation),
                (Constants.SequenceCodes.SalesOrder, Constants.SequencePrefixes.SalesOrder),
                (Constants.SequenceCodes.DeliveryChallan, Constants.SequencePrefixes.DeliveryChallan),
            };
            foreach (var (code, prefix) in sequences)
            {
                if (!await tenantDb.SequenceDefinitions
                        .IgnoreQueryFilters()
                        .AnyAsync(s => s.Code == code && s.ShopId == platformShopId, ct))
                {
                    tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                    {
                        ShopId = platformShopId,
                        Code = code,
                        Prefix = prefix,
                        PadLength = 6, LastNumber = 0, CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeding sequence: {Code}", code);
                }
            }
            await tenantDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "QuotationsSystemSeeder.SeedSequencesAsync failed");
            throw;
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            const string featureCode = "wholesale.quotations";
            if (!await platformDb.SubscriptionPlanFeatures.AnyAsync(f => f.FeatureCode == featureCode, ct))
            {
                var plans = await platformDb.SubscriptionPlans
                    .Where(p => p.Code == Constants.Plans.Growth || p.Code == Constants.Plans.Enterprise)
                    .ToListAsync(ct);

                foreach (var plan in plans)
                {
                    platformDb.SubscriptionPlanFeatures.Add(new SubscriptionPlanFeature
                    {
                        PlanId = plan.Id,
                        FeatureCode = featureCode,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                }
                logger.LogInformation("Seeding feature flag: {Code}", featureCode);
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "QuotationsSystemSeeder.SeedFeaturesAsync failed");
            throw;
        }
    }
}
