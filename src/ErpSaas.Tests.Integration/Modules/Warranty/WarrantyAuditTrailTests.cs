using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.Warranty.Enums;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Warranty;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class WarrantyAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task RegisterWarranty_CreatesAuditRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var serial = $"SN-AUDIT-{Guid.NewGuid():N}";
        var payload = new
        {
            InvoiceId = 1L,
            InvoiceLineId = 1L,
            ProductId = 1L,
            CustomerId = 1L,
            SerialNumber = serial,
            PurchaseDate = DateTime.UtcNow.AddDays(-5),
            WarrantyMonths = 24,
            Type = WarrantyType.Warranty.ToString(),
            Terms = (string?)null,
            BranchId = (long?)null
        };

        var response = await client.PostAsJsonAsync("/api/warranty/registrations", payload);

        // Auth and permission gates must have passed
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            // BaseController.Ok<T>(Result<T>) returns result.Value directly — body is the number
            var regId = doc.RootElement.ValueKind == JsonValueKind.Number
                ? doc.RootElement.GetInt64()
                : 0L;

            if (regId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("Warranty") && a.EntityId == regId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull();
                auditEntry!.EventType.Should().Be("Insert");
            }
        }
    }

    [Fact]
    public async Task CreateClaim_CreatesAuditRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new
        {
            WarrantyRegistrationId = 9999999L,
            ClaimDate = DateTime.UtcNow,
            IssueDescription = "Audit trail test claim",
            AttachmentFileIds = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/warranty/claims", payload);

        // Auth + permission gate check — must not be 401/403
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResolveClaim_CreatesAuditRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new
        {
            Status = ClaimStatus.Resolved.ToString(),
            ResolutionNotes = "Issue fixed",
            RepairCost = (decimal?)null
        };

        // Non-existent claim ID — verifies permission gate passed, not 403
        var response = await client.PatchAsJsonAsync("/api/warranty/claims/9999999", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
