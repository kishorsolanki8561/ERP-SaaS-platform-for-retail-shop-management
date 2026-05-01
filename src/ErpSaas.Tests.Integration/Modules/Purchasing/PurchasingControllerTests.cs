namespace ErpSaas.Tests.Integration.Modules.Purchasing;

[Trait("Category", "Integration")]
public class PurchasingControllerTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListSuppliers_Unauthenticated_Returns401() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListSuppliers_WithPermission_Returns200AndList() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateSupplier_ValidRequest_Returns200WithId() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateSupplier_DuplicateCode_Returns409() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateSupplier_WithoutPermission_Returns403() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreatePurchaseOrder_ValidRequest_Returns200WithId() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreatePurchaseOrder_SupplierNotFound_Returns404() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task SendPurchaseOrder_DraftPo_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task SendPurchaseOrder_AlreadySent_Returns409() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ReceivePurchaseOrder_SentPo_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CancelPurchaseOrder_DraftPo_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateBill_ValidRequest_Returns200WithId() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ApproveBill_DraftBill_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PayBill_FullPayment_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PayBill_Overpayment_Returns409() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PayBill_WithoutPermission_Returns403() => await Task.CompletedTask;
}
