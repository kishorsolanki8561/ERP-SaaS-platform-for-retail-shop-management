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
        ("Wallet.TopUp",      "Wallet",    "Initiate and manage online wallet top-ups"),
        // POS / Shift — codes must match ShiftController [RequirePermission(...)]
        ("Shift.View",         "POS",      "View shifts"),
        ("Shift.Open",         "POS",      "Open a shift"),
        ("Shift.Close",        "POS",      "Close a shift"),
        ("Shift.ForceClose",   "POS",      "Force-close a shift"),
        ("Shift.CashMovement", "POS",      "Record cash in / out"),
        // Hardware — codes must match HardwareController [RequirePermission(...)]
        ("Hardware.CashDrawer",      "POS",      "Pop the cash drawer"),
        // Hardware device & template management
        ("Device.Configure",         "Hardware", "Register and configure hardware devices"),
        ("Template.Label.Manage",    "Hardware", "Manage label templates and print labels"),
        ("Template.Receipt.Manage",  "Hardware", "Manage receipt templates and print receipts"),
        // HR — codes must match HrController [RequirePermission(...)]
        ("HR.View",       "HR",   "View employees, attendance, leave and payroll"),
        ("HR.Manage",     "HR",   "Manage employees, leave types and approve requests"),
        ("HR.Attendance", "HR",   "Record own attendance and submit leave requests"),
        ("HR.Payroll",    "HR",   "Generate, approve and pay payroll"),
        // Accounting — codes must match AccountingController [RequirePermission(...)]
        ("Accounting.View",                "Accounting", "View accounts, vouchers, and ledger"),
        ("Accounting.ManageAccounts",      "Accounting", "Create and manage chart of accounts"),
        ("Accounting.CreateVoucher",       "Accounting", "Create journal vouchers"),
        ("Accounting.PostVoucher",         "Accounting", "Post and reverse journal vouchers"),
        ("Accounting.BankReconciliation",  "Accounting", "Perform bank reconciliation"),
        ("Accounting.ManageCheques",       "Accounting", "Record and manage cheques"),
        ("Accounting.ManageExpenses",      "Accounting", "Record petty cash and expenses"),
        ("Accounting.FixedAssets",         "Accounting", "Manage fixed assets and depreciation"),
        ("Accounting.CloseFinancialYear",  "Accounting", "Close the financial year"),
        // Purchasing — codes must match PurchasingController [RequirePermission(...)]
        ("Purchasing.View",                "Purchasing", "View suppliers, POs, and bills"),
        ("Purchasing.ManageSuppliers",     "Purchasing", "Create and manage suppliers"),
        ("Purchasing.CreatePurchaseOrder", "Purchasing", "Create and approve purchase orders"),
        ("Purchasing.ReceiveGoods",        "Purchasing", "Receive goods against a PO"),
        ("Purchasing.ManageBills",         "Purchasing", "Record and pay supplier bills"),
        ("Purchasing.ManagePurchaseReturns","Purchasing","Create and approve purchase returns"),
        // SalesReturns — codes must match SalesReturnsController [RequirePermission(...)]
        ("SalesReturns.Create",            "SalesReturns", "Create sales return requests"),
        ("SalesReturns.Approve",           "SalesReturns", "Approve and process sales returns"),
        // Reports — codes must match ReportsController [RequirePermission(...)]
        ("Reports.ViewSales",              "Reports", "View sales reports"),
        ("Reports.ViewAccounting",         "Reports", "View accounting and ledger reports"),
        ("Reports.ViewGst",                "Reports", "View GST reports"),
        ("Reports.Export",                 "Reports", "Export reports to PDF / Excel"),
        // Warranty — codes must match WarrantyController [RequirePermission(...)]
        ("Warranty.View",                  "Warranty", "View warranty registrations and claims"),
        ("Warranty.Manage",                "Warranty", "Register and manage warranties"),
        ("Warranty.ManageClaims",          "Warranty", "Create and resolve warranty claims"),
        // Pricing — codes must match PricingController [RequirePermission(...)]
        ("Pricing.View",                   "Pricing", "View discount rules, offers, and extra charges"),
        ("Pricing.Manage",                 "Pricing", "Create and manage pricing rules and offers"),
        // Transport — codes must match TransportController [RequirePermission(...)]
        ("Transport.View",                 "Transport", "View transport providers, vehicles and deliveries"),
        ("Transport.Manage",               "Transport", "Create and manage transport providers, vehicles and deliveries"),
        // Quotations — codes must match QuotationsController [RequirePermission(...)]
        ("Quotation.View",                 "Quotations", "View quotations, sales orders and delivery challans"),
        ("Quotation.Create",               "Quotations", "Create quotations, sales orders and delivery challans"),
        ("Quotation.Send",                 "Quotations", "Send quotations to customers and dispatch delivery challans"),
        ("Quotation.Revise",               "Quotations", "Revise an existing quotation"),
        ("Quotation.Accept",               "Quotations", "Accept a quotation on behalf of the customer"),
        ("Quotation.Convert",              "Quotations", "Convert a quotation to a sales order"),
        ("Quotation.Delete",               "Quotations", "Reject or cancel quotations, sales orders and delivery challans"),
        // Payment — codes must match PaymentController [RequirePermission(...)]
        ("Payment.View",                   "Payment", "View payment gateway accounts and transactions"),
        ("Payment.Configure",              "Payment", "Configure payment gateway accounts"),
        ("Payment.Initiate",               "Payment", "Initiate payment gateway transactions"),
        ("Payment.Manage",                 "Payment", "Manage payment transactions"),
        ("Payment.Refund",                 "Payment", "Process payment refunds"),
        ("Payment.Reconcile",              "Payment", "Run and review payment reconciliation"),
        // Files — codes must match FilesController [RequirePermission(...)]
        ("Files.Upload",       "Files",    "Upload files"),
        ("Files.View",         "Files",    "View and download files"),
        ("Files.Delete",       "Files",    "Delete uploaded files"),
        // Marketplace — codes must match MarketplaceController [RequirePermission(...)]
        ("Marketplace.View",         "Marketplace", "View marketplace accounts, orders and product mappings"),
        ("Marketplace.Manage",       "Marketplace", "Add and configure marketplace accounts and product links"),
        ("Marketplace.Sync",         "Marketplace", "Trigger inventory, price and order sync jobs"),
        ("Marketplace.ConvertOrder", "Marketplace", "Convert marketplace orders to invoices"),
        // Subscription — codes must match SubscriptionController [RequirePermission(...)]
        ("Subscription.View",        "Subscription", "View current subscription plan and available plans"),
        ("Subscription.Manage",      "Subscription", "Change subscription plan or cancel subscription"),
        // Customer Portal — codes must match OnlineOrdersController + inquiry management [RequirePermission(...)]
        ("OnlineOrder.View",   "CustomerPortal", "View online orders from the customer portal"),
        ("OnlineOrder.Manage", "CustomerPortal", "Accept, reject, dispatch and cancel online orders"),
        ("Inquiry.View",       "CustomerPortal", "View customer inquiries from the portal"),
        ("Inquiry.Manage",     "CustomerPortal", "Reply to, assign, and close customer inquiries"),
        ("Portal.Config",      "CustomerPortal", "Configure customer portal settings per shop"),
        // Audit Log — codes must match AuditLogController [RequirePermission(...)]
        ("Admin.AuditLog.View", "Admin", "View audit logs for any record"),
        // Platform Admin — codes must match PlatformAdminController [RequirePermission(...)]
        ("Platform.Shops.View",   "Platform", "View all shops and their data (platform owner only)"),
        ("Platform.Shops.Manage", "Platform", "Manage shop subscriptions and settings (platform owner only)"),
    ];

    // Shop Admin gets everything except platform-only permissions.
    private static readonly HashSet<string> PlatformOnlyPermissions =
    [
        "MasterData.Manage",
        "Platform.Shops.View",
        "Platform.Shops.Manage",
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
