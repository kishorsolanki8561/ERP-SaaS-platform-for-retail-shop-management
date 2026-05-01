using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Modules.Accounting.Services;

namespace ErpSaas.Tests.Integration.Modules.Accounting;

/// <summary>
/// Integration tests for AccountingController exercised through the full HTTP pipeline.
/// Stubs pending IntegrationTestFixture wiring for Phase 2.
/// </summary>
[Trait("Category", "Integration")]
public class AccountingControllerTests
{
    // ── GET /api/accounting/account-groups ────────────────────────────────────

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListAccountGroups_Unauthenticated_Returns401() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListAccountGroups_WithPermission_Returns200AndList() => await Task.CompletedTask;

    // ── GET /api/accounting/accounts ─────────────────────────────────────────

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListAccounts_WithoutPermission_Returns403() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListAccounts_WithPermission_Returns200AndPagedList() => await Task.CompletedTask;

    // ── POST /api/accounting/accounts ────────────────────────────────────────

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateAccount_ValidRequest_Returns200WithId() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateAccount_DuplicateCode_Returns409() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateAccount_WithoutPermission_Returns403() => await Task.CompletedTask;

    // ── POST /api/accounting/vouchers ────────────────────────────────────────

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateVoucher_BalancedEntries_Returns200WithId() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateVoucher_ImbalancedEntries_Returns409() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateVoucher_WithoutPermission_Returns403() => await Task.CompletedTask;

    // ── POST /api/accounting/vouchers/{id}/post ───────────────────────────────

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PostVoucher_DraftVoucher_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PostVoucher_AlreadyPosted_Returns409() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PostVoucher_NotFound_Returns404() => await Task.CompletedTask;

    // ── POST /api/accounting/vouchers/{id}/reverse ────────────────────────────

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ReverseVoucher_PostedVoucher_Returns200() => await Task.CompletedTask;

    // ── POST /api/accounting/expenses ────────────────────────────────────────

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateExpense_ValidRequest_Returns200WithId() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateExpense_WithoutPermission_Returns403() => await Task.CompletedTask;

    // ── POST /api/accounting/financial-years/{id}/close ──────────────────────

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CloseFinancialYear_OpenYear_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CloseFinancialYear_FeatureOff_Returns402() => await Task.CompletedTask;
}
