using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Identity;

[Trait("Category", "Unit")]
public class ShopRegistrationServiceTests : IDisposable
{
    private readonly PlatformDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly IShopOnboardingService _onboarding = Substitute.For<IShopOnboardingService>();
    private readonly ShopRegistrationService _sut;
    private readonly SqliteConnection _sqliteConnection;

    public ShopRegistrationServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 0);

        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _db = new IdentityPlatformDbContext(opts, new AuditSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        SeedStarterPlan();

        var config = Substitute.For<IConfiguration>();
        config[Constants.Security.BcryptWorkFactorKey].Returns("4");

        _sut = new ShopRegistrationService(_db, _errorLogger, config, _onboarding);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── SubmitAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitAsync_ValidRequest_ReturnsPendingId()
    {
        var result = await _sut.SubmitAsync(ValidRequest("SHOP001", "admin@shop1.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        var reg = await _db.ShopRegistrationRequests.FindAsync(result.Value);
        reg.Should().NotBeNull();
        reg!.Status.Should().Be(RegistrationStatus.Pending);
        reg.ShopCode.Should().Be("SHOP001");
    }

    [Fact]
    public async Task SubmitAsync_ShopCodeExistsInShopsTable_ReturnsConflict()
    {
        _db.Shops.Add(new Shop { ShopCode = "TAKEN", LegalName = "Taken", CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _sut.SubmitAsync(ValidRequest("TAKEN", "new@shop.com"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains(Errors.Registration.ShopCodeExists));
    }

    [Fact]
    public async Task SubmitAsync_PendingRegistrationWithSameCode_ReturnsConflict()
    {
        await _sut.SubmitAsync(ValidRequest("DUPE", "first@shop.com"));

        var result = await _sut.SubmitAsync(ValidRequest("DUPE", "second@shop.com"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains(Errors.Registration.ShopCodeExists));
    }

    [Fact]
    public async Task SubmitAsync_EmailAlreadyRegisteredAsUser_ReturnsConflict()
    {
        _db.Users.Add(new User
        {
            Email = "taken@user.com", DisplayName = "Taken", PasswordHash = "x",
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var result = await _sut.SubmitAsync(ValidRequest("NEW001", "taken@user.com"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains(Errors.Registration.EmailExists));
    }

    // ── ApproveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_PendingRequest_CallsOnboardAndSetsApproved()
    {
        var submitResult = await _sut.SubmitAsync(ValidRequest("SHOP002", "owner@shop2.com"));
        var id = submitResult.Value;

        _onboarding.OnboardFromApprovedRequestAsync(Arg.Any<ShopRegistrationRequest>(), Arg.Any<CancellationToken>())
                   .Returns(Result<long>.Success(99L));

        var result = await _sut.ApproveAsync(id, reviewerUserId: 1L);

        result.IsSuccess.Should().BeTrue();
        await _onboarding.Received(1).OnboardFromApprovedRequestAsync(
            Arg.Is<ShopRegistrationRequest>(r => r.Id == id), Arg.Any<CancellationToken>());

        var reg = await _db.ShopRegistrationRequests.FindAsync(id);
        reg!.Status.Should().Be(RegistrationStatus.Approved);
        reg.ReviewedByUserId.Should().Be(1L);
        reg.ReviewedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApprovedRequest_ReturnsConflict()
    {
        var submitResult = await _sut.SubmitAsync(ValidRequest("SHOP003", "owner@shop3.com"));
        var id = submitResult.Value;

        _onboarding.OnboardFromApprovedRequestAsync(Arg.Any<ShopRegistrationRequest>(), Arg.Any<CancellationToken>())
                   .Returns(Result<long>.Success(100L));

        await _sut.ApproveAsync(id, reviewerUserId: 1L);
        var result = await _sut.ApproveAsync(id, reviewerUserId: 1L);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains(Errors.Registration.NotPending));
    }

    // ── RejectAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_PendingRequest_SetsRejectedWithReason()
    {
        var submitResult = await _sut.SubmitAsync(ValidRequest("SHOP004", "owner@shop4.com"));
        var id = submitResult.Value;

        var result = await _sut.RejectAsync(id, "Duplicate business", reviewerUserId: 2L);

        result.IsSuccess.Should().BeTrue();

        var reg = await _db.ShopRegistrationRequests.FindAsync(id);
        reg!.Status.Should().Be(RegistrationStatus.Rejected);
        reg.RejectionReason.Should().Be("Duplicate business");
        reg.ReviewedByUserId.Should().Be(2L);
    }

    [Fact]
    public async Task RejectAsync_AlreadyRejectedRequest_ReturnsConflict()
    {
        var submitResult = await _sut.SubmitAsync(ValidRequest("SHOP005", "owner@shop5.com"));
        var id = submitResult.Value;

        await _sut.RejectAsync(id, "First reason", reviewerUserId: 1L);
        var result = await _sut.RejectAsync(id, "Second reason", reviewerUserId: 1L);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains(Errors.Registration.NotPending));
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_FilterByStatus_ReturnsCorrectSubset()
    {
        await _sut.SubmitAsync(ValidRequest("PEND1", "p1@shop.com"));
        await _sut.SubmitAsync(ValidRequest("PEND2", "p2@shop.com"));
        var rejectId = (await _sut.SubmitAsync(ValidRequest("REJ1", "r1@shop.com"))).Value;
        await _sut.RejectAsync(rejectId, "reason", 1L);

        var (pendingItems, pendingTotal) = await _sut.ListAsync(1, 50, RegistrationStatus.Pending);
        var (rejectedItems, rejectedTotal) = await _sut.ListAsync(1, 50, RegistrationStatus.Rejected);

        pendingItems.Should().HaveCount(2);
        pendingTotal.Should().Be(2);
        rejectedItems.Should().HaveCount(1);
        rejectedTotal.Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SubmitRegistrationRequest ValidRequest(string shopCode, string email) =>
        new(shopCode, "Test Legal Name", email, "Admin User", "Password@123!");

    private void SeedStarterPlan()
    {
        _db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Code = Constants.Plans.Starter, Label = "Starter", IsActive = true,
            MonthlyPrice = 0, AnnualPrice = 0, MaxUsers = 2,
            MaxProducts = 500, MaxInvoicesPerMonth = 1000,
            StorageQuotaMb = 500, SmsQuotaPerMonth = 100, EmailQuotaPerMonth = 500,
            CreatedAtUtc = DateTime.UtcNow,
        });
        _db.SaveChanges();
    }
}

file sealed class StubTenantContext(long shopId) : ErpSaas.Shared.Data.ITenantContext
{
    public long ShopId => shopId;
    public long CurrentUserId => 1L;
    public IReadOnlyList<string> CurrentUserRoles => [];
}
