using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Transport;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class TransportAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CreateProvider_CreatesAuditLogEntry()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            Name = $"AuditProvider-{uid}",
            ContactName = "Audit Contact",
            ContactPhone = "7777777777",
            GstNumber = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/transport/providers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // CreateProviderAsync returns Result<long> → OkObjectResult(id) → plain long.
        var providerId = await response.Content.ReadFromJsonAsync<long>();
        providerId.Should().BeGreaterThan(0);

        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var auditEntry = await logDb.AuditLogs
            .Where(a => a.EntityName.Contains("Transport") && a.EntityId == providerId.ToString())
            .FirstOrDefaultAsync();

        auditEntry.Should().NotBeNull();
        auditEntry!.EventType.Should().Be("Insert");
    }

    [Fact]
    public async Task CreateVehicle_CreatesAuditLogEntry()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            LicensePlate = $"MH-02-{uid[..4]}",
            Model = "Audit Van",
            MaxLoadKg = 2000m,
            TransportProviderId = (long?)null,
            DriverName = (string?)null,
            DriverPhone = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/transport/vehicles", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // CreateVehicleAsync returns Result<long> → OkObjectResult(id) → plain long.
            var vehicleId = await response.Content.ReadFromJsonAsync<long>();

            if (vehicleId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("Vehicle") && a.EntityId == vehicleId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull();
            }
        }
    }
}
