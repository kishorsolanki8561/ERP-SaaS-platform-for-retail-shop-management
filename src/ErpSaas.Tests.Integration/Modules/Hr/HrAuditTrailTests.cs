using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Hr;

/// <summary>
/// Verifies that HR mutations produce <c>AuditLog</c> rows in LogDb.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "AuditTrail")]
public sealed class HrAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CreateEmployee_ProducesAuditLogRow()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var client = fixture.CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var beforeUtc = DateTime.UtcNow.AddSeconds(-1);

        // ── Act: create an employee ───────────────────────────────────────────
        var createResp = await client.PostAsJsonAsync("/api/employees", new
        {
            FirstName         = $"Audit{suffix}",
            LastName          = "AuditTrail",
            Phone             = (string?)null,
            Email             = $"audit-emp-{suffix}@test.local",
            DateOfBirth       = new DateTime(1990, 6, 15),
            DateOfJoining     = new DateTime(2024, 1, 1),
            Designation       = "QA Engineer",
            Department        = "Technology",
            BasicSalary       = 40000m,
            BankAccountNumber = (string?)null,
            BankIfsc          = (string?)null,
            PanNumber         = (string?)null,
            LinkedUserId      = (long?)null
        });

        createResp.IsSuccessStatusCode.Should().BeTrue("employee creation must succeed for audit test");

        // ── Assert: AuditLog row for Employee was written ─────────────────────
        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();

        var auditRow = await logDb.AuditLogs
            .Where(a => a.EntityName == "Employee" && a.OccurredAtUtc >= beforeUtc)
            .FirstOrDefaultAsync();

        auditRow.Should().NotBeNull(
            "AuditSaveChangesInterceptor must write an AuditLog row when an Employee is created");
        auditRow!.EventType.Should().NotBeNullOrWhiteSpace();
    }
}
