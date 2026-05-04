using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Seeds;

public sealed class LeadSystemSeeder(
    PlatformDbContext platformDb,
    ILogger<LeadSystemSeeder> logger) : IDataSeeder
{
    public int Order => 126;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedDdlAsync(ct);
        await SeedMenuAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Lead.View",      "Lead", "View leads list and details"),
                ("Lead.Edit",      "Lead", "Update lead status and notes"),
                ("Lead.Assign",    "Lead", "Assign leads to team members"),
                ("Lead.Convert",   "Lead", "Convert lead to a shop account"),
                ("Marketing.Edit", "Marketing", "Edit marketing page content"),
                ("Blog.Edit",      "Blog", "Create and edit blog posts"),
                ("Blog.Publish",   "Blog", "Publish blog posts"),
            };

            foreach (var (code, module, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = module, Label = label,
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
            logger.LogError(ex, "LeadSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedDdlAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var catalogs = new (string Key, string Label, (string Code, string Label)[] Items)[]
            {
                ("LEAD_SOURCE", "Lead Source",
                [
                    ("Website", "Website"), ("Referral", "Referral"),
                    ("Ad", "Advertisement"), ("Event", "Event"),
                ]),
                ("LEAD_STATUS", "Lead Status",
                [
                    ("New", "New"), ("Contacted", "Contacted"),
                    ("Qualified", "Qualified"), ("Converted", "Converted"), ("Lost", "Lost"),
                ]),
                ("VERTICAL", "Business Vertical",
                [
                    ("Electrical",   "Electrical"),
                    ("Electronics",  "Electronics"),
                    ("PowerTools",   "Power Tools"),
                    ("Medical",      "Medical / Pharma"),
                    ("Grocery",      "Grocery / FMCG"),
                    ("General",      "General Retail"),
                ]),
            };

            var existingKeys = await platformDb.DdlCatalogs.Select(c => c.Key).ToListAsync(ct);

            foreach (var (key, label, items) in catalogs)
            {
                if (existingKeys.Contains(key)) continue;

                var catalog = new DdlCatalog { Key = key, Label = label, IsActive = true };
                for (int i = 0; i < items.Length; i++)
                {
                    catalog.Items.Add(new DdlItem
                    {
                        Code = items[i].Code, Label = items[i].Label,
                        SortOrder = (i + 1) * 10, IsActive = true,
                    });
                }
                platformDb.DdlCatalogs.Add(catalog);
                logger.LogInformation("Seeding DDL catalog: {Key}", key);
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "LeadSystemSeeder.SeedDdlAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var platformGroup = await platformDb.MenuItems
                .FirstOrDefaultAsync(m => m.Code == "platform", ct);

            if (platformGroup is null)
            {
                await tx.CommitAsync(ct);
                return;
            }

            var menuItems = new[]
            {
                ("platform.leads",     "Leads",     "pi pi-users",     "/platform/leads",     "Lead.View",      30),
                ("platform.marketing", "Marketing", "pi pi-megaphone", "/platform/marketing", "Marketing.Edit", 40),
            };

            foreach (var (code, label, icon, route, perm, sort) in menuItems)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code, Label = label, Kind = MenuItemKind.Page,
                        Icon = icon, Route = route, ParentId = platformGroup.Id,
                        SortOrder = sort, RequiredPermission = perm,
                        IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Seeded menu item: {Code}", code);
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "LeadSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }
}
