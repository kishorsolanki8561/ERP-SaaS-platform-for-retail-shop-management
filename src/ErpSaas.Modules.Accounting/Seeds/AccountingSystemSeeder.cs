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

namespace ErpSaas.Modules.Accounting.Seeds;

public sealed class AccountingSystemSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    ILogger<AccountingSystemSeeder> logger) : IDataSeeder
{
    public int Order => 50;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedFeaturesAsync(ct);
        await SeedMenuAsync(ct);
        await SeedDdlAsync(ct);
        await SeedSequencesAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var perms = new[]
            {
                ("Accounting.View",                "View accounts, vouchers and reports"),
                ("Accounting.ManageAccounts",      "Create and edit ledger accounts"),
                ("Accounting.CreateVoucher",       "Create journal / payment / receipt vouchers"),
                ("Accounting.PostVoucher",         "Post and reverse vouchers"),
                ("Accounting.ManageExpenses",      "Record and manage expenses"),
                ("Accounting.CloseFinancialYear",  "Close a financial year"),
                ("Accounting.BankReconciliation",  "Manage bank statement reconciliation"),
                ("Accounting.ManageCheques",       "Receive, deposit, clear and bounce cheques"),
                ("Accounting.FixedAssets",         "Register, depreciate and dispose fixed assets"),
            };

            foreach (var (code, label) in perms)
            {
                if (!await platformDb.Permissions.AnyAsync(p => p.Code == code, ct))
                {
                    platformDb.Permissions.Add(new Permission
                    {
                        Code = code,
                        Module = "Accounting",
                        Label = label,
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
            logger.LogError(ex, "AccountingSystemSeeder.SeedPermissionsAsync failed");
            throw;
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var features = new[]
            {
                ("Accounting.Basic",         "Core accounting — COA, vouchers, expenses, bank accounts"),
                ("Accounting.Advanced",      "Bank reconciliation, fixed assets, depreciation"),
                ("Accounting.ITRFiling",     "ITR-3/4 export and tax-saving summary"),
                ("Accounting.GstReturns",    "GSTR-1 and GSTR-3B generation"),
                ("Accounting.MultiCurrency", "Multi-currency transactions and forex gain/loss"),
            };

            foreach (var (code, description) in features)
            {
                if (!await platformDb.SubscriptionPlanFeatures.AnyAsync(f => f.FeatureCode == code, ct))
                {
                    // Seed feature on Growth and Enterprise plans
                    var plans = await platformDb.SubscriptionPlans
                        .Where(p => p.Code == Constants.Plans.Growth || p.Code == Constants.Plans.Enterprise)
                        .ToListAsync(ct);

                    foreach (var plan in plans)
                    {
                        platformDb.SubscriptionPlanFeatures.Add(new SubscriptionPlanFeature
                        {
                            PlanId = plan.Id,
                            FeatureCode = code,
                            CreatedAtUtc = DateTime.UtcNow,
                        });
                    }
                    logger.LogInformation("Seeding feature: {Code}", code);
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "AccountingSystemSeeder.SeedFeaturesAsync failed");
            throw;
        }
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var group = await platformDb.MenuItems.FirstOrDefaultAsync(m => m.Code == "accounting", ct);
            if (group is null)
            {
                group = new MenuItem
                {
                    Code = "accounting",
                    Label = "Accounting",
                    Kind = MenuItemKind.Group,
                    Icon = "pi pi-calculator",
                    SortOrder = 50,
                    IsActive = true,
                    RequiredFeature = "Accounting.Basic",
                    CreatedAtUtc = DateTime.UtcNow,
                };
                platformDb.MenuItems.Add(group);
                await platformDb.SaveChangesAsync(ct);
            }

            var pages = new[]
            {
                ("accounting.accounts",        "Accounts",           "pi pi-list",            "/accounting/accounts",                 "Accounting.View",             null,                       10),
                ("accounting.vouchers",        "Vouchers",           "pi pi-book",            "/accounting/vouchers",                 "Accounting.View",             null,                       20),
                ("accounting.expenses",        "Expenses",           "pi pi-wallet",          "/accounting/expenses",                 "Accounting.ManageExpenses",   null,                       30),
                ("accounting.bank-accounts",   "Bank Accounts",      "pi pi-building-columns","/accounting/bank-accounts",            "Accounting.View",             null,                       40),
                ("accounting.reports",         "Reports",            "pi pi-chart-bar",       "/accounting/reports/trial-balance",    "Accounting.View",             null,                       50),
                ("accounting.gst",             "GST Returns",        "pi pi-file-export",     "/accounting/gst/gstr1",                "Accounting.View",             "Accounting.GstReturns",    60),
                ("accounting.year-end",        "Year End",           "pi pi-calendar-times",  "/accounting/year-end/close",           "Accounting.CloseFinancialYear","Accounting.Basic",        70),
                ("accounting.reconciliation",  "Bank Reconciliation","pi pi-arrows-h",        "/accounting/reconciliation",           "Accounting.BankReconciliation","Accounting.Advanced",     80),
                ("accounting.cheques",         "Cheques",            "pi pi-credit-card",     "/accounting/cheques",                  "Accounting.ManageCheques",    null,                       90),
                ("accounting.petty-cash",      "Petty Cash",         "pi pi-money-bill",      "/accounting/petty-cash",               "Accounting.ManageExpenses",   null,                       100),
                ("accounting.fixed-assets",    "Fixed Assets",       "pi pi-building",        "/accounting/fixed-assets",             "Accounting.FixedAssets",      "Accounting.Advanced",      110),
            };

            foreach (var (code, label, icon, route, perm, feat, sort) in pages)
            {
                if (!await platformDb.MenuItems.AnyAsync(m => m.Code == code, ct))
                {
                    platformDb.MenuItems.Add(new MenuItem
                    {
                        Code = code,
                        Label = label,
                        Kind = MenuItemKind.Page,
                        Icon = icon,
                        Route = route,
                        ParentId = group.Id,
                        SortOrder = sort,
                        RequiredPermission = perm,
                        RequiredFeature = feat,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "AccountingSystemSeeder.SeedMenuAsync failed");
            throw;
        }
    }

    private async Task SeedDdlAsync(CancellationToken ct)
    {
        await using var tx = await platformDb.Database.BeginTransactionAsync(ct);
        try
        {
            var catalogs = new (string Key, string Label, (string Code, string Label, int Sort)[] Items)[]
            {
                (Constants.DdlKeys.VoucherType, "Voucher Type",
                [
                    ("JOURNAL",  "Journal Entry",   10),
                    ("PAYMENT",  "Payment",         20),
                    ("RECEIPT",  "Receipt",         30),
                    ("CONTRA",   "Contra",          40),
                    ("SALE",     "Sale",            50),
                    ("PURCHASE", "Purchase",        60),
                ]),
                (Constants.DdlKeys.GstSlab, "GST Slab",
                [
                    ("GST_0",  "0%",  10),
                    ("GST_5",  "5%",  20),
                    ("GST_12", "12%", 30),
                    ("GST_18", "18%", 40),
                    ("GST_28", "28%", 50),
                ]),
                (Constants.DdlKeys.ChequeBounceReason, "Cheque Bounce Reason",
                [
                    ("INSUFFICIENT_FUNDS",     "Insufficient Funds",       10),
                    ("SIGNATURE_MISMATCH",     "Signature Mismatch",       20),
                    ("STALE_DATED",            "Stale Dated",              30),
                    ("POST_DATED",             "Post Dated",               40),
                    ("PAYMENT_STOPPED",        "Payment Stopped",          50),
                    ("ACCOUNT_CLOSED",         "Account Closed",           60),
                    ("REFER_TO_DRAWER",        "Refer to Drawer",          70),
                ]),
                (Constants.DdlKeys.FixedAssetCategory, "Fixed Asset Category",
                [
                    ("VEHICLE",       "Vehicle",          10),
                    ("COMPUTER",      "Computer / IT",    20),
                    ("FURNITURE",     "Furniture",        30),
                    ("POS_EQUIPMENT", "POS Equipment",    40),
                    ("BUILDING",      "Building",         50),
                    ("FIXTURE",       "Fixture / Fitting",60),
                    ("PLANT",         "Plant & Machinery",70),
                ]),
            };

            foreach (var (key, label, items) in catalogs)
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

                foreach (var (code, itemLabel, sort) in items)
                {
                    if (!catalog.Items.Any(i => i.Code == code))
                    {
                        catalog.Items.Add(new DdlItem
                        {
                            Code = code,
                            Label = itemLabel,
                            SortOrder = sort,
                            IsActive = true,
                        });
                    }
                }
            }

            await platformDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "AccountingSystemSeeder.SeedDdlAsync failed");
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
                (Constants.SequenceCodes.VoucherJournal,  Constants.SequencePrefixes.VoucherJournal),
                (Constants.SequenceCodes.VoucherPayment,  Constants.SequencePrefixes.VoucherPayment),
                (Constants.SequenceCodes.VoucherReceipt,  Constants.SequencePrefixes.VoucherReceipt),
                (Constants.SequenceCodes.VoucherContra,   Constants.SequencePrefixes.VoucherContra),
                (Constants.SequenceCodes.FixedAsset,      Constants.SequencePrefixes.FixedAsset),
            };

            foreach (var (code, prefix) in sequences)
            {
                var exists = await tenantDb.SequenceDefinitions
                    .IgnoreQueryFilters()
                    .AnyAsync(s => s.Code == code && s.ShopId == platformShopId, ct);

                if (!exists)
                {
                    tenantDb.SequenceDefinitions.Add(new SequenceDefinition
                    {
                        ShopId = platformShopId,
                        Code = code,
                        Prefix = prefix,
                        PadLength = 6,
                        LastNumber = 0,
                        CreatedAtUtc = DateTime.UtcNow,
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
            logger.LogError(ex, "AccountingSystemSeeder.SeedSequencesAsync failed");
            throw;
        }
    }
}
