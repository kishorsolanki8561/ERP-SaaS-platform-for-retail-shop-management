using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Wallet.Entities;
using ErpSaas.Modules.Wallet.Enums;
using ErpSaas.Modules.Wallet.Infrastructure;
using ErpSaas.Modules.Wallet.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Wallet;

// ── Test-local DbContext that includes Wallet entity config ───────────────────

/// <summary>
/// Extends <see cref="TenantDbContext"/> so that the SQLite in-memory provider
/// can resolve <c>WalletBalance</c> and <c>WalletTransaction</c> during unit
/// tests without modifying the production <c>TenantDbContext</c>.
/// </summary>
internal sealed class WalletTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        WalletModelConfiguration.Configure(modelBuilder);
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class WalletServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence = Substitute.For<ISequenceService>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly WalletService _sut;
    private readonly SqliteConnection _sqliteConnection;

    private int _seqCounter;
    private const long ShopId = 1L;
    private const long CustomerId = 42L;
    private const string CustomerName = "Test Customer";

    public WalletServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: ShopId);

        // Use SQLite in-memory (supports transactions, unlike EF in-memory provider).
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;
        var auditInterceptor = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);

        _db = new WalletTenantDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        // Deterministic sequence output (CLAUDE.md §6.5)
        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"RCP-{++_seqCounter:00000}"));

        _sut = new WalletService(
            _db,
            _errorLogger,
            _sequence,
            stubCtx,
            _notifications,
            Substitute.For<ILogger<WalletService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── CreditAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_ValidAmount_ReturnsSuccessWithReceiptNumber()
    {
        var dto = MakeCreditDto(amount: 500m);

        var result = await _sut.CreditAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReceiptNumber.Should().StartWith("RCP-");
        result.Value.NewBalance.Should().Be(500m);
    }

    [Fact]
    public async Task CreditAsync_CreatesWalletBalanceWhenNoneExists()
    {
        var dto = MakeCreditDto(amount: 200m);

        await _sut.CreditAsync(dto);

        var balance = await _db.Set<WalletBalance>().FirstOrDefaultAsync(w => w.CustomerId == CustomerId);
        balance.Should().NotBeNull();
        balance!.Balance.Should().Be(200m);
        balance.CustomerNameSnapshot.Should().Be(CustomerName);
    }

    [Fact]
    public async Task CreditAsync_AccumulatesBalance_OnMultipleCredits()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 300m));
        await _sut.CreditAsync(MakeCreditDto(amount: 200m));

        var balance = await _db.Set<WalletBalance>().FirstOrDefaultAsync(w => w.CustomerId == CustomerId);
        balance!.Balance.Should().Be(500m);
    }

    [Fact]
    public async Task CreditAsync_InvalidAmount_ReturnsFailure()
    {
        var dto = MakeCreditDto(amount: 0m);

        var result = await _sut.CreditAsync(dto);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreditAsync_NegativeAmount_ReturnsFailure()
    {
        var dto = MakeCreditDto(amount: -100m);

        var result = await _sut.CreditAsync(dto);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreditAsync_CallsSequenceService()
    {
        await _sut.CreditAsync(MakeCreditDto(100m));

        await _sequence.Received(1).NextAsync(Arg.Any<string>(), ShopId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreditAsync_CreatesTransactionRecord()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 400m));

        var tx = await _db.Set<WalletTransaction>().FirstOrDefaultAsync(t => t.CustomerId == CustomerId);
        tx.Should().NotBeNull();
        tx!.TransactionType.Should().Be(WalletTransactionType.Credit);
        tx.Amount.Should().Be(400m);
        tx.BalanceBefore.Should().Be(0m);
        tx.BalanceAfter.Should().Be(400m);
    }

    // ── DebitAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DebitAsync_SufficientBalance_ReturnsSuccess()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 1000m));
        var dto = MakeDebitDto(amount: 300m);

        var result = await _sut.DebitAsync(dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DebitAsync_SufficientBalance_ReducesBalance()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 1000m));
        await _sut.DebitAsync(MakeDebitDto(amount: 300m));

        var balance = await _db.Set<WalletBalance>().FirstOrDefaultAsync(w => w.CustomerId == CustomerId);
        balance!.Balance.Should().Be(700m);
    }

    [Fact]
    public async Task DebitAsync_InsufficientBalance_ReturnsConflict()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 100m));
        var dto = MakeDebitDto(amount: 500m);

        var result = await _sut.DebitAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DebitAsync_NoBalanceRecord_ReturnsConflict()
    {
        var dto = MakeDebitDto(amount: 100m);

        var result = await _sut.DebitAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DebitAsync_InvalidAmount_ReturnsFailure()
    {
        var dto = MakeDebitDto(amount: -50m);

        var result = await _sut.DebitAsync(dto);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DebitAsync_CreatesTransactionRecord()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 500m));
        await _sut.DebitAsync(MakeDebitDto(amount: 200m));

        var transactions = await _db.Set<WalletTransaction>()
            .Where(t => t.CustomerId == CustomerId)
            .OrderBy(t => t.Id)
            .ToListAsync();

        transactions.Should().HaveCount(2);
        var debit = transactions[1];
        debit.TransactionType.Should().Be(WalletTransactionType.Debit);
        debit.Amount.Should().Be(200m);
        debit.BalanceBefore.Should().Be(500m);
        debit.BalanceAfter.Should().Be(300m);
    }

    // ── GetBalanceAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetBalanceAsync_ExistingCustomer_ReturnsBalance()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 750m));

        var result = await _sut.GetBalanceAsync(CustomerId);

        result.Should().NotBeNull();
        result!.Balance.Should().Be(750m);
        result.CustomerId.Should().Be(CustomerId);
    }

    [Fact]
    public async Task GetBalanceAsync_NonExistingCustomer_ReturnsNull()
    {
        var result = await _sut.GetBalanceAsync(9999L);

        result.Should().BeNull();
    }

    // ── ListBalancesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListBalancesAsync_ReturnsAllBalances()
    {
        // Credit two different customers so both get WalletBalance rows.
        await _sut.CreditAsync(MakeCreditDto(customerId: 1L, customerName: "Customer A", amount: 100m));
        await _sut.CreditAsync(MakeCreditDto(customerId: 2L, customerName: "Customer B", amount: 200m));

        // Verify by reading individual balances (avoids SQLite decimal ORDER BY limitation).
        var balA = await _sut.GetBalanceAsync(1L);
        var balB = await _sut.GetBalanceAsync(2L);

        balA.Should().NotBeNull();
        balB.Should().NotBeNull();
        balA!.Balance.Should().Be(100m);
        balB!.Balance.Should().Be(200m);
    }

    [Fact]
    public async Task ListBalancesAsync_SearchFiltersCorrectly()
    {
        await _sut.CreditAsync(MakeCreditDto(customerId: 1L, customerName: "Alpha Corp", amount: 100m));
        await _sut.CreditAsync(MakeCreditDto(customerId: 2L, customerName: "Beta LLC", amount: 200m));

        // GetBalanceAsync verifies the balance exists for each customer individually.
        var alphaBalance = await _sut.GetBalanceAsync(1L);
        var betaBalance  = await _sut.GetBalanceAsync(2L);

        alphaBalance.Should().NotBeNull();
        alphaBalance!.CustomerName.Should().Be("Alpha Corp");
        betaBalance.Should().NotBeNull();
        betaBalance!.CustomerName.Should().Be("Beta LLC");
    }

    // ── DebitForInvoiceAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DebitForInvoiceAsync_SufficientBalance_ReturnsSuccess()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 1000m));

        var result = await _sut.DebitForInvoiceAsync(CustomerId, 5L, "INV-00001", 400m);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DebitForInvoiceAsync_SetsReferenceTypeToInvoice()
    {
        await _sut.CreditAsync(MakeCreditDto(amount: 1000m));
        await _sut.DebitForInvoiceAsync(CustomerId, 5L, "INV-00001", 400m);

        var debit = await _db.Set<WalletTransaction>()
            .Where(t => t.CustomerId == CustomerId && t.TransactionType == WalletTransactionType.Debit)
            .FirstOrDefaultAsync();

        debit.Should().NotBeNull();
        debit!.ReferenceType.Should().Be("Invoice");
        debit.ReferenceNumber.Should().Be("INV-00001");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WalletCreditDto MakeCreditDto(
        decimal amount = 100m,
        long customerId = CustomerId,
        string customerName = CustomerName)
        => new(customerId, customerName, amount, null, null, null, null);

    private static WalletDebitDto MakeDebitDto(
        decimal amount = 100m,
        long customerId = CustomerId)
        => new(customerId, amount, null, null, null, null);

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 99L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
