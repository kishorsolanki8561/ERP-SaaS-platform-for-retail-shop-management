using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Billing;

/// <summary>
/// Verifies that invoices created in Shop A are never visible to Shop B,
/// and that mutations from Shop B cannot affect Shop A's invoices.
///
/// Full implementation requires <c>IntegrationTestFixture</c> with two
/// pre-onboarded shops — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class BillingTenantIsolationTests
{
    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task ListInvoices_ShopA_DoesNotReturnShopBInvoices()
    {
        // Arrange: create invoice in ShopA, authenticate as ShopB
        // Act: GET /api/billing/invoices
        // Assert: result does not contain ShopA invoice
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task GetInvoice_ShopBCannotReadShopAInvoice_Returns404()
    {
        // Arrange: create invoice in ShopA, get its ID
        // Act: GET /api/billing/invoices/{shopA_invoiceId} authenticated as ShopB
        // Assert: 404
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task CancelInvoice_ShopBCannotCancelShopAInvoice_Returns404()
    {
        // Arrange: create draft invoice in ShopA, get its ID
        // Act: POST /api/billing/invoices/{shopA_invoiceId}/cancel as ShopB
        // Assert: 404 (not 403 — we must not leak existence)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task AddLine_ShopBCannotAddLineToShopAInvoice_Returns404()
    {
        await Task.CompletedTask;
    }
}
