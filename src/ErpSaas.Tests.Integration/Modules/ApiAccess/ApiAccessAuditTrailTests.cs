using ErpSaas.Tests.Integration.Fixtures;

namespace ErpSaas.Tests.Integration.Modules.ApiAccess;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ApiAccessAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact(Skip = "Testcontainers gate pending")]
    public async Task CreateApiKey_CreatesAuditLogRow()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task RevokeApiKey_CreatesAuditLogRow()
    {
        await Task.CompletedTask;
    }
}
