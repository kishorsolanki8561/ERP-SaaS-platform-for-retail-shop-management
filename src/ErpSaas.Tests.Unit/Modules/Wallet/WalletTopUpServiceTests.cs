using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Wallet.Entities;
using ErpSaas.Modules.Wallet.Enums;
using ErpSaas.Modules.Wallet.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Wallet;

[Trait("Category", "Unit")]
public class WalletTopUpServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly IWalletService _walletService = Substitute.For<IWalletService>();
    private readonly WalletTopUpService _sut;
    private readonly SqliteConnection _sqliteConnection;

    private const long ShopId = 1L;
    private const long CustomerId = 10L;
    private const string CustomerName = "Top-Up Customer";

    public WalletTopUpServiceTests()
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

        _sut = new WalletTopUpService(_db, _errorLogger, stubCtx, _walletService);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── InitiateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InitiateAsync_ValidAmount_ReturnsSuccessWithId()
    {
        var dto = MakeInitiateDto(amount: 500m);

        var result = await _sut.InitiateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InitiateAsync_ValidAmount_PersistsTopUpAsPending()
    {
        var dto = MakeInitiateDto(amount: 300m);

        var result = await _sut.InitiateAsync(dto);

        var topUp = await _db.Set<WalletTopUp>().FirstOrDefaultAsync(t => t.Id == result.Value);
        topUp.Should().NotBeNull();
        topUp!.Status.Should().Be(WalletTopUpStatus.Pending);
        topUp.Amount.Should().Be(300m);
        topUp.CustomerId.Should().Be(CustomerId);
    }

    [Fact]
    public async Task InitiateAsync_ZeroAmount_ReturnsFailure()
    {
        var dto = MakeInitiateDto(amount: 0m);

        var result = await _sut.InitiateAsync(dto);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task InitiateAsync_NegativeAmount_ReturnsFailure()
    {
        var dto = MakeInitiateDto(amount: -100m);

        var result = await _sut.InitiateAsync(dto);

        result.IsSuccess.Should().BeFalse();
    }

    // ── CompleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_PendingTopUpCreditSucceeds_ReturnsSuccess()
    {
        var id = (await _sut.InitiateAsync(MakeInitiateDto(200m))).Value;
        SetupCreditSuccess("RCP-00001", 200m);

        var result = await _sut.CompleteAsync(id, new CompleteTopUpDto(null, null));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_PendingTopUp_UpdatesStatusToSuccess()
    {
        var id = (await _sut.InitiateAsync(MakeInitiateDto(200m))).Value;
        SetupCreditSuccess("RCP-00001", 200m);

        await _sut.CompleteAsync(id, new CompleteTopUpDto(null, null));

        var topUp = await _db.Set<WalletTopUp>().FirstAsync(t => t.Id == id);
        topUp.Status.Should().Be(WalletTopUpStatus.Success);
        topUp.ReceiptNumber.Should().Be("RCP-00001");
        topUp.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteAsync_TopUpNotFound_ReturnsNotFound()
    {
        var result = await _sut.CompleteAsync(9999L, new CompleteTopUpDto(null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteAsync_NotPendingTopUp_ReturnsConflict()
    {
        var id = (await _sut.InitiateAsync(MakeInitiateDto(200m))).Value;
        SetupCreditSuccess("RCP-00001", 200m);
        await _sut.CompleteAsync(id, new CompleteTopUpDto(null, null));

        var result = await _sut.CompleteAsync(id, new CompleteTopUpDto(null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CompleteAsync_WalletCreditFails_ReturnsFailure()
    {
        var id = (await _sut.InitiateAsync(MakeInitiateDto(200m))).Value;
        _walletService.CreditAsync(Arg.Any<WalletCreditDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<WalletCreditResultDto>.Failure("WALLET_001"));

        var result = await _sut.CompleteAsync(id, new CompleteTopUpDto(null, null));

        result.IsSuccess.Should().BeFalse();
    }

    // ── FailAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FailAsync_PendingTopUp_UpdatesStatusToFailed()
    {
        var id = (await _sut.InitiateAsync(MakeInitiateDto(100m))).Value;

        var result = await _sut.FailAsync(id, "Payment declined");

        result.IsSuccess.Should().BeTrue();
        var topUp = await _db.Set<WalletTopUp>().FirstAsync(t => t.Id == id);
        topUp.Status.Should().Be(WalletTopUpStatus.Failed);
        topUp.FailureReason.Should().Be("Payment declined");
    }

    [Fact]
    public async Task FailAsync_TopUpNotFound_ReturnsNotFound()
    {
        var result = await _sut.FailAsync(9999L, "reason");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FailAsync_NotPendingTopUp_ReturnsConflict()
    {
        var id = (await _sut.InitiateAsync(MakeInitiateDto(100m))).Value;
        SetupCreditSuccess("RCP-00001", 100m);
        await _sut.CompleteAsync(id, new CompleteTopUpDto(null, null));

        var result = await _sut.FailAsync(id, "too late");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ── GetByIdAsync / ListAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var id = (await _sut.InitiateAsync(MakeInitiateDto(250m))).Value;

        var dto = await _sut.GetByIdAsync(id);

        dto.Should().NotBeNull();
        dto!.Amount.Should().Be(250m);
        dto.CustomerId.Should().Be(CustomerId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var dto = await _sut.GetByIdAsync(9999L);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_MultipleTopUps_ReturnsPaged()
    {
        await _sut.InitiateAsync(MakeInitiateDto(100m));
        await _sut.InitiateAsync(MakeInitiateDto(200m));

        var list = await _sut.ListAsync(CustomerId, 1, 10);

        list.Should().HaveCount(2);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static InitiateTopUpDto MakeInitiateDto(decimal amount) =>
        new(CustomerId, CustomerName, amount, "CASH", null);

    private void SetupCreditSuccess(string receipt, decimal balance) =>
        _walletService.CreditAsync(Arg.Any<WalletCreditDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<WalletCreditResultDto>.Success(new WalletCreditResultDto(receipt, balance)));

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 99L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
