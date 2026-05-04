using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Sync.Entities;
using ErpSaas.Modules.Sync.Enums;
using ErpSaas.Modules.Sync.Infrastructure;
using ErpSaas.Modules.Sync.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Sync;

file sealed class StubTenantContext(long shopId, long userId = 42L) : ITenantContext
{
    public long ShopId => shopId;
    public long CurrentUserId => userId;
    public IReadOnlyList<string> CurrentUserRoles => [];
}

// ── Test DbContext ─────────────────────────────────────────────────────────────

internal sealed class SyncTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        new SyncModelConfigurator().Configure(modelBuilder);
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class SyncServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly DeviceService _deviceSut;
    private readonly SyncService _syncSut;
    private readonly SqliteConnection _sqliteConnection;

    private const long ShopId = 1L;
    private const long BranchId = 10L;
    private const long UserId = 99L;

    public SyncServiceTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var stubCtx = new StubTenantContext(shopId: ShopId, userId: UserId);
        var auditInterceptor = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _db = new SyncTenantDbContext(options, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _deviceSut = new DeviceService(_db, _errorLogger, stubCtx);
        _syncSut   = new SyncService(_db, _errorLogger, stubCtx);
    }

    // ── DeviceService ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewDevice_CreatesRegistration()
    {
        var dto = new RegisterDeviceDto("DEV-001", BranchId, UserId, "DesktopPos", "Windows 11", "1.0.0");

        var result = await _deviceSut.RegisterAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DeviceId.Should().Be("DEV-001");
        result.Value.Type.Should().Be("DesktopPos");
        var persisted = await _db.Set<DeviceRegistration>().SingleAsync();
        persisted.DeviceId.Should().Be("DEV-001");
        persisted.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ExistingDevice_UpdatesRegistration()
    {
        var dto = new RegisterDeviceDto("DEV-001", BranchId, UserId, "DesktopPos", "Windows 11", "1.0.0");
        await _deviceSut.RegisterAsync(dto);

        var updateDto = new RegisterDeviceDto("DEV-001", BranchId, UserId, "DesktopPos", "Windows 11", "1.1.0");
        var result = await _deviceSut.RegisterAsync(updateDto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AppVersion.Should().Be("1.1.0");
        var count = await _db.Set<DeviceRegistration>().CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_InvalidDeviceType_ReturnsValidationError()
    {
        var dto = new RegisterDeviceDto("DEV-001", BranchId, UserId, "InvalidType", "Windows 11", "1.0.0");

        var result = await _deviceSut.RegisterAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("SYNC_002"));
    }

    [Fact]
    public async Task HeartbeatAsync_UnknownDevice_ReturnsNotFound()
    {
        var dto = new HeartbeatDto("DEV-999", "1.0.0", "Windows 11");

        var result = await _deviceSut.HeartbeatAsync(9999L, dto);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_ExistingDevice_SetsInactive()
    {
        var dto = new RegisterDeviceDto("DEV-001", BranchId, UserId, "MobilePos", "Android 14", "2.0.0");
        var reg = await _deviceSut.RegisterAsync(dto);

        var result = await _deviceSut.DeactivateAsync(reg.Value!.Id);

        result.IsSuccess.Should().BeTrue();
        var device = await _db.Set<DeviceRegistration>().SingleAsync();
        device.IsActive.Should().BeFalse();
    }

    // ── SyncService ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessCommandsAsync_NewCommand_StoresAndReturnsSuccess()
    {
        var cmdId = Guid.NewGuid();
        var batch = new SyncCommandsBatchDto([
            new OfflineCommandDto(cmdId, "DEV-001", "CreateInvoice", "{}", DateTime.UtcNow),
        ]);

        var result = await _syncSut.ProcessCommandsAsync(batch);

        result.Results.Should().HaveCount(1);
        result.Results[0].Success.Should().BeTrue();
        result.Results[0].ClientCommandId.Should().Be(cmdId);

        var stored = await _db.Set<OfflineCommand>().SingleAsync();
        stored.ClientCommandId.Should().Be(cmdId);
        stored.Status.Should().Be(OfflineCommandStatus.Applied);
    }

    [Fact]
    public async Task ProcessCommandsAsync_DuplicateCommand_ReturnsIdempotentResult()
    {
        var cmdId = Guid.NewGuid();
        var batch = new SyncCommandsBatchDto([
            new OfflineCommandDto(cmdId, "DEV-001", "CreateInvoice", "{}", DateTime.UtcNow),
        ]);

        await _syncSut.ProcessCommandsAsync(batch);
        var secondResult = await _syncSut.ProcessCommandsAsync(batch);

        secondResult.Results[0].Success.Should().BeTrue();
        var count = await _db.Set<OfflineCommand>().CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task AllocateInvoiceRangeAsync_FirstAllocation_StartsFromOne()
    {
        var dto = new AllocateInvoiceRangeDto("DEV-001", BranchId, 50);

        var result = await _syncSut.AllocateInvoiceRangeAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RangeStart.Should().Be(1);
        result.Value.RangeEnd.Should().Be(50);
    }

    [Fact]
    public async Task AllocateInvoiceRangeAsync_SecondAllocation_ContinuesFromEnd()
    {
        var dto = new AllocateInvoiceRangeDto("DEV-001", BranchId, 50);
        await _syncSut.AllocateInvoiceRangeAsync(dto);

        var result = await _syncSut.AllocateInvoiceRangeAsync(
            new AllocateInvoiceRangeDto("DEV-002", BranchId, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value!.RangeStart.Should().Be(51);
        result.Value.RangeEnd.Should().Be(70);
    }

    [Fact]
    public async Task ReleaseInvoiceRangeAsync_ActiveAllocation_SetsReleased()
    {
        var dto = new AllocateInvoiceRangeDto("DEV-001", BranchId, 50);
        var alloc = await _syncSut.AllocateInvoiceRangeAsync(dto);

        var result = await _syncSut.ReleaseInvoiceRangeAsync(
            alloc.Value!.AllocationId, new ReleaseInvoiceRangeDto(30));

        result.IsSuccess.Should().BeTrue();
        var stored = await _db.Set<InvoiceNumberAllocation>().SingleAsync();
        stored.Status.Should().Be(InvoiceNumberAllocationStatus.Released);
        stored.LastUsed.Should().Be(30);
    }

    [Fact]
    public async Task ReleaseInvoiceRangeAsync_AlreadyReleased_ReturnsConflict()
    {
        var dto = new AllocateInvoiceRangeDto("DEV-001", BranchId, 10);
        var alloc = await _syncSut.AllocateInvoiceRangeAsync(dto);
        await _syncSut.ReleaseInvoiceRangeAsync(alloc.Value!.AllocationId, new ReleaseInvoiceRangeDto(5));

        var result = await _syncSut.ReleaseInvoiceRangeAsync(
            alloc.Value.AllocationId, new ReleaseInvoiceRangeDto(5));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListExceptionsAsync_ReturnsOnlyRejectedAndWarnings()
    {
        var batch = new SyncCommandsBatchDto([
            new OfflineCommandDto(Guid.NewGuid(), "DEV-001", "CreateInvoice", "{}", DateTime.UtcNow),
            new OfflineCommandDto(Guid.NewGuid(), "DEV-001", "CreateInvoice", "{}", DateTime.UtcNow),
        ]);
        await _syncSut.ProcessCommandsAsync(batch);

        var cmd = await _db.Set<OfflineCommand>().FirstAsync();
        cmd.Status = OfflineCommandStatus.Rejected;
        cmd.RejectionReason = "Credit limit exceeded";
        await _db.SaveChangesAsync();

        var (items, total) = await _syncSut.ListExceptionsAsync(1, 20);

        total.Should().Be(1);
        items[0].Status.Should().Be("Rejected");
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }
}
