using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Transport.Entities;
using ErpSaas.Modules.Transport.Enums;
using ErpSaas.Modules.Transport.Infrastructure;
using ErpSaas.Modules.Transport.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Transport;

internal sealed class TransportTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        TransportModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class TransportServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence = Substitute.For<ISequenceService>();
    private readonly SqliteConnection _sqlite;
    private int _seqCounter;
    private readonly TransportService _sut;

    public TransportServiceTests()
    {
        var ctx = new StubTenantContext(1L);
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new TransportTenantDbContext(opts, ctx, new AuditSaveChangesInterceptor(ctx), new TenantSaveChangesInterceptor(ctx));
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"DEL-{++_seqCounter:000000}"));

        _sut = new TransportService(_db, _errorLogger, _sequence, ctx);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    [Fact]
    public async Task CreateProviderAsync_HappyPath_PersistsProvider()
    {
        var result = await _sut.CreateProviderAsync(new CreateTransportProviderDto("Blue Logistics", "Ravi", "9999000001", null));
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _db.Set<TransportProvider>().CountAsync());
    }

    [Fact]
    public async Task ToggleProviderAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.ToggleProviderAsync(9999, false);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Transport.ProviderNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task CreateVehicleAsync_HappyPath_PersistsVehicle()
    {
        var result = await _sut.CreateVehicleAsync(new CreateVehicleDto("MH12AB1234", "Tata Ace", 500m, null, null, null));
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _db.Set<Vehicle>().CountAsync());
    }

    [Fact]
    public async Task CreateVehicleAsync_DuplicatePlate_ReturnsConflict()
    {
        await _sut.CreateVehicleAsync(new CreateVehicleDto("MH12AB1234", "Tata Ace", 500m, null, null, null));
        var result = await _sut.CreateVehicleAsync(new CreateVehicleDto("MH12AB1234", "Mahindra", 600m, null, null, null));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Transport.LicensePlateExists, result.Errors[0]);
    }

    [Fact]
    public async Task ToggleVehicleAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.ToggleVehicleAsync(9999, false);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Transport.VehicleNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task CreateDeliveryAsync_HappyPath_PersistsDelivery()
    {
        var dto = new CreateDeliveryDto(
            DeliveryReferenceType.Invoice, 1, "INV-000001",
            1, "John Doe", null, null,
            DateTime.Today.AddDays(1), "123 Main Street", null);

        var result = await _sut.CreateDeliveryAsync(dto);

        Assert.True(result.IsSuccess);
        var delivery = await _db.Set<Delivery>().FirstAsync();
        Assert.Equal(DeliveryStatus.Scheduled, delivery.Status);
        Assert.StartsWith("DEL-", delivery.DeliveryNumber);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.UpdateDeliveryStatusAsync(9999, new UpdateDeliveryStatusDto(DeliveryStatus.Delivered, null));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Transport.DeliveryNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_AlreadyDelivered_ReturnsConflict()
    {
        var createResult = await _sut.CreateDeliveryAsync(new CreateDeliveryDto(
            DeliveryReferenceType.Invoice, 1, "INV-000001", 1, "Customer", null, null, DateTime.Today, "Address", null));
        await _sut.UpdateDeliveryStatusAsync(createResult.Value, new UpdateDeliveryStatusDto(DeliveryStatus.Delivered, "Done"));

        var result = await _sut.UpdateDeliveryStatusAsync(createResult.Value, new UpdateDeliveryStatusDto(DeliveryStatus.Delivered, "Again"));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Transport.DeliveryAlreadyDelivered, result.Errors[0]);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_ToDelivered_SetsDeliveredDate()
    {
        var createResult = await _sut.CreateDeliveryAsync(new CreateDeliveryDto(
            DeliveryReferenceType.Invoice, 1, "INV-000001", 1, "Customer", null, null, DateTime.Today, "Address", null));

        await _sut.UpdateDeliveryStatusAsync(createResult.Value, new UpdateDeliveryStatusDto(DeliveryStatus.Delivered, null));

        var delivery = await _db.Set<Delivery>().FindAsync(createResult.Value);
        Assert.NotNull(delivery!.DeliveredDate);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_CreatesDeliveryLog()
    {
        var createResult = await _sut.CreateDeliveryAsync(new CreateDeliveryDto(
            DeliveryReferenceType.Invoice, 1, "INV-000001", 1, "Customer", null, null, DateTime.Today, "Address", null));

        await _sut.UpdateDeliveryStatusAsync(createResult.Value, new UpdateDeliveryStatusDto(DeliveryStatus.OutForDelivery, "On the way"));

        var logs = await _db.Set<DeliveryLog>().ToListAsync();
        Assert.Single(logs);
        Assert.Equal(DeliveryStatus.OutForDelivery, logs[0].Status);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
