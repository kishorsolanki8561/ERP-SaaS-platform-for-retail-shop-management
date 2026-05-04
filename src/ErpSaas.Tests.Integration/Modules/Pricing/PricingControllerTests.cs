using System.Net;
using System.Net.Http.Json;
using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Pricing;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class PricingControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/pricing/discount-rules ──────────────────────────────────────

    [Fact]
    public async Task ListDiscountRules_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/pricing/discount-rules");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListDiscountRules_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/pricing/discount-rules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListDiscountRules_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Pricing.Manage");
        var response = await client.GetAsync("/api/pricing/discount-rules");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/pricing/discount-rules ─────────────────────────────────────

    [Fact]
    public async Task CreateDiscountRule_ValidPayload_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            Name = $"Rule-{uid}",
            DiscountTypeCode = "PERCENT",
            Scope = DiscountScope.Invoice.ToString(),
            ProductId = (long?)null,
            CategoryId = (long?)null,
            CustomerTypeId = (long?)null,
            PercentValue = 10m,
            FixedValue = (decimal?)null,
            BuyQty = (int?)null,
            GetQty = (int?)null,
            MinInvoiceAmount = (decimal?)null,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Priority = 1,
            IsStackable = false
        };
        var response = await client.PostAsJsonAsync("/api/pricing/discount-rules", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateDiscountRule_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Pricing.View");
        var payload = new
        {
            Name = "Test", DiscountTypeCode = "PERCENT",
            Scope = DiscountScope.Invoice.ToString(),
            PercentValue = 5m,
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(10),
            Priority = 1, IsStackable = false
        };
        var response = await client.PostAsJsonAsync("/api/pricing/discount-rules", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/pricing/extra-charges ──────────────────────────────────────

    [Fact]
    public async Task CreateExtraCharge_ValidPayload_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            Name = $"Charge-{uid}",
            Type = ChargeType.FixedAmount.ToString(),
            Value = 50m,
            IsTaxable = false,
            GstRate = (decimal?)null
        };
        var response = await client.PostAsJsonAsync("/api/pricing/extra-charges", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateExtraCharge_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Pricing.View");
        var payload = new { Name = "Test", Type = ChargeType.FixedAmount.ToString(), Value = 10m, IsTaxable = false };
        var response = await client.PostAsJsonAsync("/api/pricing/extra-charges", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/pricing/offers ──────────────────────────────────────────────

    [Fact]
    public async Task CreateOffer_ValidPayload_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            Code = $"OFF-{uid}",
            Name = $"Offer-{uid}",
            Type = OfferType.Combo.ToString(),
            RulesJson = (string?)null,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        var response = await client.PostAsJsonAsync("/api/pricing/offers", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
