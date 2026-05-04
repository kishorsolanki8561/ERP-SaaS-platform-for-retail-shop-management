using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Verticals.Grocery.Enums;
using ErpSaas.Modules.Verticals.Grocery.Infrastructure;
using ErpSaas.Modules.Verticals.Grocery.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Grocery;

internal sealed class GroceryTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        GroceryModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class LoyaltyServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly SqliteConnection _sqlite;
    private readonly LoyaltyService _sut;
    private readonly StubTenantContext _ctx = new(1L);

    public LoyaltyServiceTests()
    {
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new GroceryTenantDbContext(opts, _ctx, new AuditSaveChangesInterceptor(_ctx), new TenantSaveChangesInterceptor(_ctx));
        _db.Database.EnsureCreated();
        _sut = new LoyaltyService(_db, _errorLogger, _ctx);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    private static LoyaltyProgramDto DefaultProgramDto(bool isActive = true) =>
        new(0, "Loyalty Club", PointsPerRupee: 1m, RupeeValuePerPoint: 0.25m,
            MinimumRedemptionPoints: 100, MaxRedemptionPercentPerBill: 20m,
            PointExpiryDays: 365, IsActive: isActive);

    private async Task SeedProgram(bool isActive = true)
        => await _sut.CreateOrUpdateProgramAsync(DefaultProgramDto(isActive));

    [Fact]
    public async Task GetProgramAsync_NoProgramSeeded_ReturnsNull()
    {
        var result = await _sut.GetProgramAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateOrUpdateProgramAsync_Create_PersistsProgram()
    {
        var result = await _sut.CreateOrUpdateProgramAsync(DefaultProgramDto());
        Assert.True(result.IsSuccess);
        var dto = await _sut.GetProgramAsync();
        Assert.NotNull(dto);
        Assert.Equal("Loyalty Club", dto!.Name);
        Assert.Equal(1m, dto.PointsPerRupee);
    }

    [Fact]
    public async Task CreateOrUpdateProgramAsync_Update_ModifiesExistingProgram()
    {
        await SeedProgram();
        var updated = DefaultProgramDto() with { Name = "Super Loyalty", PointsPerRupee = 2m };
        await _sut.CreateOrUpdateProgramAsync(updated);

        var dto = await _sut.GetProgramAsync();
        Assert.Equal("Super Loyalty", dto!.Name);
        Assert.Equal(2m, dto.PointsPerRupee);
    }

    [Fact]
    public async Task GetCustomerBalanceAsync_NoTransactions_ReturnsZeroBalance()
    {
        await SeedProgram();
        var balance = await _sut.GetCustomerBalanceAsync(customerId: 1);
        Assert.Equal(0, balance.TotalPoints);
        Assert.Equal(0, balance.RedeemablePoints);
    }

    [Fact]
    public async Task EarnPointsAsync_NoProgramExists_ReturnsZero()
    {
        var result = await _sut.EarnPointsAsync(new EarnPointsDto(1, 1, 1000));
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task EarnPointsAsync_WithActiveProgram_EarnsCorrectPoints()
    {
        await SeedProgram();
        var result = await _sut.EarnPointsAsync(new EarnPointsDto(CustomerId: 1, InvoiceId: 1, InvoiceTotal: 500));
        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value);

        var balance = await _sut.GetCustomerBalanceAsync(1);
        Assert.Equal(500m, balance.TotalPoints);
    }

    [Fact]
    public async Task EarnPointsAsync_InactiveProgram_ReturnsZero()
    {
        await SeedProgram(isActive: false);
        var result = await _sut.EarnPointsAsync(new EarnPointsDto(1, 1, 500));
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
    }

    [Fact]
    public async Task RedeemPointsAsync_SufficientBalance_DeductsAndReturnsRupeeValue()
    {
        await SeedProgram();
        await _sut.EarnPointsAsync(new EarnPointsDto(1, 1, 500));

        var result = await _sut.RedeemPointsAsync(new RedeemPointsDto(1, 2, 200));
        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value); // 200 * 0.25

        var balance = await _sut.GetCustomerBalanceAsync(1);
        Assert.Equal(300m, balance.TotalPoints);
    }

    [Fact]
    public async Task RedeemPointsAsync_NoProgramFound_ReturnsConflict()
    {
        var result = await _sut.RedeemPointsAsync(new RedeemPointsDto(1, 1, 100));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Grocery.ProgramNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task RedeemPointsAsync_InsufficientBalance_ReturnsConflict()
    {
        await SeedProgram();
        await _sut.EarnPointsAsync(new EarnPointsDto(1, 1, 50));

        var result = await _sut.RedeemPointsAsync(new RedeemPointsDto(1, 2, 200));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Grocery.InsufficientPoints, result.Errors[0]);
    }

    [Fact]
    public async Task GetCustomerHistoryAsync_ReturnsAllTransactionsDescending()
    {
        await SeedProgram();
        await _sut.EarnPointsAsync(new EarnPointsDto(1, 1, 1000));
        await _sut.EarnPointsAsync(new EarnPointsDto(1, 2, 500));

        var history = await _sut.GetCustomerHistoryAsync(1);
        Assert.Equal(2, history.Count);
        Assert.All(history, t => Assert.Equal(LoyaltyTransactionType.Earn, t.TransactionType));
    }

    [Fact]
    public async Task GetCustomerHistoryAsync_OtherCustomer_NotIncluded()
    {
        await SeedProgram();
        await _sut.EarnPointsAsync(new EarnPointsDto(CustomerId: 1, InvoiceId: 1, InvoiceTotal: 1000));
        await _sut.EarnPointsAsync(new EarnPointsDto(CustomerId: 2, InvoiceId: 2, InvoiceTotal: 500));

        var history = await _sut.GetCustomerHistoryAsync(customerId: 1);
        Assert.Single(history);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
