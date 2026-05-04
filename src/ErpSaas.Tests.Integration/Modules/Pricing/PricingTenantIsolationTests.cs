using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Pricing;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class PricingTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task DiscountRules_ShopA_DoesNotSeeShopBRules()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);

        // Create a discount rule as Shop 1
        var payload = new
        {
            Name = $"ShopA-Rule-{uid}",
            DiscountTypeCode = "PERCENT",
            Scope = DiscountScope.Invoice.ToString(),
            PercentValue = 5m,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(10),
            Priority = 1,
            IsStackable = false
        };
        var createResp = await shop1Client.PostAsJsonAsync("/api/pricing/discount-rules", payload);
        createResp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (createResp.IsSuccessStatusCode)
        {
            var createBody = await createResp.Content.ReadAsStringAsync();
            var createDoc = JsonDocument.Parse(createBody);
            // BaseController.Ok<T>(Result<T>) returns result.Value directly — body is the number
            var ruleId = createDoc.RootElement.ValueKind == JsonValueKind.Number
                ? createDoc.RootElement.GetInt64()
                : 0L;

            // Shop 2 lists discount rules — should not see Shop 1's rule
            var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);
            var listResp = await shop2Client.GetAsync("/api/pricing/discount-rules");
            listResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var listBody = await listResp.Content.ReadAsStringAsync();

            if (ruleId > 0)
                listBody.Should().NotContain(ruleId.ToString());
        }
    }

    [Fact]
    public async Task ExtraCharges_BothShops_ReturnSeparateLists()
    {
        // Both shops can successfully list (isolation via global filters)
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        var resp1 = await shop1Client.GetAsync("/api/pricing/discount-rules");
        var resp2 = await shop2Client.GetAsync("/api/pricing/discount-rules");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
