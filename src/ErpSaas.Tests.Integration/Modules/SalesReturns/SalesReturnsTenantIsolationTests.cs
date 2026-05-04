using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.SalesReturns;

/// <summary>
/// Verifies that SalesReturns entities created in Shop A are never visible
/// to Shop B, and that mutations from Shop B cannot affect Shop A's data.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "TenantIsolation")]
public sealed class SalesReturnsTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ApproveSalesReturn_ShopBCannotApproveShopASalesReturn_Returns404()
    {
        // ── Arrange: create a SalesReturn as Shop A ───────────────────────────
        var shopAClient = fixture.CreateAuthenticatedClient(shopId: 1);

        var body = new
        {
            InvoiceId    = 1L,
            ReturnDate   = DateTime.UtcNow.Date,
            RefundMethod = "CreditNote",
            Reason       = (string?)null,
            Lines        = Array.Empty<object>()
        };

        var createResponse = await shopAClient.PostAsJsonAsync("/api/sales-returns", body);
        createResponse.IsSuccessStatusCode.Should().BeTrue("setup: Shop A must be able to create a sales return");

        // BaseController.Ok<T>(Result<T>) returns OkObjectResult(result.Value) — raw long.
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var returnId   = createJson.GetInt64();

        // ── Act: attempt to approve as Shop B (different shopId) ─────────────
        var shopBClient  = fixture.CreateAuthenticatedClient(shopId: 2);
        var approveResp  = await shopBClient.PostAsync($"/api/sales-returns/{returnId}/approve", null);

        // ── Assert: Shop B cannot see Shop A's SalesReturn ───────────────────
        approveResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the global query filter on ShopId must prevent Shop B from finding Shop A's sales return");
    }
}
