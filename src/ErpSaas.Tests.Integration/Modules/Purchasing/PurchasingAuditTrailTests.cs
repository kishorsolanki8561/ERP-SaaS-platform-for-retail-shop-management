using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Purchasing;

/// <summary>
/// Verifies that every Purchasing mutation produces a correct AuditLog row.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "AuditTrail")]
public class PurchasingAuditTrailTests(IntegrationTestFixture fixture)
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<long> CreateSupplierAsync(HttpClient client)
    {
        var code = Guid.NewGuid().ToString("N")[..8];
        var resp = await client.PostAsJsonAsync("/api/purchasing/suppliers", new
        {
            Name = $"Audit Supplier {code}",
            Code = code,
            GstNumber = (string?)null,
            PanNumber = (string?)null,
            Phone = (string?)null,
            Email = (string?)null,
            Address = (string?)null,
            City = (string?)null,
            State = (string?)null,
            Pincode = (string?)null,
            OpeningBalance = 0m,
            Notes = (string?)null
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<long>();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSupplier_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var before = DateTime.UtcNow.AddSeconds(-1);

        var code = Guid.NewGuid().ToString("N")[..8];
        var resp = await client.PostAsJsonAsync("/api/purchasing/suppliers", new
        {
            Name = $"Audit Supplier {code}",
            Code = code,
            GstNumber = (string?)null,
            PanNumber = (string?)null,
            Phone = (string?)null,
            Email = (string?)null,
            Address = (string?)null,
            City = (string?)null,
            State = (string?)null,
            Pincode = (string?)null,
            OpeningBalance = 0m,
            Notes = (string?)null
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var supplierId = await resp.Content.ReadFromJsonAsync<long>();

        // Verify AuditLog
        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();

        var auditRow = await logDb.AuditLogs
            .Where(a => a.EntityName == "Supplier"
                     && a.EntityId == supplierId.ToString()
                     && a.OccurredAtUtc >= before)
            .FirstOrDefaultAsync();

        auditRow.Should().NotBeNull("creating a Supplier must produce an AuditLog row");
    }
}
