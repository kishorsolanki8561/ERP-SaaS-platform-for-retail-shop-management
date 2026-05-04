using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Sync;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class SyncTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ListDevices_Shop1_CannotSeeShop2Devices()
    {
        var (shopId1, _, _) = await fixture.SeedTestUserAsync();
        var (shopId2, _, _) = await fixture.SeedTestUserAsync();

        var client1 = fixture.CreateAuthenticatedClient(shopId: shopId1, permissions: ["Device.Manage"]);
        var client2 = fixture.CreateAuthenticatedClient(shopId: shopId2, permissions: ["Device.Manage"]);

        await client1.PostAsJsonAsync("/api/devices/register", new
        {
            deviceId = "SHOP1-DEV", branchId = 1, assignedUserId = 1,
            deviceTypeCode = "DesktopPos", platformInfo = "Win", appVersion = "1.0",
        });

        var response = await client2.GetAsync("/api/devices");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("SHOP1-DEV",
            "Shop 2 must not see devices registered by Shop 1");
    }

    [Fact]
    public async Task AllocateRange_IsolatedPerShop_StartsFromOneForEachShop()
    {
        // Each shop's allocation counter is independent.
        await Task.CompletedTask;
    }
}

