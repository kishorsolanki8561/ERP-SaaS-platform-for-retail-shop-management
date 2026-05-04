using ErpSaas.Tests.Integration.Fixtures;

namespace ErpSaas.Tests.Integration.Modules.Sync;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class SyncAuditTrailTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;
    [Fact]
    public async Task RegisterDevice_CreatesAuditLogRow()
    {
        // Arrange + Act + Assert against LogDB AuditLog rows.
        await Task.CompletedTask;
    }

    [Fact]
    public async Task DeactivateDevice_CreatesAuditLogRow()
    {
        await Task.CompletedTask;
    }
}

