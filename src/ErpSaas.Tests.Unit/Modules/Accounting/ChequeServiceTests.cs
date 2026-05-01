using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Accounting.Entities;
using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Modules.Accounting.Infrastructure;
using ErpSaas.Modules.Accounting.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Accounting;

[Trait("Category", "Unit")]
public class ChequeServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence  = Substitute.For<ISequenceService>();
    private readonly ChequeService _sut;
    private readonly SqliteConnection _sqliteConnection;
    private int _seqCounter;

    public ChequeServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 1L);
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection).Options;

        var auditInterceptor  = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);
        _db = new ChequeTestDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"VR-{++_seqCounter:D6}"));

        _sut = new ChequeService(_db, _errorLogger, _sequence, stubCtx,
            Substitute.For<ILogger<ChequeService>>());
    }

    public void Dispose() { _db.Dispose(); _sqliteConnection.Dispose(); }

    [Fact]
    public async Task DepositChequeAsync_WhenStatusIsReceived_TransitionsToDeposited()
    {
        var cheque = SeedCheque(ChequeStatus.Received);

        var result = await _sut.DepositChequeAsync(cheque.Id, DateTime.Today);

        result.IsSuccess.Should().BeTrue();
        var updated = _db.Set<Cheque>().Find(cheque.Id)!;
        updated.Status.Should().Be(ChequeStatus.Deposited);
        updated.DepositedDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task DepositChequeAsync_WhenAlreadyCleared_ReturnsConflict()
    {
        var cheque = SeedCheque(ChequeStatus.Cleared);

        var result = await _sut.DepositChequeAsync(cheque.Id, DateTime.Today);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelChequeAsync_WhenReceived_SetsStatusCancelled()
    {
        var cheque = SeedCheque(ChequeStatus.Received);

        var result = await _sut.CancelChequeAsync(cheque.Id);

        result.IsSuccess.Should().BeTrue();
        _db.Set<Cheque>().Find(cheque.Id)!.Status.Should().Be(ChequeStatus.Cancelled);
    }

    [Fact]
    public async Task CancelChequeAsync_WhenCleared_ReturnsConflict()
    {
        var cheque = SeedCheque(ChequeStatus.Cleared);

        var result = await _sut.CancelChequeAsync(cheque.Id);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task MarkStaleDatedAsync_FlipsOldReceivedCheques()
    {
        var old   = SeedCheque(ChequeStatus.Received, DateTime.UtcNow.AddMonths(-4));
        var fresh = SeedCheque(ChequeStatus.Received, DateTime.UtcNow.AddDays(-1));

        await _sut.MarkStaleDatedAsync();

        _db.Set<Cheque>().IgnoreQueryFilters().First(c => c.Id == old.Id).Status.Should().Be(ChequeStatus.StaleDated);
        _db.Set<Cheque>().IgnoreQueryFilters().First(c => c.Id == fresh.Id).Status.Should().Be(ChequeStatus.Received);
    }

    [Fact]
    public async Task ListChequesAsync_FiltersByStatus()
    {
        SeedCheque(ChequeStatus.Received);
        SeedCheque(ChequeStatus.Deposited);

        var result = await _sut.ListChequesAsync(ChequeStatus.Received, 1, 20);

        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(ChequeStatus.Received);
    }

    private Cheque SeedCheque(ChequeStatus status, DateTime? chequeDate = null)
    {
        var group = new AccountGroup { ShopId = 1L, Name = $"Assets{_seqCounter}", Code = $"G{_seqCounter++:D3}", Nature = AccountNature.Asset, IsSystem = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<AccountGroup>().Add(group);
        _db.SaveChanges();

        var acct = new Account { ShopId = 1L, Name = "Bank", Code = $"A{_seqCounter++:D3}", AccountGroupId = group.Id, OpeningBalance = 0m, OpeningBalanceType = DebitCredit.Debit, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<Account>().Add(acct);
        _db.SaveChanges();

        var ba = new BankAccount { ShopId = 1L, AccountId = acct.Id, BankName = "HDFC", AccountNumber = $"ACC{_seqCounter++:D6}", IfscCode = "HDFC0001", BranchName = "Main", AccountHolderName = "Owner", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<BankAccount>().Add(ba);
        _db.SaveChanges();

        var cheque = new Cheque
        {
            ShopId = 1L, Direction = ChequeDirection.Incoming,
            ChequeNumber = $"CHQ{_seqCounter++:D6}",
            ChequeDate = chequeDate ?? DateTime.Today,
            ReceivedDate = DateTime.UtcNow, Amount = 1000m,
            BankAccountId = ba.Id, DrawerName = "Customer",
            DrawerBankName = "SBI", Status = status,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<Cheque>().Add(cheque);
        _db.SaveChanges();
        return cheque;
    }
}

// ── PettyCashService tests ────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class PettyCashServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence  = Substitute.For<ISequenceService>();
    private readonly PettyCashService _sut;
    private readonly SqliteConnection _sqliteConnection;
    private int _seqCounter;

    public PettyCashServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 1L);
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection).Options;

        var auditInterceptor  = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);
        _db = new ChequeTestDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"VC-{++_seqCounter:D6}"));

        _sut = new PettyCashService(_db, _errorLogger, _sequence, stubCtx,
            Substitute.For<ILogger<PettyCashService>>());
    }

    public void Dispose() { _db.Dispose(); _sqliteConnection.Dispose(); }

    [Fact]
    public async Task TopUpAsync_WhenPettyCashAccountMissing_ReturnsFailure()
    {
        var ba = SeedBankAccount();

        var result = await _sut.TopUpAsync(new PettyCashTopUpDto(500m, ba.Id));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task TopUpAsync_WithPettyCashAccount_ReturnsVoucherId()
    {
        SeedPettyCashAccount();
        var ba = SeedBankAccount();

        var result = await _sut.TopUpAsync(new PettyCashTopUpDto(500m, ba.Id, "Weekly top-up"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        _db.Set<Voucher>().Should().HaveCount(1);
    }

    [Fact]
    public async Task ClosePeriodAsync_WithNoVariance_CreatesClosureWithZeroVariance()
    {
        SeedPettyCashAccount();

        var result = await _sut.ClosePeriodAsync(new PettyCashClosureDto(DateTime.Today, 0m, "Weekly close"));

        result.IsSuccess.Should().BeTrue();
        var closure = _db.Set<PettyCashClosure>().Find(result.Value!);
        closure!.Variance.Should().Be(0m);
        closure.VarianceVoucherId.Should().BeNull();
    }

    [Fact]
    public async Task ListClosuresAsync_ReturnsInDescendingOrder()
    {
        _db.Set<PettyCashClosure>().Add(new PettyCashClosure { ShopId = 1L, ClosureDate = DateTime.Today.AddDays(-7), Narration = "Old", ExpectedBalance = 0, CountedBalance = 0, Variance = 0, CreatedAtUtc = DateTime.UtcNow });
        _db.Set<PettyCashClosure>().Add(new PettyCashClosure { ShopId = 1L, ClosureDate = DateTime.Today, Narration = "New", ExpectedBalance = 0, CountedBalance = 0, Variance = 0, CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _sut.ListClosuresAsync();

        result.Should().HaveCount(2);
        result[0].ClosureDate.Should().BeAfter(result[1].ClosureDate);
    }

    private Account SeedPettyCashAccount()
    {
        var group = new AccountGroup { ShopId = 1L, Name = $"Assets{_seqCounter}", Code = $"G{_seqCounter++:D3}", Nature = AccountNature.Asset, IsSystem = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<AccountGroup>().Add(group);
        _db.SaveChanges();
        var acct = new Account { ShopId = 1L, Name = "Petty Cash", Code = "1110", AccountGroupId = group.Id, OpeningBalance = 0m, OpeningBalanceType = DebitCredit.Debit, IsActive = true, IsSystem = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<Account>().Add(acct);
        _db.SaveChanges();
        return acct;
    }

    private BankAccount SeedBankAccount()
    {
        var group = new AccountGroup { ShopId = 1L, Name = $"Assets{_seqCounter}", Code = $"G{_seqCounter++:D3}", Nature = AccountNature.Asset, IsSystem = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<AccountGroup>().Add(group);
        _db.SaveChanges();
        var acct = new Account { ShopId = 1L, Name = "Bank", Code = $"A{_seqCounter++:D3}", AccountGroupId = group.Id, OpeningBalance = 0m, OpeningBalanceType = DebitCredit.Debit, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<Account>().Add(acct);
        _db.SaveChanges();
        var ba = new BankAccount { ShopId = 1L, AccountId = acct.Id, BankName = "HDFC", AccountNumber = $"ACC{_seqCounter++:D6}", IfscCode = "HDFC0001", BranchName = "Main", AccountHolderName = "Owner", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<BankAccount>().Add(ba);
        _db.SaveChanges();
        return ba;
    }
}

// ── FixedAssetService tests ───────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class FixedAssetServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence  = Substitute.For<ISequenceService>();
    private readonly FixedAssetService _sut;
    private readonly SqliteConnection _sqliteConnection;
    private int _seqCounter;

    public FixedAssetServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 1L);
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection).Options;

        var auditInterceptor  = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);
        _db = new ChequeTestDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"FA-{++_seqCounter:D6}"));

        _sut = new FixedAssetService(_db, _errorLogger, _sequence, stubCtx,
            Substitute.For<ILogger<FixedAssetService>>());
    }

    public void Dispose() { _db.Dispose(); _sqliteConnection.Dispose(); }

    [Fact]
    public async Task RegisterAsync_ValidDto_ReturnsAssetIdAndSetsNetBookValue()
    {
        var dto = new RegisterFixedAssetDto(
            Name: "POS Terminal", CategoryCode: "POS_EQUIPMENT",
            PurchaseDate: DateTime.Today, PurchaseCost: 50000m,
            Method: DepreciationMethod.StraightLine,
            UsefulLifeYears: 5, SalvageValue: 5000m);

        var result = await _sut.RegisterAsync(dto);

        result.IsSuccess.Should().BeTrue();
        var asset = _db.Set<FixedAsset>().Find(result.Value!);
        asset!.NetBookValue.Should().Be(50000m);
        asset.AccumulatedDepreciation.Should().Be(0m);
        asset.Status.Should().Be(FixedAssetStatus.InUse);
    }

    [Fact]
    public async Task RegisterAsync_StraightLineMethod_ComputesRateCorrectly()
    {
        var dto = new RegisterFixedAssetDto("Laptop", "COMPUTER", DateTime.Today,
            60000m, DepreciationMethod.StraightLine, 3m, 0m);

        var result = await _sut.RegisterAsync(dto);

        var asset = _db.Set<FixedAsset>().Find(result.Value!);
        asset!.RateOfDepreciation.Should().BeApproximately(33.3333m, 0.01m);
    }

    [Fact]
    public async Task RetireAsync_WhenInUse_SetsStatusRetired()
    {
        var asset = SeedAsset(FixedAssetStatus.InUse);

        var result = await _sut.RetireAsync(asset.Id);

        result.IsSuccess.Should().BeTrue();
        _db.Set<FixedAsset>().Find(asset.Id)!.Status.Should().Be(FixedAssetStatus.Retired);
    }

    [Fact]
    public async Task RetireAsync_WhenAlreadySold_ReturnsConflict()
    {
        var asset = SeedAsset(FixedAssetStatus.Sold);

        var result = await _sut.RetireAsync(asset.Id);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RunDepreciationAsync_StraightLine_PostsCorrectMonthlyAmount()
    {
        var asset = SeedAsset(FixedAssetStatus.InUse, purchaseCost: 60000m, salvage: 0m, lifeyears: 5m);
        SeedDepreciationAccounts();

        var result = await _sut.RunDepreciationAsync(new DateTime(2026, 5, 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        var entry = _db.Set<DepreciationEntry>().Single(e => e.FixedAssetId == asset.Id);
        entry.Amount.Should().BeApproximately(1000m, 0.01m); // 60000/5/12
        _db.Set<FixedAsset>().Find(asset.Id)!.AccumulatedDepreciation.Should().BeApproximately(1000m, 0.01m);
    }

    [Fact]
    public async Task RunDepreciationAsync_Idempotent_SecondRunSameMonthAddsZero()
    {
        SeedAsset(FixedAssetStatus.InUse, purchaseCost: 12000m, salvage: 0m, lifeyears: 1m);
        SeedDepreciationAccounts();
        var period = new DateTime(2026, 5, 1);

        await _sut.RunDepreciationAsync(period);
        var result2 = await _sut.RunDepreciationAsync(period);

        result2.Value.Should().Be(0);
        _db.Set<DepreciationEntry>().Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDepreciationScheduleAsync_ReturnsEntriesInOrder()
    {
        var asset = SeedAsset(FixedAssetStatus.InUse, purchaseCost: 12000m, salvage: 0m, lifeyears: 1m);
        SeedDepreciationAccounts();
        await _sut.RunDepreciationAsync(new DateTime(2026, 4, 1));
        await _sut.RunDepreciationAsync(new DateTime(2026, 5, 1));

        var schedule = await _sut.GetDepreciationScheduleAsync(asset.Id);

        schedule.Should().HaveCount(2);
        schedule[0].PeriodDate.Should().BeBefore(schedule[1].PeriodDate);
    }

    private FixedAsset SeedAsset(FixedAssetStatus status,
        decimal purchaseCost = 50000m, decimal salvage = 5000m, decimal lifeyears = 5m)
    {
        var asset = new FixedAsset
        {
            ShopId = 1L, AssetCode = $"FA-{_seqCounter++:D6}", Name = "Test Asset",
            CategoryCode = "POS_EQUIPMENT", PurchaseDate = DateTime.Today.AddYears(-1),
            PurchaseCost = purchaseCost, Method = DepreciationMethod.StraightLine,
            UsefulLifeYears = lifeyears, SalvageValue = salvage,
            RateOfDepreciation = lifeyears > 0 ? 100m / lifeyears : 0m,
            AccumulatedDepreciation = 0m, NetBookValue = purchaseCost,
            Status = status, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<FixedAsset>().Add(asset);
        _db.SaveChanges();
        return asset;
    }

    private void SeedDepreciationAccounts()
    {
        var group = new AccountGroup { ShopId = 1L, Name = $"Expenses{_seqCounter}", Code = $"GE{_seqCounter++:D3}", Nature = AccountNature.Expense, IsSystem = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<AccountGroup>().Add(group);
        _db.SaveChanges();
        var depExp   = new Account { ShopId = 1L, Name = "Depreciation Expense", Code = "5500", AccountGroupId = group.Id, OpeningBalance = 0m, OpeningBalanceType = DebitCredit.Debit,  IsActive = true, IsSystem = true, CreatedAtUtc = DateTime.UtcNow };
        var accumDep = new Account { ShopId = 1L, Name = "Accum Depreciation",   Code = "1510", AccountGroupId = group.Id, OpeningBalance = 0m, OpeningBalanceType = DebitCredit.Credit, IsActive = true, IsSystem = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<Account>().AddRange(depExp, accumDep);
        _db.SaveChanges();
    }
}

// Shared in-memory DbContext for cheque/petty-cash/fixed-asset tests
internal sealed class ChequeTestDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        AccountingModelConfiguration.Configure(modelBuilder);
    }
}
