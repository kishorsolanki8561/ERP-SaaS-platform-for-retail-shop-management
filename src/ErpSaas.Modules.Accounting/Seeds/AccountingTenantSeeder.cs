using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.Accounting.Entities;
using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Accounting.Seeds;

/// <summary>
/// Seeds the default Chart of Accounts for a new shop during onboarding.
/// Follows the standard Indian COA layout (Tally-compatible).
/// </summary>
public sealed class AccountingTenantSeeder(
    TenantDbContext db,
    ILogger<AccountingTenantSeeder> logger) : ITenantSeeder
{
    public int Order => 10;

    public async Task SeedAsync(long shopId, CancellationToken ct = default)
    {
        var alreadySeeded = await db.Set<AccountGroup>()
            .IgnoreQueryFilters()
            .AnyAsync(g => g.ShopId == shopId && g.IsSystem, ct);

        if (alreadySeeded) return;

        logger.LogInformation("Seeding default COA for shop {ShopId}", shopId);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var groups = DefaultAccountGroups(shopId);
            db.Set<AccountGroup>().AddRange(groups);
            await db.SaveChangesAsync(ct);

            var accounts = DefaultAccounts(shopId, groups);
            db.Set<Account>().AddRange(accounts);
            await db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "AccountingTenantSeeder failed for shop {ShopId}", shopId);
            throw;
        }
    }

    private static List<AccountGroup> DefaultAccountGroups(long shopId)
    {
        var now = DateTime.UtcNow;
        return
        [
            new() { ShopId = shopId, Code = "ASSETS",      Name = "Assets",               Nature = AccountNature.Asset,     IsSystem = true, SortOrder = 10, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "LIABILITIES",  Name = "Liabilities",          Nature = AccountNature.Liability, IsSystem = true, SortOrder = 20, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "INCOME",       Name = "Income",               Nature = AccountNature.Income,    IsSystem = true, SortOrder = 30, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "EXPENSES",     Name = "Expenses",             Nature = AccountNature.Expense,   IsSystem = true, SortOrder = 40, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "EQUITY",       Name = "Capital & Equity",     Nature = AccountNature.Equity,    IsSystem = true, SortOrder = 50, CreatedAtUtc = now },
        ];
    }

    private static List<Account> DefaultAccounts(long shopId, List<AccountGroup> groups)
    {
        var now = DateTime.UtcNow;
        var assets      = groups.First(g => g.Code == "ASSETS");
        var liabilities = groups.First(g => g.Code == "LIABILITIES");
        var income      = groups.First(g => g.Code == "INCOME");
        var expenses    = groups.First(g => g.Code == "EXPENSES");
        var equity      = groups.First(g => g.Code == "EQUITY");

        return
        [
            // Assets
            new() { ShopId = shopId, Code = "1010", Name = "Cash",                        AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1020", Name = "Bank",                         AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1030", Name = "Cheques in Hand",              AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1110", Name = "Petty Cash",                   AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1100", Name = "Accounts Receivable",          AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1200", Name = "Inventory",                    AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1300", Name = "Input CGST",                   AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1301", Name = "Input SGST",                   AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1302", Name = "Input IGST",                   AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1500", Name = "Fixed Assets — Gross Cost",    AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "1510", Name = "Accumulated Depreciation",     AccountGroupId = assets.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },

            // Liabilities
            new() { ShopId = shopId, Code = "2100", Name = "Accounts Payable",            AccountGroupId = liabilities.Id, OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "2200", Name = "Output CGST Payable",         AccountGroupId = liabilities.Id, OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "2201", Name = "Output SGST Payable",         AccountGroupId = liabilities.Id, OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "2202", Name = "Output IGST Payable",         AccountGroupId = liabilities.Id, OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "2210", Name = "Customer Wallet Liability",   AccountGroupId = liabilities.Id, OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },

            // Income
            new() { ShopId = shopId, Code = "4000", Name = "Sales Revenue",               AccountGroupId = income.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "4010", Name = "Sales Returns",               AccountGroupId = income.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },

            // Expenses
            new() { ShopId = shopId, Code = "5000", Name = "Cost of Goods Sold",          AccountGroupId = expenses.Id,    OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "5100", Name = "Salaries & Wages",            AccountGroupId = expenses.Id,    OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "5200", Name = "Rent",                        AccountGroupId = expenses.Id,    OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "5300", Name = "Office & Admin Expenses",     AccountGroupId = expenses.Id,    OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "5400", Name = "Bank Charges",                AccountGroupId = expenses.Id,    OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "5500", Name = "Depreciation Expense",        AccountGroupId = expenses.Id,    OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "5900", Name = "Loss on Asset Disposal",      AccountGroupId = expenses.Id,    OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "4810", Name = "Cash Overage",                AccountGroupId = income.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "4900", Name = "Gain on Asset Disposal",      AccountGroupId = income.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "5810", Name = "Cash Shortage",               AccountGroupId = expenses.Id,    OpeningBalance = 0, OpeningBalanceType = DebitCredit.Debit,  IsSystem = true, IsActive = true, CreatedAtUtc = now },

            // Equity
            new() { ShopId = shopId, Code = "3000", Name = "Owner's Capital",             AccountGroupId = equity.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
            new() { ShopId = shopId, Code = "3100", Name = "Retained Earnings",           AccountGroupId = equity.Id,      OpeningBalance = 0, OpeningBalanceType = DebitCredit.Credit, IsSystem = true, IsActive = true, CreatedAtUtc = now },
        ];
    }
}
