using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Purchasing;

/// <summary>
/// Integration tests for PurchasingController exercised through the full HTTP pipeline.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class PurchasingControllerTests(IntegrationTestFixture fixture)
{
    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<long> CreateSupplierAsync(HttpClient client)
    {
        var code = Guid.NewGuid().ToString("N")[..8];
        var resp = await client.PostAsJsonAsync("/api/purchasing/suppliers", new
        {
            Name = $"Test Supplier {code}",
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

    private async Task<long> CreateBillAsync(HttpClient client, long supplierId)
    {
        var now = DateTime.UtcNow;
        var resp = await client.PostAsJsonAsync("/api/purchasing/bills", new
        {
            SupplierId = supplierId,
            SupplierBillNumber = $"BILL-{Guid.NewGuid().ToString("N")[..6]}",
            PurchaseOrderId = (long?)null,
            BillDate = now,
            DueDate = now.AddDays(30),
            Notes = (string?)null,
            SubTotal = 1000m,
            TotalTaxAmount = 180m,
            GrandTotal = 1180m
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<long>();
    }

    // ── GET /api/purchasing/suppliers ────────────────────────────────────────

    [Fact]
    public async Task ListSuppliers_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/purchasing/suppliers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListSuppliers_WithPermission_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);

        var response = await client.GetAsync("/api/purchasing/suppliers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/purchasing/suppliers ────────────────────────────────────────

    [Fact]
    public async Task CreateSupplier_ValidRequest_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var code = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync("/api/purchasing/suppliers", new
        {
            Name = $"Supplier {code}",
            Code = code,
            GstNumber = (string?)null,
            PanNumber = (string?)null,
            Phone = "9876543210",
            Email = $"supplier-{code}@test.com",
            Address = "123 Test Street",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            OpeningBalance = 0m,
            Notes = "Integration test supplier"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateSupplier_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "Purchasing.View");

        var response = await client.PostAsJsonAsync("/api/purchasing/suppliers", new
        {
            Name = "No Perm Supplier",
            Code = "NOPERM1",
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

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/purchasing/bills ────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_ValidRequest_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var supplierId = await CreateSupplierAsync(client);

        var response = await client.PostAsJsonAsync("/api/purchasing/bills", new
        {
            SupplierId = supplierId,
            SupplierBillNumber = $"BILL-{Guid.NewGuid().ToString("N")[..6]}",
            PurchaseOrderId = (long?)null,
            BillDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Notes = "Integration test bill",
            SubTotal = 5000m,
            TotalTaxAmount = 900m,
            GrandTotal = 5900m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBill_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "Purchasing.View");

        var response = await client.PostAsJsonAsync("/api/purchasing/bills", new
        {
            SupplierId = 1L,
            SupplierBillNumber = "NOPERM",
            PurchaseOrderId = (long?)null,
            BillDate = DateTime.UtcNow,
            DueDate = (DateTime?)null,
            Notes = (string?)null,
            SubTotal = 100m,
            TotalTaxAmount = 18m,
            GrandTotal = 118m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/purchasing/bills/{id}/approve ───────────────────────────────

    [Fact]
    public async Task ApproveBill_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);

        var response = await client.PostAsync("/api/purchasing/bills/999999999/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApproveBill_ValidBill_Returns200()
    {
        // Approval auto-posts a voucher via IAutoVoucherService, which looks up
        // the COA accounts seeded by AccountingTenantSeeder. Seed before approving.
        await fixture.SeedTenantDataAsync(shopId: 1);

        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var supplierId = await CreateSupplierAsync(client);
        var billId = await CreateBillAsync(client, supplierId);

        var response = await client.PostAsync($"/api/purchasing/bills/{billId}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
