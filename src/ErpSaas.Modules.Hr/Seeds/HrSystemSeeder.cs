using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Sequence;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Hr.Seeds;

public sealed class HrSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<HrSystemSeeder> logger) : IDataSeeder
{
    public int Order => 90;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedMenuAsync(ct);
        await SeedDdlAsync(ct);
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
                ("HR.View",       "View employees, attendance, leave and payroll"),
                ("HR.Manage",     "Manage employees, leave types and approve requests"),
                ("HR.Attendance", "Record own attendance and submit leave requests"),
                ("HR.Payroll",    "Generate, approve and pay payroll"),
            };
            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code, Module = "HR", Label = label,
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
            logger.LogError(ex, "HrSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "hr", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "hr", Label = "HR",
                    Kind = MenuItemKind.Group, Icon = "pi pi-users",
                    SortOrder = 60, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("hr.employees",      "Employees",       "pi pi-id-card",    "/hr/employees",       "HR.View",    10),
                ("hr.attendance",     "Attendance",      "pi pi-calendar",   "/hr/attendance",      "HR.View",    20),
                ("hr.leave-requests", "Leave Requests",  "pi pi-calendar-times", "/hr/leave-requests", "HR.View", 30),
                ("hr.leave-types",    "Leave Types",     "pi pi-list",       "/hr/leave-types",     "HR.Manage",  40),
                ("hr.payroll",        "Payroll",         "pi pi-money-bill", "/hr/payroll",         "HR.Payroll", 50),
                ("hr.activities",     "Staff Activities","pi pi-history",    "/hr/activities",      "HR.View",    60),
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
            logger.LogError(ex, "HrSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedDdlAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            await EnsureDdl(Constants.DdlKeys.AttendanceStatus, "Attendance Status",
            [
                ("Present", "Present",   10),
                ("Absent",  "Absent",    20),
                ("HalfDay", "Half Day",  30),
                ("Leave",   "Leave",     40),
                ("Holiday", "Holiday",   50),
            ], ct);

            await EnsureDdl(Constants.DdlKeys.SalaryComponent, "Salary Component",
            [
                ("Basic",     "Basic Salary",    10),
                ("HRA",       "HRA",             20),
                ("DA",        "DA",              30),
                ("PF",        "PF (Deduction)",  40),
                ("Bonus",     "Bonus",           50),
                ("Deduction", "Other Deduction", 60),
            ], ct);

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "HrSystemSeeder.SeedDdlAsync failed");
            throw;
        }
    }

    private async Task EnsureDdl(string key, string label, (string code, string lbl, int sort)[] items, CancellationToken ct)
    {
        var catalog = await platformDb.DdlCatalogs
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Key == key, ct);

        if (catalog is null)
        {
            catalog = new DdlCatalog { Key = key, Label = label, IsActive = true };
            platformDb.DdlCatalogs.Add(catalog);
            await platformDb.SaveChangesAsync(ct);
        }

        foreach (var (code, lbl, sort) in items)
        {
            if (!catalog.Items.Any(i => i.Code == code))
            {
                catalog.Items.Add(new DdlItem { Code = code, Label = lbl, SortOrder = sort, IsActive = true });
                logger.LogInformation("Seeding DDL {Key}/{Code}", key, code);
            }
        }
    }

    private async Task SeedSequencesAsync(CancellationToken ct)
    {
        await using var tx = await tenantDb.Database.BeginTransactionAsync(ct);
        try
        {
            const long platformShopId = 0L;
            if (!await tenantDb.SequenceDefinitions
                    .IgnoreQueryFilters()
                    .AnyAsync(s => s.Code == Constants.SequenceCodes.Employee && s.ShopId == platformShopId, ct))
            {
                tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                {
                    ShopId = platformShopId,
                    Code = Constants.SequenceCodes.Employee,
                    Prefix = Constants.SequencePrefixes.Employee,
                    PadLength = 4, LastNumber = 0, CreatedAtUtc = DateTime.UtcNow,
                });
                logger.LogInformation("Seeding sequence: {Code}", Constants.SequenceCodes.Employee);
            }
            await tenantDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "HrSystemSeeder.SeedSequencesAsync failed");
            throw;
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            const string featureCode = "hr.payroll";
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
            logger.LogError(ex, "HrSystemSeeder.SeedFeaturesAsync failed");
            throw;
        }
    }
}
