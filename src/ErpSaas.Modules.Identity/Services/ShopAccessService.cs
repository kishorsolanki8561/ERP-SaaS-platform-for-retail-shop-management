using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Identity.Services;

public sealed class ShopAccessService(
    PlatformDbContext db,
    IPermissionService permissionService,
    ITenantContext tenant,
    IErrorLogger errorLogger)
    : BaseService<PlatformDbContext>(db, errorLogger), IShopAccessService
{
    // Human-readable metadata for known module feature codes
    private static readonly Dictionary<string, (string Label, string Icon)> ModuleMeta = new()
    {
        ["Module.Dashboard"]       = ("Dashboard",        "pi pi-home"),
        ["Module.Billing"]         = ("Billing & POS",    "pi pi-file-invoice-dollar"),
        ["Module.Inventory"]       = ("Inventory",        "pi pi-box"),
        ["Module.CRM"]             = ("CRM",              "pi pi-users"),
        ["Module.Reports"]         = ("Reports",          "pi pi-chart-bar"),
        ["Module.Wallet"]          = ("Customer Wallet",  "pi pi-wallet"),
        ["Module.Accounting"]      = ("Accounting",       "pi pi-calculator"),
        ["Module.HR"]              = ("HR & Payroll",     "pi pi-id-card"),
        ["Module.Purchasing"]      = ("Purchasing",       "pi pi-shopping-cart"),
        ["Module.Warranty"]        = ("Warranty",         "pi pi-shield"),
        ["Module.Pricing"]         = ("Pricing & Offers", "pi pi-tag"),
        ["Module.Transport"]       = ("Transport",        "pi pi-truck"),
        ["Module.Quotations"]      = ("Quotations",       "pi pi-file-edit"),
        ["Module.Payment"]         = ("Payment Gateway",  "pi pi-credit-card"),
        ["Module.Sync"]            = ("Offline Sync",     "pi pi-wifi"),
        ["Module.Hardware"]        = ("Hardware",         "pi pi-desktop"),
        ["Module.CustomerPortal"]  = ("Customer Portal",  "pi pi-globe"),
        ["Module.Marketplace"]     = ("Marketplace",      "pi pi-shopping-bag"),
        ["Module.ApiAccess"]       = ("API Access",       "pi pi-code"),
        ["Module.ServiceJobs"]     = ("Service Jobs",     "pi pi-wrench"),
        ["Module.Verticals"]       = ("Vertical Packs",   "pi pi-th-large"),
        ["Module.OnPrem"]          = ("On-Premise",       "pi pi-server"),
    };

    // Permission code → module feature code mapping
    private static readonly Dictionary<string, string> PermissionModuleMap = new()
    {
        // Billing
        { "Billing.View",    "Module.Billing" }, { "Billing.Create", "Module.Billing" },
        { "Billing.Edit",    "Module.Billing" }, { "Billing.Cancel", "Module.Billing" },
        // Inventory
        { "Inventory.View",   "Module.Inventory" }, { "Inventory.Manage", "Module.Inventory" },
        // CRM
        { "Crm.View",    "Module.CRM" }, { "Crm.Create", "Module.CRM" },
        { "Crm.Edit",    "Module.CRM" }, { "Crm.Manage",  "Module.CRM" },
        // Wallet
        { "Wallet.View",   "Module.Wallet" }, { "Wallet.Credit", "Module.Wallet" },
        { "Wallet.Debit",  "Module.Wallet" }, { "Wallet.TopUp",  "Module.Wallet" },
        // POS / Shift
        { "Shift.View", "Module.Billing" }, { "Shift.Open",  "Module.Billing" },
        { "Shift.Close","Module.Billing" }, { "Shift.ForceClose","Module.Billing" },
        { "Shift.CashMovement","Module.Billing" }, { "Hardware.CashDrawer","Module.Billing" },
        // Hardware
        { "Device.Configure",       "Module.Hardware" },
        { "Template.Label.Manage",  "Module.Hardware" },
        { "Template.Receipt.Manage","Module.Hardware" },
        // HR
        { "HR.View","Module.HR" }, { "HR.Manage","Module.HR" },
        { "HR.Attendance","Module.HR" }, { "HR.Payroll","Module.HR" },
        // Accounting
        { "Accounting.View","Module.Accounting" }, { "Accounting.ManageAccounts","Module.Accounting" },
        { "Accounting.CreateVoucher","Module.Accounting" }, { "Accounting.PostVoucher","Module.Accounting" },
        { "Accounting.BankReconciliation","Module.Accounting" }, { "Accounting.ManageCheques","Module.Accounting" },
        { "Accounting.ManageExpenses","Module.Accounting" }, { "Accounting.FixedAssets","Module.Accounting" },
        { "Accounting.CloseFinancialYear","Module.Accounting" },
        // Purchasing
        { "Purchasing.View","Module.Purchasing" }, { "Purchasing.ManageSuppliers","Module.Purchasing" },
        { "Purchasing.CreatePurchaseOrder","Module.Purchasing" }, { "Purchasing.ReceiveGoods","Module.Purchasing" },
        { "Purchasing.ManageBills","Module.Purchasing" }, { "Purchasing.ManagePurchaseReturns","Module.Purchasing" },
        // SalesReturns (part of Billing tier)
        { "SalesReturns.Create","Module.Billing" }, { "SalesReturns.Approve","Module.Billing" },
        // Reports
        { "Reports.ViewSales","Module.Reports" }, { "Reports.ViewAccounting","Module.Reports" },
        { "Reports.ViewGst","Module.Reports" }, { "Reports.Export","Module.Reports" },
        // Warranty
        { "Warranty.View","Module.Warranty" }, { "Warranty.Manage","Module.Warranty" },
        { "Warranty.ManageClaims","Module.Warranty" },
        // Pricing
        { "Pricing.View","Module.Pricing" }, { "Pricing.Manage","Module.Pricing" },
        // Transport
        { "Transport.View","Module.Transport" }, { "Transport.Manage","Module.Transport" },
        // Quotations
        { "Quotation.View","Module.Quotations" }, { "Quotation.Create","Module.Quotations" },
        { "Quotation.Send","Module.Quotations" }, { "Quotation.Revise","Module.Quotations" },
        { "Quotation.Accept","Module.Quotations" }, { "Quotation.Convert","Module.Quotations" },
        { "Quotation.Delete","Module.Quotations" },
        // Payment
        { "Payment.View","Module.Payment" }, { "Payment.Configure","Module.Payment" },
        { "Payment.Initiate","Module.Payment" }, { "Payment.Manage","Module.Payment" },
        { "Payment.Refund","Module.Payment" }, { "Payment.Reconcile","Module.Payment" },
        // Marketplace
        { "Marketplace.View","Module.Marketplace" }, { "Marketplace.Manage","Module.Marketplace" },
        { "Marketplace.Sync","Module.Marketplace" }, { "Marketplace.ConvertOrder","Module.Marketplace" },
        // API Access
        { "Integration.ManageApiKeys","Module.ApiAccess" }, { "Integration.ManageWebhooks","Module.ApiAccess" },
        { "Integration.ViewDeliveries","Module.ApiAccess" },
        // Sync
        { "Device.Register","Module.Sync" }, { "Device.Manage","Module.Sync" },
        { "Sync.View","Module.Sync" }, { "Sync.ResolveException","Module.Sync" },
        // On-prem
        { "OnPrem.View","Module.OnPrem" }, { "OnPrem.Manage","Module.OnPrem" },
        // Customer Portal
        { "OnlineOrder.View","Module.CustomerPortal" }, { "OnlineOrder.Manage","Module.CustomerPortal" },
        { "Inquiry.View","Module.CustomerPortal" }, { "Inquiry.Manage","Module.CustomerPortal" },
        { "Portal.Config","Module.CustomerPortal" },
        // Service Jobs
        { "ServiceJob.View","Module.ServiceJobs" }, { "ServiceJob.Create","Module.ServiceJobs" },
        { "ServiceJob.Diagnose","Module.ServiceJobs" }, { "ServiceJob.Approve","Module.ServiceJobs" },
        { "ServiceJob.Progress","Module.ServiceJobs" }, { "ServiceJob.Deliver","Module.ServiceJobs" },
        // Verticals
        { "Vertical.View","Module.Verticals" }, { "Vertical.Manage","Module.Verticals" },
        { "Medical.View","Module.Verticals" }, { "Medical.Manage","Module.Verticals" }, { "Medical.Dispense","Module.Verticals" },
        { "Loyalty.View","Module.Verticals" }, { "Loyalty.Manage","Module.Verticals" },
        { "Loyalty.Earn","Module.Verticals" }, { "Loyalty.Redeem","Module.Verticals" },
    };

    public async Task<IReadOnlyList<ModuleAccessDto>> GetModuleAccessAsync(CancellationToken ct = default)
    {
        var shopId = tenant.ShopId;
        var planCodes = await GetPlanFeatureCodesAsync(shopId, ct);
        var overrides = await _db.ShopFeatureOverrides
            .Where(o => o.ShopId == shopId)
            .ToListAsync(ct);

        var overrideMap = overrides.ToDictionary(o => o.FeatureCode);
        var effectiveCodes = (await permissionService.GetFeatureCodesAsync(shopId, ct)).ToHashSet();

        return ModuleMeta
            .Select(kvp =>
            {
                var code = kvp.Key;
                var isInPlan    = planCodes.Contains(code);
                var isEffective = effectiveCodes.Contains(code);
                var hasOverride = overrideMap.TryGetValue(code, out var ov);
                return new ModuleAccessDto(
                    code, kvp.Value.Label, kvp.Value.Icon,
                    isInPlan, isEffective,
                    hasOverride, hasOverride ? ov!.IsEnabled : null);
            })
            .ToList();
    }

    public async Task<Result<bool>> SetModuleVisibilityAsync(SetModuleVisibilityDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("ShopAccess.SetModuleVisibility", async () =>
        {
            if (!dto.FeatureCode.StartsWith("Module.", StringComparison.Ordinal))
                return Result<bool>.Failure(Errors.ShopAccess.FeatureNotModuleLevel);

            var shopId     = tenant.ShopId;
            var planCodes  = await GetPlanFeatureCodesAsync(shopId, ct);

            // Shop admin can only enable features that are in their plan
            if (dto.IsVisible && !planCodes.Contains(dto.FeatureCode))
                return Result<bool>.Failure(Errors.ShopAccess.FeatureNotInPlan);

            // Shop admin can only disable features that are in their plan
            if (!dto.IsVisible && !planCodes.Contains(dto.FeatureCode))
                return Result<bool>.Failure(Errors.ShopAccess.FeatureNotInPlan);

            var existing = await _db.ShopFeatureOverrides
                .FirstOrDefaultAsync(o => o.ShopId == shopId && o.FeatureCode == dto.FeatureCode, ct);

            if (existing is null)
            {
                _db.ShopFeatureOverrides.Add(new ShopFeatureOverride
                {
                    ShopId = shopId, FeatureCode = dto.FeatureCode,
                    IsEnabled = dto.IsVisible, CreatedAtUtc = DateTime.UtcNow,
                });
            }
            else
            {
                existing.IsEnabled    = dto.IsVisible;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            permissionService.InvalidateShopFeatureCache(shopId);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<UserPermissionSummaryDto?> GetUserPermissionsAsync(long targetUserId, CancellationToken ct = default)
    {
        var shopId = tenant.ShopId;
        var userShop = await _db.UserShops
            .Where(us => us.UserId == targetUserId && us.ShopId == shopId)
            .Select(us => new { us.User.DisplayName })
            .FirstOrDefaultAsync(ct);

        if (userShop is null) return null;

        var allPerms = await _db.Permissions
            .Select(p => new { p.Code, p.Label, p.Module })
            .ToListAsync(ct);

        var rolePermCodes = await _db.UserRoles
            .Where(ur => ur.UserId == targetUserId && ur.ShopId == shopId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(ct);

        var overrides = await _db.UserPermissionOverrides
            .Where(o => o.UserId == targetUserId && o.ShopId == shopId)
            .ToListAsync(ct);

        var overrideMap = overrides.ToDictionary(o => o.PermissionCode);
        var granted     = overrides.Where(o => o.IsGranted).Select(o => o.PermissionCode).ToHashSet();
        var revoked     = overrides.Where(o => !o.IsGranted).Select(o => o.PermissionCode).ToHashSet();

        var permStatuses = allPerms
            .Select(p =>
            {
                var fromRole    = rolePermCodes.Contains(p.Code);
                var hasOverride = overrideMap.TryGetValue(p.Code, out var ov);
                var isGranted   = revoked.Contains(p.Code) ? false
                                : granted.Contains(p.Code) ? true
                                : fromRole;
                return new PermissionStatusDto(p.Code, p.Label, p.Module, fromRole, hasOverride, isGranted);
            })
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .ToList();

        return new UserPermissionSummaryDto(targetUserId, userShop.DisplayName, permStatuses);
    }

    public async Task<Result<bool>> SetUserPermissionOverrideAsync(long targetUserId, SetPermissionOverrideDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("ShopAccess.SetUserPermissionOverride", async () =>
        {
            var shopId = tenant.ShopId;

            var userInShop = await _db.UserShops.AnyAsync(
                us => us.UserId == targetUserId && us.ShopId == shopId, ct);
            if (!userInShop)
                return Result<bool>.NotFound(Errors.ShopAccess.UserNotInShop);

            // If granting, ensure the permission's module is in this shop's effective feature set
            if (dto.IsGranted)
            {
                if (PermissionModuleMap.TryGetValue(dto.PermissionCode, out var reqModule))
                {
                    var effectiveFeats = await permissionService.GetFeatureCodesAsync(shopId, ct);
                    if (!effectiveFeats.Contains(reqModule))
                        return Result<bool>.Failure(Errors.ShopAccess.PermissionModuleNotInPlan);
                }
            }

            var existing = await _db.UserPermissionOverrides
                .FirstOrDefaultAsync(o => o.UserId == targetUserId && o.ShopId == shopId
                                       && o.PermissionCode == dto.PermissionCode, ct);

            if (existing is null)
            {
                _db.UserPermissionOverrides.Add(new UserPermissionOverride
                {
                    UserId = targetUserId, ShopId = shopId,
                    PermissionCode = dto.PermissionCode, IsGranted = dto.IsGranted,
                    SetByUserId = tenant.CurrentUserId, CreatedAtUtc = DateTime.UtcNow,
                });
            }
            else
            {
                existing.IsGranted    = dto.IsGranted;
                existing.SetByUserId  = tenant.CurrentUserId;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            permissionService.InvalidateUserPermissionCache(targetUserId, shopId);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<Result<bool>> RemoveUserPermissionOverrideAsync(long targetUserId, string permissionCode, CancellationToken ct = default)
        => await ExecuteAsync<bool>("ShopAccess.RemoveUserPermissionOverride", async () =>
        {
            var shopId   = tenant.ShopId;
            var existing = await _db.UserPermissionOverrides
                .FirstOrDefaultAsync(o => o.UserId == targetUserId && o.ShopId == shopId
                                       && o.PermissionCode == permissionCode, ct);

            if (existing is null)
                return Result<bool>.NotFound(Errors.ShopAccess.OverrideNotFound);

            _db.UserPermissionOverrides.Remove(existing);
            await _db.SaveChangesAsync(ct);
            permissionService.InvalidateUserPermissionCache(targetUserId, shopId);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<IReadOnlyList<string>> GetShopPlanFeaturesAsync(long shopId, CancellationToken ct = default)
        => await permissionService.GetFeatureCodesAsync(shopId, ct);

    // Returns only the PLAN features (no overrides) — used for validation
    private async Task<HashSet<string>> GetPlanFeatureCodesAsync(long shopId, CancellationToken ct)
    {
        var codes = await _db.ShopSubscriptions
            .Where(ss => ss.ShopId == shopId && ss.IsActive)
            .SelectMany(ss => ss.Plan.Features)
            .Select(f => f.FeatureCode)
            .ToListAsync(ct);
        return codes.ToHashSet();
    }
}
