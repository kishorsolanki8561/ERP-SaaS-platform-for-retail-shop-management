using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Purchasing;

/// <summary>
/// Verifies that Purchasing data from Shop A is never visible to Shop B.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "TenantIsolation")]
public class PurchasingTenantIsolationTests(IntegrationTestFixture fixture)
{
    private const long ShopA = 1L;
    private const long ShopB = 2L;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<long> CreateSupplierAsync(long shopId)
    {
        var client = fixture.CreateAuthenticatedClient(shopId: shopId);
        var code = Guid.NewGuid().ToString("N")[..8];

        var resp = await client.PostAsJsonAsync("/api/purchasing/suppliers", new
        {
            Name = $"Isolation Supplier {code}",
            Code = code,
            GstNumber = (string?)null,
            PanNumber = (string?)null,
            Phone = (string?)null,
            Email = (string?)null,
            Address = (string?)null,
            City = (string?)null,
            State = (string?)null,
            Pincode = (string?)null,
            OpeningBalance = 0m,
            Notes = (string?)null
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<long>();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSuppliers_ShopA_DoesNotReturnShopBSuppliers()
    {
        // Arrange: create a supplier in Shop B with a unique name
        var uniqueCode = $"ISO{Guid.NewGuid().ToString("N")[..6]}";
        var clientB = fixture.CreateAuthenticatedClient(shopId: ShopB);
        var createResp = await clientB.PostAsJsonAsync("/api/purchasing/suppliers", new
        {
            Name = $"ShopB Supplier {uniqueCode}",
            Code = uniqueCode,
            GstNumber = (string?)null,
            PanNumber = (string?)null,
            Phone = (string?)null,
            Email = (string?)null,
            Address = (string?)null,
            City = (string?)null,
            State = (string?)null,
            Pincode = (string?)null,
            OpeningBalance = 0m,
            Notes = (string?)null
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: list suppliers as Shop A
        var clientA = fixture.CreateAuthenticatedClient(shopId: ShopA);
        var response = await clientA.GetAsync("/api/purchasing/suppliers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var suppliers = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        var codes = suppliers!.Select(s => s.GetProperty("code").GetString()).ToList();

        codes.Should().NotContain(uniqueCode,
            "suppliers created in Shop B must not be visible to Shop A");
    }

    [Fact]
    public async Task GetSupplier_ShopBCannotReadShopASupplier_Returns404()
    {
        // Arrange: create a supplier in Shop A
        var supplierId = await CreateSupplierAsync(ShopA);

        // Act: try to GET it as Shop B — the global query filter should hide it
        var clientB = fixture.CreateAuthenticatedClient(shopId: ShopB);

        // Suppliers list endpoint doesn't expose individual supplier by ID in the controller,
        // so we verify isolation through the bill creation path (supplier not found for wrong shop)
        var response = await clientB.PostAsJsonAsync("/api/purchasing/bills", new
        {
            SupplierId = supplierId,  // Shop A supplier — invisible to Shop B
            SupplierBillNumber = "XSHOP-BILL",
            PurchaseOrderId = (long?)null,
            BillDate = DateTime.UtcNow,
            DueDate = (DateTime?)null,
            Notes = (string?)null,
            SubTotal = 100m,
            TotalTaxAmount = 18m,
            GrandTotal = 118m
        });

        // The bill creation must fail with 404 because the supplier is not visible to Shop B
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Shop B must not be able to use Shop A's supplier");
    }
}
