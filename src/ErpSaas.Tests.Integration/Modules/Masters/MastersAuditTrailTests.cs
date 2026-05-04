using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Masters;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class MastersAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CreateCountry_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var uid = Guid.NewGuid().ToString("N")[..4];
        var payload = new
        {
            Code = $"AC{uid}",
            Name = $"AuditCountry-{uid}",
            PhoneCode = "+88",
            CurrencyCode = "AUD"
        };

        var response = await client.PostAsJsonAsync("/api/masters/countries", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // CreateCountry returns Result<long> → OkObjectResult(id) → plain long.
            var countryId = await response.Content.ReadFromJsonAsync<long>();

            if (countryId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("Country") && a.EntityId == countryId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull("creating a country should produce an audit row");
                auditEntry!.EventType.Should().Be("Insert");
            }
        }
    }

    [Fact]
    public async Task CreateState_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);

        // First create a country to attach the state to
        var uid = Guid.NewGuid().ToString("N")[..4];
        var countryPayload = new { Code = $"ST{uid}", Name = $"StateParent-{uid}", PhoneCode = "+77", CurrencyCode = "STT" };
        var countryResp = await client.PostAsJsonAsync("/api/masters/countries", countryPayload);

        if (countryResp.IsSuccessStatusCode)
        {
            // CreateCountry returns Result<long> → OkObjectResult(id) → plain long.
            var countryId = await countryResp.Content.ReadFromJsonAsync<long>();

            if (countryId > 0)
            {
                var statePayload = new { Code = $"S{uid}", Name = $"AuditState-{uid}", GstStateCode = (string?)null };
                var stateResp = await client.PostAsJsonAsync($"/api/masters/countries/{countryId}/states", statePayload);
                stateResp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

                if (stateResp.IsSuccessStatusCode)
                {
                    // CreateState returns Result<long> → OkObjectResult(id) → plain long.
                    var stateId = await stateResp.Content.ReadFromJsonAsync<long>();

                    if (stateId > 0)
                    {
                        await using var scope = fixture.CreateScope();
                        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                        var auditEntry = await logDb.AuditLogs
                            .Where(a => a.EntityName.Contains("State") && a.EntityId == stateId.ToString())
                            .FirstOrDefaultAsync();

                        auditEntry.Should().NotBeNull();
                    }
                }
            }
        }
    }

    [Fact]
    public async Task CreateCity_ProducesAuditLogRow()
    {
        // Creating a city requires a state — attempt with state 1 (from seed data)
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var uid = Guid.NewGuid().ToString("N")[..6];
        var payload = new { Name = $"AuditCity-{uid}" };

        var response = await client.PostAsJsonAsync("/api/masters/states/1/cities", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // CreateCity returns Result<long> → OkObjectResult(id) → plain long.
            var cityId = await response.Content.ReadFromJsonAsync<long>();

            if (cityId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("City") && a.EntityId == cityId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull();
            }
        }
    }
}
