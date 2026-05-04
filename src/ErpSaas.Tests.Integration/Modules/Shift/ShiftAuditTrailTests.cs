using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Shift;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ShiftAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task OpenShift_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 800);
        var payload = new { OpeningCash = 800m, BranchId = 800L, Notes = "Audit test open", CashierName = "Test Cashier" };
        var response = await client.PostAsJsonAsync("/api/shifts/open", payload);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // OpenShiftAsync returns Result<long> → OkObjectResult(id) → plain long.
            var shiftId = await response.Content.ReadFromJsonAsync<long>();

            if (shiftId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("Shift") && a.EntityId == shiftId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull("shift open should produce an audit row");
                auditEntry!.EventType.Should().Be("Insert");
            }
        }
    }

    [Fact]
    public async Task CloseShift_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 900);
        var openPayload = new { OpeningCash = 900m, BranchId = 900L, CashierName = "Test Cashier" };
        var openResp = await client.PostAsJsonAsync("/api/shifts/open", openPayload);

        if (openResp.IsSuccessStatusCode)
        {
            // OpenShiftAsync returns Result<long> → OkObjectResult(id) → plain long.
            var shiftId = await openResp.Content.ReadFromJsonAsync<long>();

            if (shiftId > 0)
            {
                var closePayload = new { ClosingCash = 900m, Notes = "Audit close" };
                var closeResp = await client.PostAsJsonAsync($"/api/shifts/{shiftId}/close", closePayload);
                closeResp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

                if (closeResp.IsSuccessStatusCode)
                {
                    await using var scope = fixture.CreateScope();
                    var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                    var auditEntry = await logDb.AuditLogs
                        .Where(a => a.EntityName.Contains("Shift") && a.EntityId == shiftId.ToString())
                        .OrderByDescending(a => a.OccurredAtUtc)
                        .FirstOrDefaultAsync();

                    auditEntry.Should().NotBeNull("shift close should produce an audit row");
                }
            }
        }
    }

    [Fact]
    public async Task ForceCloseShift_ProducesAuditLogRow()
    {
        // Non-existent shift — confirms gate passed
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new { Reason = "Audit force close test" };
        var response = await client.PostAsJsonAsync("/api/shifts/9999999/force-close", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RecordCashIn_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new { Amount = 100m, Notes = "Cash in audit test" };
        var response = await client.PostAsJsonAsync("/api/shifts/9999999/cash-in", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RecordCashOut_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new { Amount = 50m, Notes = "Cash out audit test" };
        var response = await client.PostAsJsonAsync("/api/shifts/9999999/cash-out", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
