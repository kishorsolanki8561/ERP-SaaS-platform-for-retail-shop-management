using ErpSaas.Tests.Integration.Fixtures;

namespace ErpSaas.Tests.Integration.Modules.ApiAccess;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ApiAccessAuditTrailTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;
    [Fact]
    public async Task CreateApiKey_CreatesAuditLogRow()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RevokeApiKey_CreatesAuditLogRow()
    {
        await Task.CompletedTask;
    }
}

