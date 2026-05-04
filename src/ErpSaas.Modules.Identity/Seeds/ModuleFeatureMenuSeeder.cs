using ErpSaas.Infrastructure.Data;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Seeds;

/// <summary>
/// Runs after all module seeders (Order=200) and stamps RequiredFeature
/// on every top-level menu group so that plan-based feature gating
/// controls menu visibility end-to-end.
/// </summary>
public sealed class ModuleFeatureMenuSeeder(
    PlatformDbContext db,
    ILogger<ModuleFeatureMenuSeeder> logger) : IDataSeeder
{
    public int Order => 200;

    // Maps menu group code → Module.X feature code
    private static readonly Dictionary<string, string> GroupFeatureMap = new()
    {
        ["dashboard"]       = "Module.Dashboard",
        ["billing"]         = "Module.Billing",
        ["inventory"]       = "Module.Inventory",
        ["crm"]             = "Module.CRM",
        ["reports"]         = "Module.Reports",
        ["wallet"]          = "Module.Wallet",
        ["pos"]             = "Module.Billing",  // POS is part of billing tier
        ["accounting"]      = "Module.Accounting",
        ["hr"]              = "Module.HR",
        ["purchasing"]      = "Module.Purchasing",
        ["sales-returns"]   = "Module.Billing",
        ["warranty"]        = "Module.Warranty",
        ["pricing"]         = "Module.Pricing",
        ["transport"]       = "Module.Transport",
        ["quotations"]      = "Module.Quotations",
        ["payment"]         = "Module.Payment",
        ["marketplace"]     = "Module.Marketplace",
        ["api-access"]      = "Module.ApiAccess",
        ["service-jobs"]    = "Module.ServiceJobs",
        ["verticals"]       = "Module.Verticals",
        ["sync"]            = "Module.Sync",
        ["on-prem"]         = "Module.OnPrem",
        ["customer-portal"] = "Module.CustomerPortal",
        ["hardware"]        = "Module.Hardware",
    };

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var (groupCode, featureCode) in GroupFeatureMap)
            {
                var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Code == groupCode, ct);
                if (item is null) continue;
                if (item.RequiredFeature == featureCode) continue;

                item.RequiredFeature = featureCode;
                logger.LogInformation("Menu group '{Code}' → RequiredFeature={Feature}", groupCode, featureCode);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "ModuleFeatureMenuSeeder failed — rolled back");
            throw;
        }
    }
}
