using BCrypt.Net;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Seeds;

public sealed class IdentityDataSeeder(
    PlatformDbContext db,
    IConfiguration configuration,
    ILogger<IdentityDataSeeder> logger) : IDataSeeder
{
    public int Order => 20;

    // Master list of every permission in the system.
    // Add a row here whenever a new module ships a new permission code.
    private static readonly (string Code, string Module, string Label)[] AllPermissions =
    [
        ("Dashboard.View",    "Dashboard", "View Dashboard"),
        ("ShopProfile.View",  "Admin",     "View Shop Profile"),
        ("ShopProfile.Edit",  "Admin",     "Edit Shop Profile"),
        ("Users.View",        "Admin",     "View Users"),
        ("Users.Manage",      "Admin",     "Manage Users"),
        ("Users.Invite",      "Admin",     "Invite Users"),
        ("Users.Deactivate",  "Admin",     "Deactivate Users"),
        ("MasterData.Manage", "Admin",     "Manage Master Data"),
        // Billing — codes must match BillingController [RequirePermission(...)]
        ("Billing.View",      "Billing",   "View invoices and billing records"),
        ("Billing.Create",    "Billing",   "Create draft invoices and add lines"),
        ("Billing.Edit",      "Billing",   "Edit and finalize invoices"),
        ("Billing.Cancel",    "Billing",   "Cancel invoices"),
        // Inventory — codes must match InventoryController [RequirePermission(...)]
        ("Inventory.View",    "Inventory", "View products, warehouses, and stock levels"),
        ("Inventory.Manage",  "Inventory", "Create and update products, warehouses, and stock"),
        // CRM — codes must match CrmController [RequirePermission(...)]
        ("Crm.View",          "CRM",       "View CRM records"),
        ("Crm.Create",        "CRM",       "Create customers"),
        ("Crm.Edit",          "CRM",       "Edit / deactivate customers"),
        ("Crm.Manage",        "CRM",       "Manage customer groups and CRM settings"),
        // Wallet
        ("Wallet.View",       "Wallet",    "View customer wallet balances and transactions"),
        ("Wallet.Credit",     "Wallet",    "Credit a customer wallet"),
        ("Wallet.Debit",      "Wallet",    "Debit from a customer wallet"),
    ];

    // Shop Admin gets everything except platform-only permissions.
    private static readonly HashSet<string> PlatformOnlyPermissions =
    [
        "MasterData.Manage",
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await SeedSubscriptionPlansAsync(ct);
            await SeedPermissionsAndRolesAsync(ct);
            await SeedProductOwnerAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "IdentityDataSeeder failed — rolled back");
            throw;
        }
    }

    // ── Subscription plans ────────────────────────────────────────────────────

    private async Task SeedSubscriptionPlansAsync(CancellationToken ct)
    {
        var plans = new[]
        {
            (Constants.Plans.Starter,    "Starter",    0m,    0m,    2),
            (Constants.Plans.Growth,     "Growth",     999m,  9990m, 10),
            (Constants.Plans.Enterprise, "Enterprise", 2999m, 29990m, 100),
        };

        foreach (var (code, label, monthly, annual, maxUsers) in plans)
        {
            if (!await db.SubscriptionPlans.AnyAsync(p => p.Code == code, ct))
            {
                db.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Code = code, Label = label,
                    MonthlyPrice = monthly, AnnualPrice = annual,
                    MaxUsers = maxUsers, IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
                logger.LogInformation("Seeding subscription plan: {Code}", code);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    // ── Permissions + system roles ────────────────────────────────────────────

    private async Task SeedPermissionsAndRolesAsync(CancellationToken ct)
    {
        // 1. Upsert Permission rows
        foreach (var (code, module, label) in AllPermissions)
        {
            if (!await db.Permissions.AnyAsync(p => p.Code == code, ct))
            {
                db.Permissions.Add(new Permission
                {
                    Code = code, Module = module, Label = label,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync(ct);

        // Build a code → id lookup for assignment below
        var permMap = await db.Permissions
            .Select(p => new { p.Id, p.Code })
            .ToDictionaryAsync(p => p.Code, p => p.Id, ct);

        // 2. Ensure Platform Owner role exists
        var poRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == Constants.Roles.PlatformOwner, ct);
        if (poRole is null)
        {
            poRole = new Role
            {
                Code = Constants.Roles.PlatformOwner, Label = "Platform Owner",
                IsSystemRole = true, CreatedAtUtc = DateTime.UtcNow
            };
            db.Roles.Add(poRole);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded role: {Code}", Constants.Roles.PlatformOwner);
        }

        // 3. Ensure Shop Admin role exists
        var saRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == Constants.Roles.ShopAdmin, ct);
        if (saRole is null)
        {
            saRole = new Role
            {
                Code = Constants.Roles.ShopAdmin, Label = "Shop Admin",
                IsSystemRole = true, CreatedAtUtc = DateTime.UtcNow
            };
            db.Roles.Add(saRole);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded role: {Code}", Constants.Roles.ShopAdmin);
        }

        // 4. Assign ALL permissions to Platform Owner (idempotent)
        var existingPoPermIds = (await db.RolePermissions
            .Where(rp => rp.RoleId == poRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct)).ToHashSet();

        foreach (var (code, _, _) in AllPermissions)
        {
            var permId = permMap[code];
            if (!existingPoPermIds.Contains(permId))
                db.RolePermissions.Add(new RolePermission
                    { RoleId = poRole.Id, PermissionId = permId, CreatedAtUtc = DateTime.UtcNow });
        }

        // 5. Assign shop-level permissions to Shop Admin (idempotent)
        var existingSaPermIds = (await db.RolePermissions
            .Where(rp => rp.RoleId == saRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct)).ToHashSet();

        foreach (var (code, _, _) in AllPermissions)
        {
            if (PlatformOnlyPermissions.Contains(code)) continue;
            var permId = permMap[code];
            if (!existingSaPermIds.Contains(permId))
                db.RolePermissions.Add(new RolePermission
                    { RoleId = saRole.Id, PermissionId = permId, CreatedAtUtc = DateTime.UtcNow });
        }

        await db.SaveChangesAsync(ct);
    }

    // ── Product Owner user ────────────────────────────────────────────────────

    private async Task SeedProductOwnerAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.IsPlatformAdmin, ct))
            return;

        var email    = configuration["PRODUCT_OWNER_EMAIL"];
        var name     = configuration["PRODUCT_OWNER_NAME"];
        var password = configuration["PRODUCT_OWNER_PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("PRODUCT_OWNER_EMAIL/PASSWORD not set — skipping PO seed");
            return;
        }

        var user = new User
        {
            Email = email,
            DisplayName = name ?? "Product Owner",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password,
                workFactor: int.Parse(
                    configuration[Constants.Security.BcryptWorkFactorKey]
                    ?? Constants.Security.DefaultBcryptWorkFactor.ToString())),
            IsActive = true, IsPlatformAdmin = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        // Assign Platform Owner role (seeded in SeedPermissionsAndRolesAsync)
        var poRole = await db.Roles.FirstAsync(r => r.Code == Constants.Roles.PlatformOwner, ct);
        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id, ShopId = 0, RoleId = poRole.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded product owner: {Email}", email);
    }
}
