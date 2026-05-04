using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ErpSaas.Tests.Integration.Modules.Crm;

/// <summary>
/// Verifies that every Customer mutation produces a correct AuditLog row.
/// Customer is marked [Auditable("Customer.Mutated")] so the
/// AuditSaveChangesInterceptor must write an AuditLog entry on every save.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Module", "Crm")]
public class CrmAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CreateCustomer_ProducesAuditLogRow()
    {
        // Arrange
        var (shopId, _, _) = await fixture.SeedTestUserAsync();
        var client = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var dto = new
        {
            DisplayName = $"Audit Test Customer {suffix}",
            CustomerType = "RETAIL",
            Email = (string?)null,
            Phone = (string?)null,
            GstNumber = (string?)null,
            CreditLimit = 0m,
            GroupId = (long?)null
        };

        // Act: create a customer via the API
        var response = await client.PostAsJsonAsync("/api/crm/customers", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerId = await response.Content.ReadFromJsonAsync<long>();
        customerId.Should().BeGreaterThan(0);

        // Assert: AuditLog has a row for the Customer entity in this shop
        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();

        var hasAuditRow = await logDb.AuditLogs
            .Where(a => a.ShopId == shopId && a.EntityName == "Customer")
            .AnyAsync();

        hasAuditRow.Should().BeTrue(
            because: $"creating a Customer (id={customerId}) must produce an AuditLog row " +
                     "via the [Auditable] interceptor");
    }
}
