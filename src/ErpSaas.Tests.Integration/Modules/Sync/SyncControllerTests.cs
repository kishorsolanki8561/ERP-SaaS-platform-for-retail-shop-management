using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Sync;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class SyncControllerTests(IntegrationTestFixture fixture)
{
    // ── POST /api/devices/register ────────────────────────────────────────────

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task RegisterDevice_WithoutAuth_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/devices/register", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task RegisterDevice_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(permissions: ["Device.Register"]);
        var response = await client.PostAsJsonAsync("/api/devices/register", new
        {
            deviceId = "INT-DEV-001",
            branchId = 1,
            assignedUserId = 1,
            deviceTypeCode = "DesktopPos",
            platformInfo = "Windows 11",
            appVersion = "1.0.0",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/devices ──────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task ListDevices_WithoutPermission_Returns403()
    {
        var client = fixture.CreateAuthenticatedClient(permissions: []);
        var response = await client.GetAsync("/api/devices");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/sync/commands ───────────────────────────────────────────────

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task ProcessCommands_ValidBatch_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(permissions: ["Device.Register"]);
        var response = await client.PostAsJsonAsync("/api/sync/commands", new
        {
            commands = new[] {
                new { clientCommandId = Guid.NewGuid(), deviceId = "DEV-001", commandType = "CreateInvoice",
                      payloadJson = "{}", clientTimestampUtc = DateTime.UtcNow }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/invoice-ranges/allocate ─────────────────────────────────────

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task AllocateRange_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(permissions: ["Device.Register"]);
        var response = await client.PostAsJsonAsync("/api/invoice-ranges/allocate", new
        {
            deviceId = "DEV-001",
            branchId = 1,
            count = 50,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
