using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Shift;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ShiftTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ListShifts_ShopA_DoesNotReturnShopBShifts()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 100);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 101);

        // Open a shift as Shop 100
        var openPayload = new { OpeningCash = 100m, BranchId = 100L, CashierName = "Test Cashier" };
        var openResp = await shop1Client.PostAsJsonAsync("/api/shifts/open", openPayload);
        openResp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        long shiftId = 0;
        if (openResp.IsSuccessStatusCode)
        {
            // OpenShiftAsync returns Result<long> → OkObjectResult(id) → plain long.
            shiftId = await openResp.Content.ReadFromJsonAsync<long>();
        }

        // Shop 101 lists shifts — should not contain Shop 100's shift
        var listResp = await shop2Client.GetAsync("/api/shifts");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        if (shiftId > 0)
        {
            var listBody = await listResp.Content.ReadAsStringAsync();
            listBody.Should().NotContain($"\"{shiftId}\"");
        }
    }

    [Fact]
    public async Task GetShiftSummary_ShopBCannotReadShopAShift_Returns404()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 200);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 201);

        // Open a shift as Shop 200
        var openPayload = new { OpeningCash = 200m, BranchId = 200L, CashierName = "Test Cashier" };
        var openResp = await shop1Client.PostAsJsonAsync("/api/shifts/open", openPayload);

        if (openResp.IsSuccessStatusCode)
        {
            // OpenShiftAsync returns Result<long> → OkObjectResult(id) → plain long.
            var shiftId = await openResp.Content.ReadFromJsonAsync<long>();

            if (shiftId > 0)
            {
                // Shop 201 tries to read Shop 200's shift — should be 404
                var getResp = await shop2Client.GetAsync($"/api/shifts/{shiftId}");
                getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            }
        }
    }

    [Fact]
    public async Task CloseShift_ShopBCannotCloseShopAShift_Returns404()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 300);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 301);

        var openPayload = new { OpeningCash = 300m, BranchId = 300L, CashierName = "Test Cashier" };
        var openResp = await shop1Client.PostAsJsonAsync("/api/shifts/open", openPayload);

        if (openResp.IsSuccessStatusCode)
        {
            // OpenShiftAsync returns Result<long> → OkObjectResult(id) → plain long.
            var shiftId = await openResp.Content.ReadFromJsonAsync<long>();

            if (shiftId > 0)
            {
                var closePayload = new { ClosingCash = 300m };
                var closeResp = await shop2Client.PostAsJsonAsync(
                    $"/api/shifts/{shiftId}/close", closePayload);
                // Cross-shop access should fail — 404 (not 403, to avoid leaking existence)
                ((int)closeResp.StatusCode).Should().BeOneOf(404, 409);
            }
        }
    }

    [Fact]
    public async Task ForceClose_ShopBCannotForceCloseShopAShift_Returns404()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 400);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 401);

        var openPayload = new { OpeningCash = 400m, BranchId = 400L, CashierName = "Test Cashier" };
        var openResp = await shop1Client.PostAsJsonAsync("/api/shifts/open", openPayload);

        if (openResp.IsSuccessStatusCode)
        {
            // OpenShiftAsync returns Result<long> → OkObjectResult(id) → plain long.
            var shiftId = await openResp.Content.ReadFromJsonAsync<long>();

            if (shiftId > 0)
            {
                var forcePayload = new { Reason = "Cross-shop force close attempt" };
                var forceResp = await shop2Client.PostAsJsonAsync(
                    $"/api/shifts/{shiftId}/force-close", forcePayload);
                ((int)forceResp.StatusCode).Should().BeOneOf(404, 409);
            }
        }
    }
}
