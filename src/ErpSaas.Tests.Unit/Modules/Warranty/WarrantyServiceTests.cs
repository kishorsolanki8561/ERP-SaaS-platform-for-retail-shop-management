using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Warranty.Entities;
using ErpSaas.Modules.Warranty.Enums;
using ErpSaas.Modules.Warranty.Infrastructure;
using ErpSaas.Modules.Warranty.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Warranty;

internal sealed class WarrantyTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        WarrantyModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class WarrantyServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence = Substitute.For<ISequenceService>();
    private readonly SqliteConnection _sqlite;
    private int _seqCounter;
    private readonly WarrantyService _sut;

    public WarrantyServiceTests()
    {
        var ctx = new StubTenantContext(1L);
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new WarrantyTenantDbContext(opts, ctx, new AuditSaveChangesInterceptor(ctx), new TenantSaveChangesInterceptor(ctx));
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"WC-{++_seqCounter:000000}"));

        _sut = new WarrantyService(_db, _errorLogger, _sequence, ctx, Substitute.For<ILogger<WarrantyService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    [Fact]
    public async Task RegisterWarrantyAsync_HappyPath_PersistsRegistration()
    {
        var dto = new RegisterWarrantyDto(1, 1, 1, 1, "SN-001", DateTime.Today, 12, WarrantyType.Warranty, null, null);
        var result = await _sut.RegisterWarrantyAsync(dto);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _db.Set<WarrantyRegistration>().CountAsync());
    }

    [Fact]
    public async Task RegisterWarrantyAsync_DuplicateSerial_ReturnsConflict()
    {
        var dto = new RegisterWarrantyDto(1, 1, 1, 1, "SN-DUPE", DateTime.Today, 12, WarrantyType.Warranty, null, null);
        await _sut.RegisterWarrantyAsync(dto);
        var result = await _sut.RegisterWarrantyAsync(dto);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Warranty.SerialNumberExists, result.Errors[0]);
    }

    [Fact]
    public async Task RegisterWarrantyAsync_SetsWarrantyEndDate_BasedOnMonths()
    {
        var start = new DateTime(2026, 1, 1);
        var dto = new RegisterWarrantyDto(1, 1, 1, 1, "SN-DATE", start, 24, WarrantyType.Guarantee, null, null);
        await _sut.RegisterWarrantyAsync(dto);
        var reg = await _db.Set<WarrantyRegistration>().FirstAsync();
        Assert.Equal(start.AddMonths(24), reg.WarrantyEndDate);
    }

    [Fact]
    public async Task GetBySerialAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetBySerialAsync("NONEXISTENT");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySerialAsync_Found_ReturnsDto()
    {
        var dto = new RegisterWarrantyDto(1, 1, 1, 1, "SN-FIND", DateTime.Today, 6, WarrantyType.Warranty, null, null);
        await _sut.RegisterWarrantyAsync(dto);
        var result = await _sut.GetBySerialAsync("SN-FIND");
        Assert.NotNull(result);
        Assert.Equal("SN-FIND", result!.SerialNumber);
    }

    [Fact]
    public async Task CreateClaimAsync_HappyPath_PersistsClaim()
    {
        var reg = await _sut.RegisterWarrantyAsync(
            new RegisterWarrantyDto(1, 1, 1, 1, "SN-CLM", DateTime.Today, 12, WarrantyType.Warranty, null, null));
        var result = await _sut.CreateClaimAsync(new CreateClaimDto(reg.Value, DateTime.Today, "Device not charging", null));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateClaimAsync_RegistrationNotFound_ReturnsNotFound()
    {
        var result = await _sut.CreateClaimAsync(new CreateClaimDto(9999, DateTime.Today, "Issue", null));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Warranty.RegistrationNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task CreateClaimAsync_WarrantyExpired_ReturnsConflict()
    {
        var reg = await _sut.RegisterWarrantyAsync(
            new RegisterWarrantyDto(1, 1, 1, 1, "SN-EXP", DateTime.Today.AddMonths(-13), 12, WarrantyType.Warranty, null, null));
        var result = await _sut.CreateClaimAsync(new CreateClaimDto(reg.Value, DateTime.Today, "Issue", null));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Warranty.WarrantyExpired, result.Errors[0]);
    }

    [Fact]
    public async Task ResolveClaimAsync_ClaimNotFound_ReturnsNotFound()
    {
        var result = await _sut.ResolveClaimAsync(9999, new ResolveClaimDto(ClaimStatus.Resolved, "Fixed", null));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Warranty.ClaimNotFound, result.Errors[0]);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
