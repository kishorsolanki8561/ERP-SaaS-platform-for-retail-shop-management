using ErpSaas.Tests.Integration.Fixtures;

namespace ErpSaas.Tests.Integration.Modules.CustomerPortal;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CustomerPortalAuditTrailTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;
    [Fact(Skip = "TODO: Testcontainers gate — after mutation assert AuditLog row exists")]
    public async Task AcceptOrder_CreatesAuditLogRow()
    {
        // Create order via portal, accept via staff endpoint, assert AuditLog has row
        // with EventType=Update, EntityType=OnlineOrder, ActorId=staff user id.
        await Task.CompletedTask;
    }

    [Fact(Skip = "TODO: Testcontainers gate")]
    public async Task RejectOrder_CreatesAuditLogRow()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "TODO: Testcontainers gate")]
    public async Task CreateInquiry_CreatesAuditLogRow()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "TODO: Testcontainers gate")]
    public async Task CloseInquiry_CreatesAuditLogRow()
    {
        await Task.CompletedTask;
    }
}
