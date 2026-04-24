using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Billing;

/// <summary>
/// Verifies that every billing mutation produces a correct <c>AuditLog</c> row.
///
/// Relies on <c>AuditSaveChangesInterceptor</c> + the <c>[Auditable]</c> attribute
/// on <c>Invoice</c>.  Full implementation against a real SQL Server DB is
/// deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class BillingAuditTrailTests
{
    [Fact(Skip = "Requires IntegrationTestFixture with LogDb — Phase 1")]
    public async Task CreateDraftInvoice_ProducesAuditLogRow()
    {
        // Arrange + Act: create a draft invoice
        // Assert: AuditLog has one row with EventType = "Invoice" and EntityName = "Invoice"
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with LogDb — Phase 1")]
    public async Task FinalizeInvoice_ProducesAuditLogRow()
    {
        // Assert: AuditLog row captures old Status = "Draft", new Status = "Finalized"
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with LogDb — Phase 1")]
    public async Task CancelInvoice_ProducesAuditLogRow()
    {
        // Assert: AuditLog row captures old Status, new Status = "Cancelled", and reason in Notes
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with LogDb — Phase 1")]
    public async Task AddLine_ProducesAuditLogRow()
    {
        // Assert: AuditLog row for InvoiceLine insert
        await Task.CompletedTask;
    }
}
