using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Seeds;

public static class DdlSeed
{
    private static readonly (string Key, string Label, (string Code, string Label)[] Items)[] Catalogs =
    [
        ("PAYMENT_MODE", "Payment Mode",
        [
            ("CASH", "Cash"),
            ("CARD", "Card"),
            ("UPI", "UPI"),
            ("BANK_TRANSFER", "Bank Transfer"),
            ("CREDIT", "Credit"),
        ]),
        ("INVOICE_STATUS", "Invoice Status",
        [
            ("DRAFT", "Draft"),
            ("FINALIZED", "Finalized"),
            ("PAID", "Paid"),
            ("CANCELLED", "Cancelled"),
            ("PARTIALLY_PAID", "Partially Paid"),
        ]),
        ("WARRANTY_STATUS", "Warranty Status",
        [
            ("ACTIVE", "Active"),
            ("CLAIMED", "Claimed"),
            ("EXPIRED", "Expired"),
            ("VOIDED", "Voided"),
        ]),
        ("STOCK_MOVEMENT_TYPE", "Stock Movement Type",
        [
            ("PURCHASE", "Purchase"),
            ("SALE", "Sale"),
            ("RETURN", "Return"),
            ("ADJUSTMENT", "Adjustment"),
            ("TRANSFER", "Transfer"),
        ]),
        ("CUSTOMER_TYPE", "Customer Type",
        [
            ("RETAIL", "Retail"),
            ("WHOLESALE", "Wholesale"),
            ("DEALER", "Dealer"),
            ("ONLINE", "Online"),
        ]),
    ];

    public static async Task SeedAsync(PlatformDbContext db, CancellationToken ct = default)
    {
        var existingKeys = await db.DdlCatalogs.Select(c => c.Key).ToListAsync(ct);

        foreach (var (key, label, items) in Catalogs)
        {
            if (existingKeys.Contains(key))
                continue;

            var catalog = new DdlCatalog { Key = key, Label = label, IsActive = true };
            catalog.Items.Clear();

            for (int i = 0; i < items.Length; i++)
            {
                catalog.Items.Add(new DdlItem
                {
                    Code = items[i].Code,
                    Label = items[i].Label,
                    SortOrder = (i + 1) * 10,
                    IsActive = true
                });
            }

            db.DdlCatalogs.Add(catalog);
        }

        await db.SaveChangesAsync(ct);
    }
}
