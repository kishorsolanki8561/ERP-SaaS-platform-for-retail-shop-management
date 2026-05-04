using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Sync;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class SyncSubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task RegisterDevice_FeatureDisabled_Returns402()
    {
        var client = fixture.CreateAuthenticatedClient(
            permissions: ["Device.Register"],
            features: []);

        var response = await client.PostAsJsonAsync("/api/devices/register", new
        {
            deviceId = "DEV-001", branchId = 1, assignedUserId = 1,
            deviceTypeCode = "DesktopPos", platformInfo = "Win", appVersion = "1.0",
        });

        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task RegisterDevice_FeatureEnabled_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(
            permissions: ["Device.Register"],
            features: ["offline_mode"]);

        var response = await client.PostAsJsonAsync("/api/devices/register", new
        {
            deviceId = "DEV-001", branchId = 1, assignedUserId = 1,
            deviceTypeCode = "DesktopPos", platformInfo = "Win", appVersion = "1.0",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

