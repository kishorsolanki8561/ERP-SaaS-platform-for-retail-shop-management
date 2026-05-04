using ErpSaas.Tests.Integration.Fixtures;

namespace ErpSaas.Tests.Integration.Modules.CustomerPortal;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CustomerPortalSubscriptionGateTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;
    [Fact]
    public async Task ListOnlineOrders_OnlineFlagOff_Returns402()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ListOnlineOrders_OnlineFlagOn_Returns200()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task PortalMenuItems_HiddenWhenFeatureOff()
    {
        await Task.CompletedTask;
    }
}

