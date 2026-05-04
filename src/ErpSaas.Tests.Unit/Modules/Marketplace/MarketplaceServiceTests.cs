using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Marketplace.Connectors;
using ErpSaas.Modules.Marketplace.Entities;
using ErpSaas.Modules.Marketplace.Enums;
using ErpSaas.Modules.Marketplace.Infrastructure;
using ErpSaas.Modules.Marketplace.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Marketplace;

// ── Test-local DbContext ──────────────────────────────────────────────────────

internal sealed class MarketplaceTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        MarketplaceModelConfiguration.Configure(modelBuilder);
    }
}

internal sealed class MarketplaceStubTenantContext(long shopId) : ITenantContext
{
    public long ShopId => shopId;
    public long CurrentUserId => 1L;
    public IReadOnlyList<string> CurrentUserRoles => [];
}

// ── MarketplaceAccountService tests ──────────────────────────────────────────

[Trait("Category", "Unit")]
public class MarketplaceAccountServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly MarketplaceAccountService _sut;
    private readonly SqliteConnection _connection;
    private const long ShopId = 1L;

    public MarketplaceAccountServiceTests()
    {
        var stubCtx = new MarketplaceStubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new MarketplaceTenantDbContext(opts, stubCtx,
            new AuditSaveChangesInterceptor(stubCtx),
            new TenantSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        _sut = new MarketplaceAccountService(_db, _errorLogger, stubCtx);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsSuccessWithId()
    {
        var result = await _sut.CreateAsync(MakeAccountDto());
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_SetsIsActiveTrue()
    {
        var result = await _sut.CreateAsync(MakeAccountDto());
        var account = await _db.Set<MarketplaceAccount>().FindAsync(result.Value);
        account!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_SetsMarketplaceCode()
    {
        var result = await _sut.CreateAsync(MakeAccountDto());
        var account = await _db.Set<MarketplaceAccount>().FindAsync(result.Value);
        account!.MarketplaceCode.Should().Be("Amazon");
    }

    [Fact]
    public async Task ListAsync_ReturnsAllAccounts()
    {
        await _sut.CreateAsync(MakeAccountDto());
        await _sut.CreateAsync(MakeAccountDto("Flipkart", "Flip123"));
        var list = await _sut.ListAsync();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsync_EmptyShop_ReturnsEmpty()
    {
        var list = await _sut.ListAsync();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ValidAccount_UpdatesName()
    {
        var created = await _sut.CreateAsync(MakeAccountDto());
        var patch = new UpdateMarketplaceAccountDto("Updated Name", null, null, null, null, null);

        var result = await _sut.UpdateAsync(created.Value, patch);

        result.IsSuccess.Should().BeTrue();
        var account = await _db.Set<MarketplaceAccount>().FindAsync(created.Value);
        account!.AccountName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_ToggleSyncFlags_UpdatesCorrectly()
    {
        var created = await _sut.CreateAsync(MakeAccountDto());
        var patch = new UpdateMarketplaceAccountDto(null, null, true, true, true, null);

        await _sut.UpdateAsync(created.Value, patch);

        var account = await _db.Set<MarketplaceAccount>().FindAsync(created.Value);
        account!.SyncInventory.Should().BeTrue();
        account.SyncPricing.Should().BeTrue();
        account.SyncOrders.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.UpdateAsync(9999, new UpdateMarketplaceAccountDto(null, null, null, null, null, null));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_ExistingAccount_ReturnsSuccess()
    {
        var created = await _sut.CreateAsync(MakeAccountDto());
        var result = await _sut.TestConnectionAsync(created.Value);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.TestConnectionAsync(9999);
        result.IsSuccess.Should().BeFalse();
    }

    private static CreateMarketplaceAccountDto MakeAccountDto(
        string code = "Amazon", string sellerId = "AMZ001") => new(
        code, $"{code} Store", sellerId,
        "{\"token\":\"test\"}", false, false, false);
}

// ── MarketplaceOrderService tests ─────────────────────────────────────────────

[Trait("Category", "Unit")]
public class MarketplaceOrderServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly MarketplaceOrderService _sut;
    private readonly SqliteConnection _connection;
    private const long ShopId = 1L;

    public MarketplaceOrderServiceTests()
    {
        var stubCtx = new MarketplaceStubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new MarketplaceTenantDbContext(opts, stubCtx,
            new AuditSaveChangesInterceptor(stubCtx),
            new TenantSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        _sut = new MarketplaceOrderService(_db, _errorLogger, stubCtx);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task IngestAsync_NewOrder_ReturnsSuccessWithId()
    {
        await SeedAccount();
        var result = await _sut.IngestAsync(MakeIngestDto());
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task IngestAsync_SetsStatusToNew()
    {
        await SeedAccount();
        var result = await _sut.IngestAsync(MakeIngestDto());
        var order = await _db.Set<MarketplaceOrder>().FindAsync(result.Value);
        order!.Status.Should().Be(MarketplaceOrderStatus.New);
    }

    [Fact]
    public async Task IngestAsync_DuplicateOrderId_ReturnsConflict()
    {
        await SeedAccount();
        await _sut.IngestAsync(MakeIngestDto("MKT-001"));
        var result = await _sut.IngestAsync(MakeIngestDto("MKT-001"));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_FilterByStatus_ReturnsMatchingOrders()
    {
        await SeedAccount();
        await _sut.IngestAsync(MakeIngestDto("MKT-001"));
        await _sut.IngestAsync(MakeIngestDto("MKT-002"));

        var list = await _sut.ListAsync(new MarketplaceOrderListRequest(null, MarketplaceOrderStatus.New, null, null));
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsync_FilterByAccount_ReturnsOnlyMatchingOrders()
    {
        await SeedAccount();
        await _sut.IngestAsync(MakeIngestDto("MKT-001", accountId: 1));

        var list = await _sut.ListAsync(new MarketplaceOrderListRequest(AccountId: 1, null, null, null));
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task ConvertToInvoiceAsync_NewOrder_SetsConverted()
    {
        await SeedAccount();
        var ingested = await _sut.IngestAsync(MakeIngestDto());
        var result = await _sut.ConvertToInvoiceAsync(ingested.Value);

        result.IsSuccess.Should().BeTrue();
        var order = await _db.Set<MarketplaceOrder>().FindAsync(ingested.Value);
        order!.Status.Should().Be(MarketplaceOrderStatus.Converted);
    }

    [Fact]
    public async Task ConvertToInvoiceAsync_AlreadyConverted_ReturnsConflict()
    {
        await SeedAccount();
        var ingested = await _sut.IngestAsync(MakeIngestDto());
        await _sut.ConvertToInvoiceAsync(ingested.Value);
        var result = await _sut.ConvertToInvoiceAsync(ingested.Value);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ConvertToInvoiceAsync_UnknownOrder_ReturnsNotFound()
    {
        var result = await _sut.ConvertToInvoiceAsync(9999);
        result.IsSuccess.Should().BeFalse();
    }

    private async Task SeedAccount()
    {
        _db.Set<MarketplaceAccount>().Add(new MarketplaceAccount
        {
            Id = 1, ShopId = ShopId,
            MarketplaceCode = "Amazon", AccountName = "Test Amazon",
            SellerId = "AMZ001", CredentialsJsonEncrypted = "{}",
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
    }

    private static IngestOrderDto MakeIngestDto(string orderId = "MKT-001", long accountId = 1) => new(
        accountId, orderId,
        DateTime.UtcNow.Date, "Test Customer", "9876543210",
        "{\"city\":\"Mumbai\"}", 1500m, "{\"raw\":\"payload\"}");
}

// ── MarketplaceSyncService tests ──────────────────────────────────────────────

[Trait("Category", "Unit")]
public class MarketplaceSyncServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly MarketplaceSyncService _sut;
    private readonly SqliteConnection _connection;
    private const long ShopId = 1L;

    public MarketplaceSyncServiceTests()
    {
        var stubCtx = new MarketplaceStubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new MarketplaceTenantDbContext(opts, stubCtx,
            new AuditSaveChangesInterceptor(stubCtx),
            new TenantSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        var connectors = new List<IMarketplaceConnector>();
        _sut = new MarketplaceSyncService(_db, _errorLogger, stubCtx, connectors, NullLogger<MarketplaceSyncService>.Instance);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task LinkProductAsync_ValidAccount_ReturnsSuccess()
    {
        await SeedAccount();
        var result = await _sut.LinkProductAsync(new LinkProductDto(1, 10, null, "SKU-001", "LIST-001", null));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LinkProductAsync_UnknownAccount_ReturnsNotFound()
    {
        var result = await _sut.LinkProductAsync(new LinkProductDto(9999, 10, null, "SKU-001", "LIST-001", null));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task LinkProductAsync_DuplicateSku_ReturnsConflict()
    {
        await SeedAccount();
        await _sut.LinkProductAsync(new LinkProductDto(1, 10, null, "SKU-001", "LIST-001", null));
        var result = await _sut.LinkProductAsync(new LinkProductDto(1, 11, null, "SKU-001", "LIST-002", null));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListProductMappingsAsync_ReturnsAllMappings()
    {
        await SeedAccount();
        await _sut.LinkProductAsync(new LinkProductDto(1, 10, null, "SKU-001", "LIST-001", null));
        var list = await _sut.ListProductMappingsAsync();
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task SyncOrdersAsync_NoActiveAccounts_ReturnsEmptyResult()
    {
        var result = await _sut.SyncOrdersAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.AccountsProcessed.Should().Be(0);
    }

    [Fact]
    public async Task SyncInventoryAsync_NoActiveAccounts_ReturnsEmptyResult()
    {
        var result = await _sut.SyncInventoryAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.AccountsProcessed.Should().Be(0);
    }

    [Fact]
    public async Task SyncPricesAsync_NoActiveAccounts_ReturnsEmptyResult()
    {
        var result = await _sut.SyncPricesAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.AccountsProcessed.Should().Be(0);
    }

    private async Task SeedAccount()
    {
        _db.Set<MarketplaceAccount>().Add(new MarketplaceAccount
        {
            Id = 1, ShopId = ShopId,
            MarketplaceCode = "Amazon", AccountName = "Test Amazon",
            SellerId = "AMZ001", CredentialsJsonEncrypted = "{}",
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
    }
}
