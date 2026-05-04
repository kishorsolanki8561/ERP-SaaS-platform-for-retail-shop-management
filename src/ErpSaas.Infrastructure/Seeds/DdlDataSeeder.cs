using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Seeds;

public sealed class DdlDataSeeder(
    PlatformDbContext db,
    ILogger<DdlDataSeeder> logger) : IDataSeeder
{
    public int Order => 10;

    private static readonly (string Key, string Label, (string Code, string Label)[] Items)[] Catalogs =
    [
        ("PAYMENT_MODE", "Payment Mode",
        [
            ("CASH", "Cash"), ("CARD", "Card"), ("UPI", "UPI"),
            ("BANK_TRANSFER", "Bank Transfer"), ("CREDIT", "Credit"),
        ]),
        ("INVOICE_STATUS", "Invoice Status",
        [
            ("DRAFT", "Draft"), ("FINALIZED", "Finalized"), ("PAID", "Paid"),
            ("CANCELLED", "Cancelled"), ("PARTIALLY_PAID", "Partially Paid"),
        ]),
        ("WARRANTY_STATUS", "Warranty Status",
        [
            ("ACTIVE", "Active"), ("CLAIMED", "Claimed"),
            ("EXPIRED", "Expired"), ("VOIDED", "Voided"),
        ]),
        ("STOCK_MOVEMENT_TYPE", "Stock Movement Type",
        [
            ("PURCHASE", "Purchase"), ("SALE", "Sale"), ("RETURN", "Return"),
            ("ADJUSTMENT", "Adjustment"), ("TRANSFER", "Transfer"),
        ]),
        ("CUSTOMER_TYPE", "Customer Type",
        [
            ("RETAIL", "Retail"), ("WHOLESALE", "Wholesale"),
            ("DEALER", "Dealer"), ("ONLINE", "Online"),
        ]),
        ("PAYMENT_GATEWAY", "Payment Gateway",
        [
            ("RAZORPAY", "Razorpay"), ("STRIPE", "Stripe"),
            ("PAYTM", "Paytm"), ("PHONEPE", "PhonePe"),
        ]),
        ("METER_CODE", "Usage Meter Code",
        [
            ("invoices",     "Monthly Invoices"),
            ("products",     "Total Products"),
            ("active_users", "Active Users"),
            ("sms",          "SMS per Month"),
            ("email",        "Emails per Month"),
            ("storage_mb",   "Storage (MB)"),
        ]),
        ("AUDIT_ENTITY_TYPE", "Audit Entity Type",
        [
            ("Invoice", "Invoice"),
            ("InvoiceLine", "Invoice Line"),
            ("Product", "Product"),
            ("Customer", "Customer"),
            ("Supplier", "Supplier"),
            ("PurchaseOrder", "Purchase Order"),
            ("PurchaseOrderLine", "Purchase Order Line"),
            ("Bill", "Bill"),
            ("PurchaseReturn", "Purchase Return"),
            ("SalesReturn", "Sales Return"),
            ("SalesReturnLine", "Sales Return Line"),
            ("Employee", "Employee"),
            ("Voucher", "Voucher"),
            ("Quotation", "Quotation"),
            ("SalesOrder", "Sales Order"),
            ("DeliveryChallan", "Delivery Challan"),
            ("DiscountRule", "Discount Rule"),
        ]),
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var existingKeys = await db.DdlCatalogs.Select(c => c.Key).ToListAsync(ct);

            foreach (var (key, label, items) in Catalogs)
            {
                if (existingKeys.Contains(key)) continue;

                var catalog = new DdlCatalog { Key = key, Label = label, IsActive = true };
                for (int i = 0; i < items.Length; i++)
                {
                    catalog.Items.Add(new DdlItem
                    {
                        Code = items[i].Code, Label = items[i].Label,
                        SortOrder = (i + 1) * 10, IsActive = true
                    });
                }
                db.DdlCatalogs.Add(catalog);
                logger.LogInformation("Seeding DDL catalog: {Key}", key);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "DdlDataSeeder failed — rolled back");
            throw;
        }
    }
}
