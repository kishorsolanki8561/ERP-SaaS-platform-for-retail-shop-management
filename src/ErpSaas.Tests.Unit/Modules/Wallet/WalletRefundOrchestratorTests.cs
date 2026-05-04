using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Wallet.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Wallet;

[Trait("Category", "Unit")]
public class WalletRefundOrchestratorTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly IWalletService _walletService = Substitute.For<IWalletService>();
    private readonly WalletRefundOrchestrator _sut;
    private readonly SqliteConnection _sqliteConnection;

    private const long ShopId = 1L;
    private const long CustomerId = 20L;
    private const long SalesReturnId = 500L;

    public WalletRefundOrchestratorTests()
    {
        var stubCtx = new StubTenantContext(shopId: ShopId);

        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _db = new WalletTenantDbContext(opts, stubCtx,
            new AuditSaveChangesInterceptor(stubCtx),
            new TenantSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        _sut = new WalletRefundOrchestrator(_db, _errorLogger, _walletService);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── ProcessRefundAsync — wallet credit ────────────────────────────────────

    [Fact]
    public async Task ProcessRefundAsync_WalletAmount_CallsWalletCredit()
    {
        SetupCreditSuccess();

        var result = await _sut.ProcessRefundAsync(
            SalesReturnId, CustomerId, "Customer", null, "SR-00001",
            refundToWallet: 200m, refundToCash: 0m);

        result.IsSuccess.Should().BeTrue();
        await _walletService.Received(1).CreditAsync(
            Arg.Is<WalletCreditDto>(d => d.CustomerId == CustomerId && d.Amount == 200m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_ZeroWalletAmount_SkipsCredit()
    {
        var result = await _sut.ProcessRefundAsync(
            SalesReturnId, CustomerId, "Customer", null, "SR-00001",
            refundToWallet: 0m, refundToCash: 100m);

        result.IsSuccess.Should().BeTrue();
        await _walletService.DidNotReceive().CreditAsync(
            Arg.Any<WalletCreditDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_BothZero_ReturnsSuccess()
    {
        var result = await _sut.ProcessRefundAsync(
            SalesReturnId, CustomerId, "Customer", null, "SR-00001",
            refundToWallet: 0m, refundToCash: 0m);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessRefundAsync_NegativeWalletAmount_ReturnsFailure()
    {
        var result = await _sut.ProcessRefundAsync(
            SalesReturnId, CustomerId, "Customer", null, "SR-00001",
            refundToWallet: -50m, refundToCash: 0m);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessRefundAsync_NegativeCashAmount_ReturnsFailure()
    {
        var result = await _sut.ProcessRefundAsync(
            SalesReturnId, CustomerId, "Customer", null, "SR-00001",
            refundToWallet: 0m, refundToCash: -50m);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessRefundAsync_WalletCreditFails_ReturnsFailure()
    {
        _walletService.CreditAsync(Arg.Any<WalletCreditDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<WalletCreditResultDto>.Failure("WALLET_001"));

        var result = await _sut.ProcessRefundAsync(
            SalesReturnId, CustomerId, "Customer", null, "SR-00001",
            refundToWallet: 100m, refundToCash: 0m);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessRefundAsync_SetsRefundReferenceType()
    {
        SetupCreditSuccess();

        await _sut.ProcessRefundAsync(
            SalesReturnId, CustomerId, "Customer", null, "SR-00001",
            refundToWallet: 150m, refundToCash: 0m);

        await _walletService.Received(1).CreditAsync(
            Arg.Is<WalletCreditDto>(d => d.ReferenceType == "REFUND" && d.ReferenceNumber == "SR-00001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_PassesCustomerPhoneToCredit()
    {
        SetupCreditSuccess();

        await _sut.ProcessRefundAsync(
            SalesReturnId, CustomerId, "Customer", "+91-9999999999", "SR-00001",
            refundToWallet: 100m, refundToCash: 0m);

        await _walletService.Received(1).CreditAsync(
            Arg.Is<WalletCreditDto>(d => d.CustomerPhone == "+91-9999999999"),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupCreditSuccess() =>
        _walletService.CreditAsync(Arg.Any<WalletCreditDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<WalletCreditResultDto>.Success(new WalletCreditResultDto("RCP-00001", 200m)));

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 99L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
