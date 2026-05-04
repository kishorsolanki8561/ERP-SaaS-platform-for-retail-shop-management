using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Modules.Billing.Entities;
using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Modules.Crm.Entities;
using ErpSaas.Modules.Inventory.Entities;
using ErpSaas.Modules.Inventory.Enums;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Api.Seeds;

/// <summary>
/// Seeds a realistic demo shop ("Nexus Electronics") with products, customers, and invoices
/// so every screen shows real data during a demo.
/// Only runs when Features:SeedDemoData = true. Fully idempotent.
/// </summary>
public sealed class DemoDataSeeder(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    IConfiguration config,
    ILogger<DemoDataSeeder> logger) : IDataSeeder
{
    public int Order => 1000;

    private const string DemoShopCode = "DEMO";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!config.GetValue<bool>("Features:SeedDemoData"))
            return;

        logger.LogInformation("DemoDataSeeder: SeedDemoData=true, starting...");
        var shopId = await SeedPlatformAsync(ct);
        if (shopId == 0)
        {
            logger.LogWarning("DemoDataSeeder: could not resolve demo shop — skipping tenant data");
            return;
        }

        await SeedTenantDataAsync(shopId, ct);
    }

    // ── A. PlatformDB: shop + subscription + admin link ──────────────────────────

    private async Task<long> SeedPlatformAsync(CancellationToken ct)
    {
        var shop = await platformDb.Shops
            .FirstOrDefaultAsync(s => s.ShopCode == DemoShopCode, ct);

        if (shop is null)
        {
            shop = new Shop
            {
                ShopCode     = DemoShopCode,
                LegalName    = "Nexus Electronics",
                TradeName    = "Nexus Electricals & Power Tools",
                GstNumber    = "29AABCN1234A1Z5",
                AddressLine1 = "Shop No. 12, Industrial Estate",
                City         = "Bengaluru",
                StateCode    = "KA",
                PinCode      = "560001",
                CurrencyCode = "INR",
                TimeZone     = "Asia/Kolkata",
                IsActive     = true,
            };
            platformDb.Shops.Add(shop);
            await platformDb.SaveChangesAsync(ct);
            logger.LogInformation("DemoDataSeeder: created demo shop (Id={ShopId})", shop.Id);
        }

        // Subscription — use Enterprise plan so all [RequireFeature] gates pass
        if (!await platformDb.ShopSubscriptions.AnyAsync(s => s.ShopId == shop.Id, ct))
        {
            var plan = await platformDb.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == Constants.Plans.Enterprise, ct);
            if (plan is not null)
            {
                platformDb.ShopSubscriptions.Add(new ShopSubscription
                {
                    ShopId        = shop.Id,
                    PlanId        = plan.Id,
                    StartsAtUtc   = DateTime.UtcNow,
                    IsActive      = true,
                    BillingCycle  = BillingCycle.Annual,
                });
                await platformDb.SaveChangesAsync(ct);
                logger.LogInformation("DemoDataSeeder: created Enterprise subscription for demo shop");
            }
        }

        // Link platform admin so they can switch into the demo shop
        var adminUser = await platformDb.Users
            .FirstOrDefaultAsync(u => u.IsPlatformAdmin, ct);
        if (adminUser is not null &&
            !await platformDb.UserShops
                .AnyAsync(us => us.UserId == adminUser.Id && us.ShopId == shop.Id, ct))
        {
            platformDb.UserShops.Add(new UserShop
                { UserId = adminUser.Id, ShopId = shop.Id, IsActive = true });

            var saRole = await platformDb.Roles
                .FirstOrDefaultAsync(r => r.Code == Constants.Roles.ShopAdmin, ct);
            if (saRole is not null)
                platformDb.UserRoles.Add(new UserRole
                    { UserId = adminUser.Id, ShopId = shop.Id, RoleId = saRole.Id });

            await platformDb.SaveChangesAsync(ct);
            logger.LogInformation("DemoDataSeeder: linked platform admin to demo shop");
        }

        return shop.Id;
    }

    // ── B–G. TenantDB: warehouse → groups → customers → products → stock → invoices ──

    private async Task SeedTenantDataAsync(long shopId, CancellationToken ct)
    {
        if (await tenantDb.Set<Warehouse>().IgnoreQueryFilters()
                .AnyAsync(w => w.ShopId == shopId && w.Code == "WH-MAIN", ct))
        {
            logger.LogInformation("DemoDataSeeder: tenant data already seeded for shop {ShopId}", shopId);
            return;
        }

        logger.LogInformation("DemoDataSeeder: seeding tenant data for shop {ShopId}", shopId);

        // B. Warehouse
        var warehouse = new Warehouse
            { ShopId = shopId, Code = "WH-MAIN", Name = "Main Warehouse", IsDefault = true, IsActive = true };
        tenantDb.Set<Warehouse>().Add(warehouse);
        await tenantDb.SaveChangesAsync(ct);

        // C. Customer groups
        var retail    = new CustomerGroup { ShopId = shopId, Code = "RETAIL",    Name = "Retail Customers",    DiscountPercent = 0m,  IsActive = true };
        var wholesale = new CustomerGroup { ShopId = shopId, Code = "WHOLESALE", Name = "Wholesale Customers", DiscountPercent = 5m,  IsActive = true };
        tenantDb.Set<CustomerGroup>().AddRange(retail, wholesale);
        await tenantDb.SaveChangesAsync(ct);

        // D. Customers
        var customers = BuildCustomers(shopId, retail.Id, wholesale.Id);
        tenantDb.Set<Customer>().AddRange(customers);
        await tenantDb.SaveChangesAsync(ct);

        // E. Products + ProductUnits (cascade-saved together)
        var products = BuildProducts(shopId);
        tenantDb.Set<Product>().AddRange(products);
        await tenantDb.SaveChangesAsync(ct);

        // F. Opening stock movements (15 products)
        var movements = BuildOpeningStock(shopId, products, warehouse.Id);
        tenantDb.Set<StockMovement>().AddRange(movements);
        await tenantDb.SaveChangesAsync(ct);

        // G. Invoices with lines + payments
        var invoices = BuildInvoices(shopId, customers, products, warehouse.Id);
        tenantDb.Set<Invoice>().AddRange(invoices);
        await tenantDb.SaveChangesAsync(ct);

        logger.LogInformation(
            "DemoDataSeeder: seeded {P} products, {C} customers, {I} invoices for shop {S}",
            products.Count, customers.Count, invoices.Count, shopId);
    }

    // ── Data builders ─────────────────────────────────────────────────────────────

    private static List<Customer> BuildCustomers(long shopId, long retailId, long wholesaleId)
    {
        static Customer C(long s, string code, string name, string type, string? phone, string? gst, long groupId, decimal credit = 0)
            => new() { ShopId = s, CustomerCode = code, DisplayName = name, CustomerType = type, Phone = phone, GstNumber = gst, CustomerGroupId = groupId, CreditLimitAmount = credit, IsActive = true };

        return
        [
            C(shopId, "CUST-001", "Rahul Constructions",           "WHOLESALE", "9876543210", null,               wholesaleId, 100_000m),
            C(shopId, "CUST-002", "City Electricals & Hardware",   "WHOLESALE", "9845612345", "29AACCX1234B1ZK", wholesaleId,  50_000m),
            C(shopId, "CUST-003", "Priya Sharma",                  "RETAIL",    "9988776655", null,               retailId),
            C(shopId, "CUST-004", "Rajesh Kumar",                  "RETAIL",    "9900112233", null,               retailId),
            C(shopId, "CUST-005", "Modern Builders Ltd",           "WHOLESALE", "9123456780", "29AADCM5678C1Z2", wholesaleId,  75_000m),
            C(shopId, "CUST-006", "M/s Khan Electric Works",       "RETAIL",    "9234567890", null,               retailId),
            C(shopId, "CUST-007", "Sunrise Contractor",            "WHOLESALE", "9345678901", "29AADSN9012D1Z1", wholesaleId,  40_000m),
            C(shopId, "CUST-008", "Amit Patel",                    "RETAIL",    "9456789012", null,               retailId),
            C(shopId, "CUST-009", "Tech Solutions Pvt Ltd",        "WHOLESALE", "9567890123", "29AADTS3456E1Z5", wholesaleId,  60_000m),
            C(shopId, "CUST-010", "Quick Fix Electricals",         "RETAIL",    "9678901234", null,               retailId),
        ];
    }

    private static List<Product> BuildProducts(long shopId)
    {
        static ProductUnit Pcs(long s) => new()
            { ShopId = s, UnitCode = "PCS", UnitLabel = "Pieces", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true };
        static ProductUnit Mtr(long s) => new()
            { ShopId = s, UnitCode = "MTR", UnitLabel = "Metres",  ConversionFactor = 1m, IsBaseUnit = true, IsActive = true };

        Product P(string code, string name, string cat, string hsn, decimal gst, decimal sale, decimal purchase, decimal? mrp = null, decimal minStock = 20m, bool useMtr = false)
            => new()
            {
                ShopId        = shopId,
                ProductCode   = code,
                Name          = name,
                CategoryCode  = cat,
                HsnSacCode    = hsn,
                GstRate       = gst,
                BaseUnitCode  = useMtr ? "MTR" : "PCS",
                SalePrice     = sale,
                PurchasePrice = purchase,
                MrpPrice      = mrp ?? sale * 1.1m,
                MinStockLevel = minStock,
                IsActive      = true,
                Units         = [useMtr ? Mtr(shopId) : Pcs(shopId)],
            };

        return
        [
            P("PRD-001", "LED Bulb 9W",                 "LIGHTING",   "8539", 12m,    85m,    55m,  100m,  50m),
            P("PRD-002", "LED Tube Light 20W",          "LIGHTING",   "8539", 12m,   320m,   210m,  380m,  30m),
            P("PRD-003", "Ceiling Fan (Brown)",         "ELECTRICAL", "8414", 18m,  1800m,  1250m, 2100m,  10m),
            P("PRD-004", "MCB Single Pole 32A",         "ELECTRICAL", "8536", 18m,   450m,   310m,  520m,  20m),
            P("PRD-005", "Copper Wire 1.5mm",           "CABLES",     "8544", 18m,    28m,    20m,   32m, 100m, useMtr: true),
            P("PRD-006", "Copper Wire 2.5mm",           "CABLES",     "8544", 18m,    45m,    31m,   52m, 100m, useMtr: true),
            P("PRD-007", "PVC Conduit Pipe 20mm",       "HARDWARE",   "3917", 18m,    65m,    44m,   75m,  50m),
            P("PRD-008", "Switch Board 4-Module",       "ELECTRICAL", "8536", 18m,   180m,   120m,  210m,  30m),
            P("PRD-009", "Electric Switch 6A",          "ELECTRICAL", "8536", 18m,    35m,    22m,   42m,  80m),
            P("PRD-010", "Power Socket 6A",             "ELECTRICAL", "8536", 18m,    45m,    29m,   52m,  80m),
            P("PRD-011", "Voltage Stabilizer 5KVA",     "ELECTRICAL", "8504", 18m,  4200m,  3000m, 4800m,   5m),
            P("PRD-012", "UPS 600VA",                   "ELECTRONICS","8504", 18m,  2800m,  2000m, 3200m,  10m),
            P("PRD-013", "Extension Board 4-Socket",    "ACCESSORIES","8536", 18m,   320m,   210m,  370m,  20m),
            P("PRD-014", "CCTV Dome Camera 2MP",        "ELECTRONICS","8525", 18m,  1800m,  1300m, 2100m,  10m),
            P("PRD-015", "Inverter Battery 150Ah",      "BATTERIES",  "8507", 28m, 12500m,  9500m,14000m,   5m),
            P("PRD-016", "Solar Panel 100W",            "ELECTRICAL", "8541",  5m,  4500m,  3400m, 5200m,   5m),
            P("PRD-017", "Electric Drill Machine 13mm", "POWER_TOOLS","8467", 18m,  2200m,  1600m, 2500m,   5m),
            P("PRD-018", "Angle Grinder 4\"",           "POWER_TOOLS","8467", 18m,  1400m,  1000m, 1600m,   5m),
            P("PRD-019", "Soldering Iron 25W",          "HARDWARE",   "8515", 18m,   180m,   120m,  210m,  20m),
            P("PRD-020", "Digital Multimeter",          "ELECTRONICS","9030", 18m,   850m,   600m, 1000m,  10m),
        ];
    }

    private static List<StockMovement> BuildOpeningStock(long shopId, List<Product> products, long warehouseId)
    {
        var baseDate = DateTime.UtcNow.AddDays(-90);
        var qtys = new[] { 150, 80, 25, 60, 500, 300, 200, 100, 250, 200, 12, 20, 60, 15, 8, 10, 8, 8, 40, 20 };

        var result = new List<StockMovement>();
        for (int i = 0; i < Math.Min(products.Count, qtys.Length); i++)
        {
            var p    = products[i];
            var unit = p.Units.First();
            result.Add(new StockMovement
            {
                ShopId                  = shopId,
                ProductId               = p.Id,
                WarehouseId             = warehouseId,
                MovementType            = StockMovementType.Opening,
                ProductUnitId           = unit.Id,
                UnitCodeSnapshot        = unit.UnitCode,
                ConversionFactorSnapshot = unit.ConversionFactor,
                QuantityInBilledUnit    = qtys[i],
                QuantityInBaseUnit      = qtys[i],
                Remarks                 = "Opening stock",
                MovedAtUtc              = baseDate,
            });
        }
        return result;
    }

    private static List<Invoice> BuildInvoices(
        long shopId, List<Customer> customers, List<Product> products, long warehouseId)
    {
        var baseDate = DateTime.UtcNow;

        Invoice Inv(string num, int custIdx, InvoiceStatus status, int daysAgo, params (int pIdx, decimal qty)[] lines)
        {
            var cust    = customers[custIdx];
            var invDate = baseDate.AddDays(-daysAgo);
            var invLines = lines.Select((l, i) =>
            {
                var p    = products[l.pIdx];
                var unit = p.Units.First();
                var (taxable, cgst, sgst, total) = Calc(l.qty, p.SalePrice, p.GstRate);
                return new InvoiceLine
                {
                    ShopId                   = shopId,
                    ProductId                = p.Id,
                    ProductNameSnapshot      = p.Name,
                    ProductCodeSnapshot      = p.ProductCode,
                    HsnSacCodeSnapshot       = p.HsnSacCode,
                    ProductUnitId            = unit.Id,
                    UnitCodeSnapshot         = unit.UnitCode,
                    ConversionFactorSnapshot = 1m,
                    QuantityInBilledUnit     = l.qty,
                    QuantityInBaseUnit       = l.qty,
                    UnitPrice                = p.SalePrice,
                    TaxableAmount            = taxable,
                    GstRate                  = p.GstRate,
                    CgstAmount               = cgst,
                    SgstAmount               = sgst,
                    LineTotal                = total,
                    SortOrder                = i + 1,
                };
            }).ToList();

            var subTotal  = invLines.Sum(l => l.TaxableAmount);
            var totalTax  = invLines.Sum(l => l.CgstAmount + l.SgstAmount);
            var grandTotal = subTotal + totalTax;

            var invoice = new Invoice
            {
                ShopId               = shopId,
                InvoiceNumber        = num,
                InvoiceDate          = invDate,
                CustomerId           = cust.Id,
                CustomerNameSnapshot = cust.DisplayName,
                CustomerPhoneSnapshot = cust.Phone,
                Status               = status,
                SubTotal             = subTotal,
                TotalTaxAmount       = totalTax,
                GrandTotal           = grandTotal,
                PaidAmount           = status == InvoiceStatus.Paid ? grandTotal : 0m,
                OutstandingAmount    = status == InvoiceStatus.Finalized ? grandTotal : 0m,
                WarehouseId          = warehouseId,
                Lines                = invLines,
            };

            if (status == InvoiceStatus.Paid)
            {
                invoice.Payments.Add(new InvoicePayment
                {
                    ShopId    = shopId,
                    Mode      = custIdx % 2 == 0 ? PaymentMode.Cash : PaymentMode.Upi,
                    Amount    = grandTotal,
                    PaidAtUtc = invDate.AddHours(2),
                });
            }

            return invoice;
        }

        return
        [
            Inv("DEMO-INV-001", 0, InvoiceStatus.Paid,      88, (0, 10), (1, 5),  (2, 1)),
            Inv("DEMO-INV-002", 1, InvoiceStatus.Paid,      80, (3, 8),  (4, 200),(10, 1)),
            Inv("DEMO-INV-003", 2, InvoiceStatus.Paid,      75, (0, 6),  (8, 20)),
            Inv("DEMO-INV-004", 3, InvoiceStatus.Finalized, 60, (5, 100),(6, 50)),
            Inv("DEMO-INV-005", 4, InvoiceStatus.Finalized, 50, (7, 10), (9, 10), (11, 2), (12, 3), (13, 1)),
            Inv("DEMO-INV-006", 5, InvoiceStatus.Finalized, 40, (1, 4),  (3, 5),  (17, 2)),
            Inv("DEMO-INV-007", 6, InvoiceStatus.Finalized, 30, (14, 2), (15, 1)),
            Inv("DEMO-INV-008", 7, InvoiceStatus.Draft,     20, (19, 1)),
            Inv("DEMO-INV-009", 8, InvoiceStatus.Draft,     15, (16, 1), (17, 1), (18, 2)),
            Inv("DEMO-INV-010", 9, InvoiceStatus.Cancelled, 10, (2, 1)),
        ];
    }

    private static (decimal taxable, decimal cgst, decimal sgst, decimal total) Calc(
        decimal qty, decimal price, decimal gstRate)
    {
        var taxable = Math.Round(qty * price, 2);
        var cgst    = Math.Round(taxable * gstRate / 2m / 100m, 2);
        var sgst    = Math.Round(taxable * gstRate / 2m / 100m, 2);
        return (taxable, cgst, sgst, taxable + cgst + sgst);
    }
}
