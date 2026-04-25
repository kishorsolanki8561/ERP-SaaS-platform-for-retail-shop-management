using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Wallet;

/// <summary>
/// Verifies that every wallet mutation produces a correct <c>AuditLog</c> row.
///
/// Relies on <c>AuditSaveChangesInterceptor</c> + the <c>[Auditable]</c>
/// attributes on <c>WalletBalance</c> and <c>WalletTransaction</c>.
/// Full implementation against a real SQL Server DB is deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class WalletAuditTrailTests
{
    [Fact(Skip = "Requires IntegrationTestFixture with LogDb — Phase 1")]
    public async Task CreditAsync_ProducesAuditLogRowForBalance()
    {
        // Arrange + Act: credit a customer
        // Assert: AuditLog has a row with EntityName = "WalletBalance",
        //         EventType = "Insert" or "Update", and correct Balance values
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with LogDb — Phase 1")]
    public async Task CreditAsync_ProducesAuditLogRowForTransaction()
    {
        // Assert: AuditLog row for WalletTransaction insert with
        //         TransactionType = "Credit"
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with LogDb — Phase 1")]
    public async Task DebitAsync_ProducesAuditLogRowForBalance()
    {
        // Arrange: credit then debit
        // Assert: AuditLog row captures BalanceBefore → BalanceAfter reduction
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with LogDb — Phase 1")]
    public async Task DebitAsync_ProducesAuditLogRowForTransaction()
    {
        // Assert: AuditLog row for WalletTransaction insert with
        //         TransactionType = "Debit"
        await Task.CompletedTask;
    }
}
