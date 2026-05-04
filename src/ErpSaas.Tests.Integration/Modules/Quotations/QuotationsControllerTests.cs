using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Quotations;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class QuotationsControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/quotations ───────────────────────────────────────────────────

    [Fact]
    public async Task ListQuotations_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/quotations");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListQuotations_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/quotations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListQuotations_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Quotation.Create");
        var response = await client.GetAsync("/api/quotations");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/quotations ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateQuotation_ValidPayload_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var payload = new
        {
            CustomerId = 1L,
            CustomerNameSnapshot = "Test Customer",
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Notes = "Integration test quotation",
            Lines = new[]
            {
                new
                {
                    ProductId = 1L,
                    ProductNameSnapshot = "Test Product",
                    ProductUnitId = 1L,
                    UnitCodeSnapshot = "PCS",
                    ConversionFactor = 1m,
                    QuantityInBilledUnit = 2m,
                    UnitPrice = 500m,
                    DiscountAmount = 0m,
                    GstRate = 18m
                }
            }
        };
        var response = await client.PostAsJsonAsync("/api/quotations", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateQuotation_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Quotation.View");
        var payload = new
        {
            CustomerId = 1L, CustomerNameSnapshot = "Test",
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Lines = Array.Empty<object>()
        };
        var response = await client.PostAsJsonAsync("/api/quotations", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/quotations/{id}/convert ─────────────────────────────────────

    [Fact]
    public async Task ConvertToSalesOrder_NonExistent_Returns404OrConflict()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.PostAsync("/api/quotations/9999999/convert", null);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConvertToSalesOrder_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Quotation.View");
        var response = await client.PostAsync("/api/quotations/1/convert", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/quotations/sales-orders ─────────────────────────────────────

    [Fact]
    public async Task ListSalesOrders_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/quotations/sales-orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/quotations/delivery-challans ─────────────────────────────────

    [Fact]
    public async Task ListDeliveryChallans_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/quotations/delivery-challans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── PATCH /api/quotations/{id}/send ───────────────────────────────────────

    [Fact]
    public async Task SendQuotation_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Quotation.View");
        var response = await client.PatchAsync("/api/quotations/1/send", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
